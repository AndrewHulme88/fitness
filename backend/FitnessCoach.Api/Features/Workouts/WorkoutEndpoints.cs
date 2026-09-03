using System.Globalization;

using FitnessCoach.Api.Features.Exercises;
using FitnessCoach.Api.Features.Identity;
using FitnessCoach.Api.Features.Profiles;
using FitnessCoach.Api.Persistence;

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Microsoft.AspNetCore.RateLimiting;

using FitnessCoach.Api.Infrastructure;

namespace FitnessCoach.Api.Features.Workouts;

internal static class WorkoutEndpoints
{
    private const int DefaultLimit = 20;
    private const int MaximumLimit = 50;
    private const int MaximumOffset = 10_000;

    public static IEndpointRouteBuilder MapWorkoutEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var workouts = endpoints
            .MapGroup("/profiles/{profileId:guid}/workouts")
            .WithTags("Workouts")
            .RequireOwnedProfile()
            .RequireRateLimiting(ApiRateLimitPolicies.Standard);
        workouts.ProducesProblem(StatusCodes.Status429TooManyRequests);
        if (endpoints.ServiceProvider.GetRequiredService<IConfiguration>().GetSection("Cognito").Exists())
        {
            workouts.RequireAuthorization();
        }

        workouts.MapPost("/", CreateWorkoutAsync)
            .WithName("CreateWorkout")
            .WithSummary("Create a reusable workout plan");
        workouts.MapGet("/", ListWorkoutsAsync)
            .WithName("ListWorkouts")
            .WithSummary("List reusable workout plans")
            .AddOpenApiOperationTransformer(ConfigureListContractAsync);
        workouts.MapGet("/{workoutId:guid}", GetWorkoutAsync)
            .WithName("GetWorkout")
            .WithSummary("Get a reusable workout plan");
        workouts.MapPut("/{workoutId:guid}", UpdateWorkoutAsync)
            .WithName("UpdateWorkout")
            .WithSummary("Update and reorder a reusable workout plan")
            .ProducesProblem(StatusCodes.Status409Conflict);

        return endpoints;
    }

    private static Task ConfigureListContractAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext _,
        CancellationToken cancellationToken)
    {
        foreach (var parameter in operation.Parameters ?? [])
        {
            if (parameter is not OpenApiParameter concreteParameter
                || concreteParameter.Name is not ("limit" or "offset"))
            {
                continue;
            }

            concreteParameter.Schema = new OpenApiSchema
            {
                Type = JsonSchemaType.Integer,
                Format = "int32",
                Minimum = concreteParameter.Name == "limit" ? "1" : "0",
                Maximum = concreteParameter.Name == "limit"
                    ? MaximumLimit.ToString(CultureInfo.InvariantCulture)
                    : MaximumOffset.ToString(CultureInfo.InvariantCulture),
            };
        }

        return cancellationToken.IsCancellationRequested
            ? Task.FromCanceled(cancellationToken)
            : Task.CompletedTask;
    }

    private static async Task<Results<
        Created<WorkoutDetailResponse>,
        NotFound,
        ValidationProblem>> CreateWorkoutAsync(
            Guid profileId,
            CreateWorkoutRequest request,
            FitnessCoachDbContext dbContext,
            TimeProvider timeProvider,
            CancellationToken cancellationToken)
    {
        if (!await ProfileExistsAsync(profileId, dbContext, cancellationToken))
        {
            return TypedResults.NotFound();
        }

        if (request.Exercises is null)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["exercises"] = ["Choose at least one exercise."],
            });
        }

        var exercises = await LoadExercisesAsync(request.Exercises, dbContext, cancellationToken);
        var errors = WorkoutRequestValidator.Validate(
            request.Name,
            request.Exercises,
            exercises,
            out var inputs);
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        var workout = WorkoutPlan.Create(
            profileId,
            request.Name,
            inputs,
            timeProvider.GetUtcNow());
        dbContext.Add(workout);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = MapDetail(workout, exercises);
        return TypedResults.Created(
            $"/profiles/{profileId}/workouts/{workout.Id}",
            response);
    }

    private static async Task<Results<Ok<WorkoutListResponse>, NotFound, ValidationProblem>>
        ListWorkoutsAsync(
            Guid profileId,
            string? limit,
            string? offset,
            FitnessCoachDbContext dbContext,
            CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        var parsedLimit = ParseBoundedInteger(
            limit,
            DefaultLimit,
            1,
            MaximumLimit,
            "limit",
            errors);
        var parsedOffset = ParseBoundedInteger(
            offset,
            0,
            0,
            MaximumOffset,
            "offset",
            errors);
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        if (!await ProfileExistsAsync(profileId, dbContext, cancellationToken))
        {
            return TypedResults.NotFound();
        }

        var matches = await dbContext.Set<WorkoutPlan>()
            .AsNoTracking()
            .Where(workout => workout.ProfileId == profileId)
            .OrderByDescending(workout => workout.UpdatedAt)
            .ThenBy(workout => workout.Id)
            .Select(workout => new WorkoutSummaryResponse(
                workout.Id,
                workout.Name,
                workout.Exercises.Count,
                workout.Exercises.Sum(exercise => exercise.PlannedSets),
                workout.Revision,
                workout.UpdatedAt))
            .Skip(parsedOffset)
            .Take(parsedLimit + 1)
            .ToListAsync(cancellationToken);

        var hasMore = matches.Count > parsedLimit;
        return TypedResults.Ok(new WorkoutListResponse(
            matches.Take(parsedLimit).ToArray(),
            hasMore ? parsedOffset + parsedLimit : null));
    }

    private static async Task<Results<Ok<WorkoutDetailResponse>, NotFound>> GetWorkoutAsync(
        Guid profileId,
        Guid workoutId,
        FitnessCoachDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var workout = await dbContext.Set<WorkoutPlan>()
            .AsNoTracking()
            .Include(item => item.Exercises)
            .AsSplitQuery()
            .SingleOrDefaultAsync(
                item => item.Id == workoutId && item.ProfileId == profileId,
                cancellationToken);
        if (workout is null)
        {
            return TypedResults.NotFound();
        }

        var exercises = await LoadExercisesAsync(
            workout.Exercises.Select(item => item.ExerciseId),
            dbContext,
            cancellationToken);
        return TypedResults.Ok(MapDetail(workout, exercises));
    }

    private static async Task<Results<
        Ok<WorkoutDetailResponse>,
        NotFound,
        ValidationProblem,
        ProblemHttpResult>> UpdateWorkoutAsync(
            Guid profileId,
            Guid workoutId,
            UpdateWorkoutRequest request,
            FitnessCoachDbContext dbContext,
            TimeProvider timeProvider,
            CancellationToken cancellationToken)
    {
        var workout = await dbContext.Set<WorkoutPlan>()
            .Include(item => item.Exercises)
            .SingleOrDefaultAsync(
                item => item.Id == workoutId && item.ProfileId == profileId,
                cancellationToken);
        if (workout is null)
        {
            return TypedResults.NotFound();
        }

        if (request.ExpectedRevision != workout.Revision)
        {
            return CreateRevisionConflict();
        }

        if (request.Exercises is null)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["exercises"] = ["Choose at least one exercise."],
            });
        }

        var exercises = await LoadExercisesAsync(request.Exercises, dbContext, cancellationToken);
        var errors = WorkoutRequestValidator.Validate(
            request.Name,
            request.Exercises,
            exercises,
            out var inputs);
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        workout.Update(request.Name, inputs, timeProvider.GetUtcNow());
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return CreateRevisionConflict();
        }

        return TypedResults.Ok(MapDetail(workout, exercises));
    }

    private static ProblemHttpResult CreateRevisionConflict()
    {
        return TypedResults.Problem(
            detail: "Reload the workout before saving your changes.",
            statusCode: StatusCodes.Status409Conflict,
            title: "The workout changed after it was loaded.");
    }

    private static Task<bool> ProfileExistsAsync(
        Guid profileId,
        FitnessCoachDbContext dbContext,
        CancellationToken cancellationToken)
    {
        return dbContext.Set<TrainingProfile>()
            .AsNoTracking()
            .AnyAsync(profile => profile.Id == profileId, cancellationToken);
    }

    private static Task<Dictionary<Guid, Exercise>> LoadExercisesAsync(
        IEnumerable<WorkoutExerciseRequest> requests,
        FitnessCoachDbContext dbContext,
        CancellationToken cancellationToken)
    {
        return LoadExercisesAsync(
            requests.Select(request => request.ExerciseId),
            dbContext,
            cancellationToken);
    }

    private static async Task<Dictionary<Guid, Exercise>> LoadExercisesAsync(
        IEnumerable<Guid> exerciseIds,
        FitnessCoachDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var ids = exerciseIds.Distinct().ToArray();
        return await dbContext.Set<Exercise>()
            .AsNoTracking()
            .Where(exercise => ids.Contains(exercise.Id))
            .Include(exercise => exercise.Muscles)
            .AsSplitQuery()
            .ToDictionaryAsync(exercise => exercise.Id, cancellationToken);
    }

    private static WorkoutDetailResponse MapDetail(
        WorkoutPlan workout,
        Dictionary<Guid, Exercise> exercises)
    {
        var items = workout.Exercises
            .OrderBy(item => item.Position)
            .Select(item =>
            {
                var exercise = exercises[item.ExerciseId];
                return new WorkoutExerciseResponse(
                    item.ExerciseId,
                    item.Position,
                    exercise.Name,
                    exercise.TrackingMode,
                    exercise.Muscles
                        .Where(muscle => muscle.Role == MuscleRole.Primary)
                        .Select(muscle => muscle.Muscle)
                        .Order()
                        .ToArray(),
                    item.PlannedSets,
                    item.MinimumRepetitions,
                    item.MaximumRepetitions,
                    item.TargetLoadKilograms,
                    item.TargetDurationSeconds,
                    item.TargetDistanceMetres);
            })
            .ToArray();

        return new WorkoutDetailResponse(
            workout.Id,
            workout.ProfileId,
            workout.Name,
            workout.Revision,
            items,
            workout.CreatedAt,
            workout.UpdatedAt);
    }

    private static int ParseBoundedInteger(
        string? value,
        int defaultValue,
        int minimum,
        int maximum,
        string fieldName,
        Dictionary<string, string[]> errors)
    {
        if (value is null)
        {
            return defaultValue;
        }

        if (int.TryParse(
                value,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var parsed)
            && parsed >= minimum
            && parsed <= maximum)
        {
            return parsed;
        }

        errors[fieldName] = [$"Value must be between {minimum} and {maximum}."];
        return defaultValue;
    }
}

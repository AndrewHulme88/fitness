using FitnessCoach.Api.Features.Exercises;
using FitnessCoach.Api.Features.Workouts;
using FitnessCoach.Api.Persistence;

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

using Npgsql;

namespace FitnessCoach.Api.Features.Sessions;

internal static class WorkoutSessionEndpoints
{
    public static IEndpointRouteBuilder MapWorkoutSessionEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var sessions = endpoints
            .MapGroup("/profiles/{profileId:guid}/workout-sessions")
            .WithTags("Workout sessions");

        sessions.MapPost("/", StartSessionAsync)
            .WithName("StartWorkoutSession")
            .WithSummary("Start a workout session from an immutable plan snapshot")
            .ProducesProblem(StatusCodes.Status409Conflict);
        sessions.MapGet("/active", GetActiveSessionAsync)
            .WithName("GetActiveWorkoutSession")
            .WithSummary("Get the profile's active workout session");
        sessions.MapGet("/{sessionId:guid}", GetSessionAsync)
            .WithName("GetWorkoutSession")
            .WithSummary("Get a workout session");
        sessions.MapPut("/{sessionId:guid}", UpdateSessionAsync)
            .WithName("UpdateWorkoutSession")
            .WithSummary("Synchronize an active workout session")
            .ProducesProblem(StatusCodes.Status409Conflict);
        sessions.MapDelete("/{sessionId:guid}", DiscardSessionAsync)
            .WithName("DiscardWorkoutSession")
            .WithSummary("Permanently discard an active workout session")
            .ProducesProblem(StatusCodes.Status409Conflict);

        return endpoints;
    }

    private static async Task<Results<
        Created<WorkoutSessionResponse>,
        Ok<WorkoutSessionResponse>,
        NotFound,
        ValidationProblem,
        ProblemHttpResult>> StartSessionAsync(
        Guid profileId,
        StartWorkoutSessionRequest request,
        FitnessCoachDbContext dbContext,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        if (request.SessionId == Guid.Empty)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["sessionId"] = ["Provide a stable session identifier."],
            });
        }

        var existingById = await LoadSessionAsync(
            profileId,
            request.SessionId,
            dbContext,
            cancellationToken);
        if (existingById is not null)
        {
            return existingById.WorkoutPlanId == request.WorkoutPlanId
                ? TypedResults.Ok(Map(existingById))
                : CreateSessionIdentifierConflict();
        }

        if (await dbContext.Set<WorkoutSession>().AsNoTracking().AnyAsync(
                item => item.ProfileId == profileId
                    && item.Status == WorkoutSessionStatus.Active,
                cancellationToken))
        {
            return CreateActiveSessionConflict();
        }

        var workout = await dbContext.Set<WorkoutPlan>()
            .AsNoTracking()
            .Include(item => item.Exercises)
            .AsSplitQuery()
            .SingleOrDefaultAsync(
                item => item.Id == request.WorkoutPlanId && item.ProfileId == profileId,
                cancellationToken);
        if (workout is null)
        {
            return TypedResults.NotFound();
        }

        var exerciseIds = workout.Exercises.Select(item => item.ExerciseId).ToArray();
        var exercises = await dbContext.Set<Exercise>()
            .AsNoTracking()
            .Where(item => exerciseIds.Contains(item.Id))
            .Include(item => item.Muscles)
            .AsSplitQuery()
            .ToDictionaryAsync(item => item.Id, cancellationToken);

        var session = WorkoutSession.Start(
            request.SessionId,
            workout,
            exercises,
            timeProvider.GetUtcNow());
        dbContext.Add(session);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException { SqlState: "23505" })
        {
            return CreateActiveSessionConflict();
        }

        return TypedResults.Created(
            $"/profiles/{profileId}/workout-sessions/{session.Id}",
            Map(session));
    }

    private static async Task<Results<Ok<WorkoutSessionResponse>, NotFound>>
        GetActiveSessionAsync(
            Guid profileId,
            FitnessCoachDbContext dbContext,
            CancellationToken cancellationToken)
    {
        var session = await LoadSessionQuery(dbContext)
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.ProfileId == profileId
                    && item.Status == WorkoutSessionStatus.Active,
                cancellationToken);
        return session is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(Map(session));
    }

    private static async Task<Results<Ok<WorkoutSessionResponse>, NotFound>> GetSessionAsync(
        Guid profileId,
        Guid sessionId,
        FitnessCoachDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var session = await LoadSessionAsync(
            profileId,
            sessionId,
            dbContext,
            cancellationToken);
        return session is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(Map(session));
    }

    private static async Task<Results<
        Ok<WorkoutSessionResponse>,
        NotFound,
        ValidationProblem,
        ProblemHttpResult>> UpdateSessionAsync(
        Guid profileId,
        Guid sessionId,
        UpdateWorkoutSessionRequest request,
        FitnessCoachDbContext dbContext,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var session = await LoadSessionAsync(
            profileId,
            sessionId,
            dbContext,
            cancellationToken);
        if (session is null)
        {
            return TypedResults.NotFound();
        }

        if (session.LastMutationId == request.ClientMutationId
            && request.ClientMutationId != Guid.Empty)
        {
            return TypedResults.Ok(Map(session));
        }

        if (session.Status != WorkoutSessionStatus.Active)
        {
            return CreateCompletedSessionConflict();
        }

        if (request.ExpectedRevision != session.Revision)
        {
            return CreateRevisionConflict();
        }

        var errors = WorkoutSessionRequestValidator.Validate(session, request, out var input);
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        var existingSetIds = session.Exercises
            .SelectMany(item => item.Sets)
            .Select(item => item.Id)
            .ToHashSet();
        session.Update(input, timeProvider.GetUtcNow());
        foreach (var addedSet in session.Exercises
                     .SelectMany(item => item.Sets)
                     .Where(item => !existingSetIds.Contains(item.Id)))
        {
            dbContext.Entry(addedSet).State = EntityState.Added;
        }

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return CreateRevisionConflict();
        }

        return TypedResults.Ok(Map(session));
    }

    private static async Task<Results<NoContent, NotFound, ProblemHttpResult>>
        DiscardSessionAsync(
            Guid profileId,
            Guid sessionId,
            FitnessCoachDbContext dbContext,
            CancellationToken cancellationToken)
    {
        var session = await dbContext.Set<WorkoutSession>().SingleOrDefaultAsync(
            item => item.Id == sessionId && item.ProfileId == profileId,
            cancellationToken);
        if (session is null)
        {
            return TypedResults.NotFound();
        }

        if (session.Status != WorkoutSessionStatus.Active)
        {
            return CreateCompletedSessionConflict();
        }

        dbContext.Remove(session);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return CreateRevisionConflict();
        }
        return TypedResults.NoContent();
    }

    private static Task<WorkoutSession?> LoadSessionAsync(
        Guid profileId,
        Guid sessionId,
        FitnessCoachDbContext dbContext,
        CancellationToken cancellationToken) => LoadSessionQuery(dbContext)
            .SingleOrDefaultAsync(
                item => item.Id == sessionId && item.ProfileId == profileId,
                cancellationToken);

    private static IQueryable<WorkoutSession> LoadSessionQuery(FitnessCoachDbContext dbContext) =>
        dbContext.Set<WorkoutSession>()
            .Include(item => item.Exercises)
            .ThenInclude(item => item.Sets)
            .AsSplitQuery();

    private static WorkoutSessionResponse Map(WorkoutSession session) => new(
        session.Id,
        session.ProfileId,
        session.WorkoutPlanId,
        session.WorkoutPlanRevision,
        session.WorkoutName,
        session.Revision,
        session.Status,
        session.StartedAt,
        session.UpdatedAt,
        session.FinishedAt,
        session.Notes,
        session.Exercises
            .OrderBy(item => item.Position)
            .Select(item => new WorkoutSessionExerciseResponse(
                item.ExerciseId,
                item.Position,
                item.ExerciseName,
                item.TrackingMode,
                item.PrimaryMuscles.Select(value =>
                    Enum.Parse<MuscleGroup>(value, ignoreCase: false)).ToArray(),
                item.PlannedSets,
                item.MinimumRepetitions,
                item.MaximumRepetitions,
                item.TargetLoadKilograms,
                item.TargetDurationSeconds,
                item.TargetDistanceMetres,
                item.IsSkipped,
                item.Notes,
                item.Sets.OrderBy(set => set.Position)
                    .Select(set => new WorkoutSessionSetResponse(
                        set.Id,
                        set.Position,
                        set.IsCompleted,
                        set.CompletedAt,
                        set.ActualRepetitions,
                        set.ActualLoadKilograms,
                        set.ActualDurationSeconds,
                        set.ActualDistanceMetres))
                    .ToArray()))
            .ToArray());

    private static ProblemHttpResult CreateActiveSessionConflict() => TypedResults.Problem(
        detail: "Resume or discard the active workout before starting another one.",
        statusCode: StatusCodes.Status409Conflict,
        title: "A workout is already active.");

    private static ProblemHttpResult CreateSessionIdentifierConflict() => TypedResults.Problem(
        detail: "Generate a new session identifier before starting this workout.",
        statusCode: StatusCodes.Status409Conflict,
        title: "The session identifier is already in use.");

    private static ProblemHttpResult CreateRevisionConflict() => TypedResults.Problem(
        detail: "Keep the local copy and reload the server version before choosing which to use.",
        statusCode: StatusCodes.Status409Conflict,
        title: "The workout session changed after it was loaded.");

    private static ProblemHttpResult CreateCompletedSessionConflict() => TypedResults.Problem(
        detail: "Completed workout sessions cannot be changed in this phase.",
        statusCode: StatusCodes.Status409Conflict,
        title: "The workout session is already complete.");
}

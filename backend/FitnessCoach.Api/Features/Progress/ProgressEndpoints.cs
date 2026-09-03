using System.Globalization;

using FitnessCoach.Api.Features.Profiles;
using FitnessCoach.Api.Features.Identity;
using FitnessCoach.Api.Features.Sessions;
using FitnessCoach.Api.Persistence;

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Microsoft.AspNetCore.RateLimiting;

using FitnessCoach.Api.Infrastructure;

namespace FitnessCoach.Api.Features.Progress;

internal static class ProgressEndpoints
{
    private const int DefaultAppearanceLimit = 12;
    private const int MaximumAppearanceLimit = 12;

    public static IEndpointRouteBuilder MapProgressEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var progress = endpoints
            .MapGroup("/profiles/{profileId:guid}/progress")
            .WithTags("Progress")
            .RequireOwnedProfile()
            .RequireRateLimiting(ApiRateLimitPolicies.Standard);
        progress.ProducesProblem(StatusCodes.Status429TooManyRequests);
        if (endpoints.ServiceProvider.GetRequiredService<IConfiguration>().GetSection("Cognito").Exists())
        {
            progress.RequireAuthorization();
        }

        progress.MapGet("/", GetOverviewAsync)
            .WithName("GetProgressOverview")
            .WithSummary("Get explainable four-week training totals and recorded exercises");
        progress.MapGet("/exercises/{exerciseId:guid}", GetExercisePerformanceAsync)
            .WithName("GetExercisePerformance")
            .WithSummary("Get recent recorded performance for one exercise")
            .AddOpenApiOperationTransformer(ConfigureExercisePerformanceContractAsync);

        return endpoints;
    }

    private static Task ConfigureExercisePerformanceContractAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext _,
        CancellationToken cancellationToken)
    {
        var parameter = operation.Parameters?
            .OfType<OpenApiParameter>()
            .SingleOrDefault(item => item.Name == "limit");
        if (parameter is not null)
        {
            parameter.Schema = new OpenApiSchema
            {
                Type = JsonSchemaType.Integer,
                Format = "int32",
                Minimum = "1",
                Maximum = MaximumAppearanceLimit.ToString(CultureInfo.InvariantCulture),
            };
        }

        return cancellationToken.IsCancellationRequested
            ? Task.FromCanceled(cancellationToken)
            : Task.CompletedTask;
    }

    private static async Task<Results<Ok<ProgressOverviewResponse>, NotFound>> GetOverviewAsync(
        Guid profileId,
        FitnessCoachDbContext dbContext,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        if (!await ProfileExistsAsync(profileId, dbContext, cancellationToken))
        {
            return TypedResults.NotFound();
        }

        var periodEnd = timeProvider.GetUtcNow();
        var periodStart = periodEnd.AddDays(-28);
        var completedSessions = dbContext.Set<WorkoutSession>()
            .AsNoTracking()
            .Where(item => item.ProfileId == profileId
                && item.Status == WorkoutSessionStatus.Completed);
        var periodSessions = completedSessions.Where(item =>
            item.FinishedAt >= periodStart && item.FinishedAt <= periodEnd);

        var completedWorkoutCount = await periodSessions.CountAsync(cancellationToken);
        var completedSetCount = await periodSessions
            .SelectMany(session => session.Exercises)
            .SelectMany(exercise => exercise.Sets)
            .CountAsync(set => set.IsCompleted, cancellationToken);
        var durationSeconds = await periodSessions
            .SumAsync(
                session => (session.FinishedAt!.Value - session.StartedAt).TotalSeconds,
                cancellationToken);
        var recordedExercises = await completedSessions
            .SelectMany(session => session.Exercises
                .Where(exercise => exercise.Sets.Any(set => set.IsCompleted))
                .Select(exercise => new
                {
                    exercise.ExerciseId,
                    exercise.ExerciseName,
                    exercise.TrackingMode,
                    session.FinishedAt,
                }))
            .GroupBy(item => new
            {
                item.ExerciseId,
                item.ExerciseName,
                item.TrackingMode,
            })
            .Select(group => new
            {
                group.Key.ExerciseId,
                group.Key.ExerciseName,
                group.Key.TrackingMode,
                AppearanceCount = group.Count(),
                LastPerformedAt = group.Max(item => item.FinishedAt)!.Value,
            })
            .OrderByDescending(item => item.LastPerformedAt)
            .ThenBy(item => item.ExerciseName)
            .Take(50)
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(new ProgressOverviewResponse(
            periodStart,
            periodEnd,
            completedWorkoutCount,
            completedSetCount,
            ToDurationSeconds(durationSeconds),
            recordedExercises.Select(item => new RecordedExerciseSummaryResponse(
                item.ExerciseId,
                item.ExerciseName,
                item.TrackingMode,
                item.AppearanceCount,
                item.LastPerformedAt))
                .ToArray()));
    }

    private static async Task<Results<
        Ok<ExercisePerformanceResponse>,
        NotFound,
        ValidationProblem>> GetExercisePerformanceAsync(
            Guid profileId,
            Guid exerciseId,
            string? limit,
            FitnessCoachDbContext dbContext,
            CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        var parsedLimit = ParseBoundedInteger(
            limit,
            DefaultAppearanceLimit,
            1,
            MaximumAppearanceLimit,
            "limit",
            errors);
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        if (!await ProfileExistsAsync(profileId, dbContext, cancellationToken))
        {
            return TypedResults.NotFound();
        }

        var appearances = await dbContext.Set<WorkoutSession>()
            .AsNoTracking()
            .Where(session => session.ProfileId == profileId
                && session.Status == WorkoutSessionStatus.Completed)
            .SelectMany(session => session.Exercises
                .Where(exercise => exercise.ExerciseId == exerciseId
                    && exercise.Sets.Any(set => set.IsCompleted))
                .Select(exercise => new
                {
                    SessionId = session.Id,
                    session.WorkoutName,
                    PerformedAt = session.FinishedAt!.Value,
                    exercise.ExerciseName,
                    exercise.TrackingMode,
                }))
            .OrderByDescending(item => item.PerformedAt)
            .ThenByDescending(item => item.SessionId)
            .Take(parsedLimit)
            .ToListAsync(cancellationToken);

        if (appearances.Count == 0)
        {
            return TypedResults.NotFound();
        }

        var sessionIds = appearances.Select(item => item.SessionId).ToArray();
        var recordedSets = await dbContext.Set<WorkoutSessionSet>()
            .AsNoTracking()
            .Where(set => sessionIds.Contains(set.WorkoutSessionId)
                && set.ExerciseId == exerciseId
                && set.IsCompleted)
            .OrderBy(set => set.WorkoutSessionId)
            .ThenBy(set => set.Position)
            .Select(set => new
            {
                set.WorkoutSessionId,
                Response = new RecordedSetResponse(
                    set.Position,
                    set.ActualRepetitions,
                    set.ActualLoadKilograms,
                    set.ActualDurationSeconds,
                    set.ActualDistanceMetres),
            })
            .ToListAsync(cancellationToken);
        var setsBySession = recordedSets
            .GroupBy(item => item.WorkoutSessionId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<RecordedSetResponse>)group
                    .Select(item => item.Response)
                    .ToArray());

        var first = appearances[0];
        return TypedResults.Ok(new ExercisePerformanceResponse(
            exerciseId,
            first.ExerciseName,
            first.TrackingMode,
            appearances.Select(item => new ExercisePerformanceAppearanceResponse(
                item.SessionId,
                item.WorkoutName,
                item.PerformedAt,
                setsBySession[item.SessionId]))
                .ToArray()));
    }

    private static Task<bool> ProfileExistsAsync(
        Guid profileId,
        FitnessCoachDbContext dbContext,
        CancellationToken cancellationToken) => dbContext.Set<TrainingProfile>()
            .AsNoTracking()
            .AnyAsync(item => item.Id == profileId, cancellationToken);

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

        errors[fieldName] = [$"Choose a whole number from {minimum} to {maximum}."];
        return defaultValue;
    }

    private static int ToDurationSeconds(double value)
    {
        var seconds = Math.Max(0, value);
        return (int)Math.Min(seconds, int.MaxValue);
    }
}

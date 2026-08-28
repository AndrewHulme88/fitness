using FitnessCoach.Api.Features.Exercises;

namespace FitnessCoach.Api.Features.Sessions;

public enum WorkoutSessionStatus
{
    Active,
    Completed,
}

public sealed record StartWorkoutSessionRequest(Guid SessionId, Guid WorkoutPlanId);

public sealed record UpdateWorkoutSessionRequest(
    int ExpectedRevision,
    Guid ClientMutationId,
    WorkoutSessionStatus Status,
    DateTimeOffset? FinishedAt,
    string? Notes,
    IReadOnlyList<WorkoutSessionExerciseRequest> Exercises);

public sealed record WorkoutSessionExerciseRequest(
    Guid ExerciseId,
    bool IsSkipped,
    string? Notes,
    IReadOnlyList<WorkoutSessionSetRequest> Sets);

public sealed record WorkoutSessionSetRequest(
    Guid SetId,
    bool IsCompleted,
    DateTimeOffset? CompletedAt,
    int? ActualRepetitions,
    decimal? ActualLoadKilograms,
    int? ActualDurationSeconds,
    decimal? ActualDistanceMetres);

public sealed record WorkoutSessionResponse(
    Guid Id,
    Guid ProfileId,
    Guid WorkoutPlanId,
    int WorkoutPlanRevision,
    string WorkoutName,
    int Revision,
    WorkoutSessionStatus Status,
    DateTimeOffset StartedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? FinishedAt,
    string? Notes,
    IReadOnlyList<WorkoutSessionExerciseResponse> Exercises);

public sealed record WorkoutSessionExerciseResponse(
    Guid ExerciseId,
    int Position,
    string ExerciseName,
    ExerciseTrackingMode TrackingMode,
    IReadOnlyList<MuscleGroup> PrimaryMuscles,
    int PlannedSets,
    int? MinimumRepetitions,
    int? MaximumRepetitions,
    decimal? TargetLoadKilograms,
    int? TargetDurationSeconds,
    decimal? TargetDistanceMetres,
    bool IsSkipped,
    string? Notes,
    IReadOnlyList<WorkoutSessionSetResponse> Sets);

public sealed record WorkoutSessionSetResponse(
    Guid SetId,
    int Position,
    bool IsCompleted,
    DateTimeOffset? CompletedAt,
    int? ActualRepetitions,
    decimal? ActualLoadKilograms,
    int? ActualDurationSeconds,
    decimal? ActualDistanceMetres);

using FitnessCoach.Api.Features.Exercises;

namespace FitnessCoach.Api.Features.Workouts;

public sealed record CreateWorkoutRequest(
    string Name,
    IReadOnlyList<WorkoutExerciseRequest> Exercises);

public sealed record UpdateWorkoutRequest(
    string Name,
    int ExpectedRevision,
    IReadOnlyList<WorkoutExerciseRequest> Exercises);

public sealed record WorkoutExerciseRequest(
    Guid ExerciseId,
    int PlannedSets,
    int? MinimumRepetitions,
    int? MaximumRepetitions,
    decimal? TargetLoadKilograms,
    int? TargetDurationSeconds,
    decimal? TargetDistanceMetres);

public sealed record WorkoutListResponse(
    IReadOnlyList<WorkoutSummaryResponse> Items,
    int? NextOffset);

public sealed record WorkoutSummaryResponse(
    Guid Id,
    string Name,
    int ExerciseCount,
    int PlannedSetCount,
    int Revision,
    DateTimeOffset UpdatedAt);

public sealed record WorkoutDetailResponse(
    Guid Id,
    Guid ProfileId,
    string Name,
    int Revision,
    IReadOnlyList<WorkoutExerciseResponse> Exercises,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record WorkoutExerciseResponse(
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
    decimal? TargetDistanceMetres);

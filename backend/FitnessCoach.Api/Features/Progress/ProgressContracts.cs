using FitnessCoach.Api.Features.Exercises;

namespace FitnessCoach.Api.Features.Progress;

public sealed record ProgressOverviewResponse(
    DateTimeOffset PeriodStart,
    DateTimeOffset PeriodEnd,
    int CompletedWorkoutCount,
    int CompletedSetCount,
    int TotalWorkoutDurationSeconds,
    IReadOnlyList<RecordedExerciseSummaryResponse> RecordedExercises);

public sealed record RecordedExerciseSummaryResponse(
    Guid ExerciseId,
    string ExerciseName,
    ExerciseTrackingMode TrackingMode,
    int AppearanceCount,
    DateTimeOffset LastPerformedAt);

public sealed record ExercisePerformanceResponse(
    Guid ExerciseId,
    string ExerciseName,
    ExerciseTrackingMode TrackingMode,
    IReadOnlyList<ExercisePerformanceAppearanceResponse> Appearances);

public sealed record ExercisePerformanceAppearanceResponse(
    Guid SessionId,
    string WorkoutName,
    DateTimeOffset PerformedAt,
    IReadOnlyList<RecordedSetResponse> Sets);

public sealed record RecordedSetResponse(
    int Position,
    int? ActualRepetitions,
    decimal? ActualLoadKilograms,
    int? ActualDurationSeconds,
    decimal? ActualDistanceMetres);

using FitnessCoach.Api.Domain;

namespace FitnessCoach.Api.Features.Exercises;

public sealed record ExerciseSearchResponse(
    IReadOnlyList<ExerciseSummaryResponse> Items,
    int? NextOffset);

public sealed record ExerciseSummaryResponse(
    Guid Id,
    string Slug,
    string Name,
    ExerciseCategory Category,
    ExerciseMovementPattern MovementPattern,
    ExerciseTrackingMode TrackingMode,
    IReadOnlyList<EquipmentType> RequiredEquipment,
    IReadOnlyList<MuscleGroup> PrimaryMuscles);

public sealed record ExerciseDetailResponse(
    Guid Id,
    string Slug,
    string Name,
    ExerciseCategory Category,
    ExerciseMovementPattern MovementPattern,
    ExerciseTrackingMode TrackingMode,
    IReadOnlyList<EquipmentType> RequiredEquipment,
    IReadOnlyList<MuscleGroup> PrimaryMuscles,
    IReadOnlyList<MuscleGroup> SecondaryMuscles,
    string Setup,
    string Execution,
    string Safety);

namespace FitnessCoach.Api.Features.Exercises;

public enum ExerciseCategory
{
    Strength,
    Cardio,
}

public enum ExerciseMovementPattern
{
    Squat,
    Hinge,
    Lunge,
    HorizontalPush,
    VerticalPush,
    HorizontalPull,
    VerticalPull,
    Carry,
    CoreStability,
    ElbowFlexion,
    ElbowExtension,
    CalfRaise,
    Locomotion,
}

public enum MuscleGroup
{
    Quadriceps,
    Hamstrings,
    Glutes,
    Calves,
    Chest,
    Back,
    Shoulders,
    Biceps,
    Triceps,
    Forearms,
    Core,
}

public enum ExerciseTrackingMode
{
    Repetitions,
    RepetitionsAndLoad,
    Duration,
    DistanceAndDuration,
    DistanceDurationAndLoad,
}

internal enum MuscleRole
{
    Primary,
    Secondary,
}

internal enum ContentReviewStatus
{
    RequiresQualifiedReview,
    QualifiedReviewComplete,
}

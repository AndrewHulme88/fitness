namespace FitnessCoach.Api.Features.Workouts;

internal sealed class WorkoutPlan
{
    private WorkoutPlan()
    {
    }

    private WorkoutPlan(
        Guid id,
        Guid profileId,
        string name,
        DateTimeOffset createdAt)
    {
        Id = id;
        ProfileId = profileId;
        Name = name;
        Revision = 1;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid ProfileId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public int Revision { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public ICollection<WorkoutPlanExercise> Exercises { get; } = [];

    public static WorkoutPlan Create(
        Guid profileId,
        string name,
        IReadOnlyList<WorkoutExerciseInput> exercises,
        DateTimeOffset createdAt)
    {
        var workout = new WorkoutPlan(Guid.NewGuid(), profileId, name, createdAt);
        workout.SynchronizeExercises(exercises);
        return workout;
    }

    public void Update(
        string name,
        IReadOnlyList<WorkoutExerciseInput> exercises,
        DateTimeOffset updatedAt)
    {
        Name = name;
        Revision++;
        UpdatedAt = updatedAt;
        SynchronizeExercises(exercises);
    }

    private void SynchronizeExercises(IReadOnlyList<WorkoutExerciseInput> desiredExercises)
    {
        var desiredIds = desiredExercises.Select(item => item.ExerciseId).ToHashSet();

        foreach (var exercise in Exercises
                     .Where(item => !desiredIds.Contains(item.ExerciseId))
                     .ToArray())
        {
            Exercises.Remove(exercise);
        }

        for (var position = 0; position < desiredExercises.Count; position++)
        {
            var desired = desiredExercises[position];
            var existing = Exercises.SingleOrDefault(item =>
                item.ExerciseId == desired.ExerciseId);

            if (existing is null)
            {
                Exercises.Add(new WorkoutPlanExercise(Id, desired, position));
            }
            else
            {
                existing.Update(desired, position);
            }
        }
    }
}

internal sealed class WorkoutPlanExercise
{
    private WorkoutPlanExercise()
    {
    }

    public WorkoutPlanExercise(
        Guid workoutPlanId,
        WorkoutExerciseInput source,
        int position)
    {
        WorkoutPlanId = workoutPlanId;
        ExerciseId = source.ExerciseId;
        Update(source, position);
    }

    public Guid WorkoutPlanId { get; private set; }

    public Guid ExerciseId { get; private set; }

    public int Position { get; private set; }

    public int PlannedSets { get; private set; }

    public int? MinimumRepetitions { get; private set; }

    public int? MaximumRepetitions { get; private set; }

    public decimal? TargetLoadKilograms { get; private set; }

    public int? TargetDurationSeconds { get; private set; }

    public decimal? TargetDistanceMetres { get; private set; }

    public void Update(WorkoutExerciseInput source, int position)
    {
        Position = position;
        PlannedSets = source.PlannedSets;
        MinimumRepetitions = source.MinimumRepetitions;
        MaximumRepetitions = source.MaximumRepetitions;
        TargetLoadKilograms = source.TargetLoadKilograms;
        TargetDurationSeconds = source.TargetDurationSeconds;
        TargetDistanceMetres = source.TargetDistanceMetres;
    }
}

internal sealed record WorkoutExerciseInput(
    Guid ExerciseId,
    int PlannedSets,
    int? MinimumRepetitions,
    int? MaximumRepetitions,
    decimal? TargetLoadKilograms,
    int? TargetDurationSeconds,
    decimal? TargetDistanceMetres);

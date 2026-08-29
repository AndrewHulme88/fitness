using FitnessCoach.Api.Features.Exercises;
using FitnessCoach.Api.Features.Workouts;

namespace FitnessCoach.Api.Features.Sessions;

internal sealed class WorkoutSession
{
    private WorkoutSession()
    {
    }

    private WorkoutSession(
        Guid id,
        WorkoutPlan workout,
        IReadOnlyDictionary<Guid, Exercise> exercises,
        DateTimeOffset startedAt)
    {
        Id = id;
        ProfileId = workout.ProfileId;
        WorkoutPlanId = workout.Id;
        WorkoutPlanRevision = workout.Revision;
        WorkoutName = workout.Name;
        Revision = 1;
        Status = WorkoutSessionStatus.Active;
        StartedAt = startedAt;
        UpdatedAt = startedAt;

        foreach (var planned in workout.Exercises.OrderBy(item => item.Position))
        {
            var exercise = exercises[planned.ExerciseId];
            Exercises.Add(new WorkoutSessionExercise(Id, planned, exercise));
        }
    }

    public Guid Id { get; private set; }
    public Guid ProfileId { get; private set; }
    public Guid WorkoutPlanId { get; private set; }
    public int WorkoutPlanRevision { get; private set; }
    public string WorkoutName { get; private set; } = string.Empty;
    public int Revision { get; private set; }
    public WorkoutSessionStatus Status { get; private set; }
    public Guid? LastMutationId { get; private set; }
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset? FinishedAt { get; private set; }
    public DateTimeOffset? CorrectedAt { get; private set; }
    public string? Notes { get; private set; }
    public ICollection<WorkoutSessionExercise> Exercises { get; } = [];

    public static WorkoutSession Start(
        Guid id,
        WorkoutPlan workout,
        IReadOnlyDictionary<Guid, Exercise> exercises,
        DateTimeOffset startedAt) => new(id, workout, exercises, startedAt);

    public void Update(
        UpdateWorkoutSessionInput input,
        DateTimeOffset updatedAt)
    {
        Status = input.Status;
        FinishedAt = input.FinishedAt;
        Notes = input.Notes;
        LastMutationId = input.ClientMutationId;
        Revision++;
        UpdatedAt = updatedAt;

        foreach (var desiredExercise in input.Exercises)
        {
            Exercises.Single(item => item.ExerciseId == desiredExercise.ExerciseId)
                .Update(desiredExercise);
        }
    }

    public void Correct(
        CorrectWorkoutSessionInput input,
        DateTimeOffset correctedAt)
    {
        Notes = input.Notes;
        Revision++;
        UpdatedAt = correctedAt;
        CorrectedAt = correctedAt;

        foreach (var desiredExercise in input.Exercises)
        {
            Exercises.Single(item => item.ExerciseId == desiredExercise.ExerciseId)
                .Update(desiredExercise);
        }
    }
}

internal sealed class WorkoutSessionExercise
{
    private WorkoutSessionExercise()
    {
    }

    public WorkoutSessionExercise(
        Guid sessionId,
        WorkoutPlanExercise planned,
        Exercise exercise)
    {
        WorkoutSessionId = sessionId;
        ExerciseId = planned.ExerciseId;
        Position = planned.Position;
        ExerciseName = exercise.Name;
        TrackingMode = exercise.TrackingMode;
        PrimaryMuscles = exercise.Muscles
            .Where(item => item.Role == MuscleRole.Primary)
            .Select(item => item.Muscle)
            .Order()
            .Select(item => item.ToString())
            .ToArray();
        PlannedSets = planned.PlannedSets;
        MinimumRepetitions = planned.MinimumRepetitions;
        MaximumRepetitions = planned.MaximumRepetitions;
        TargetLoadKilograms = planned.TargetLoadKilograms;
        TargetDurationSeconds = planned.TargetDurationSeconds;
        TargetDistanceMetres = planned.TargetDistanceMetres;

        for (var position = 0; position < PlannedSets; position++)
        {
            Sets.Add(WorkoutSessionSet.Create(sessionId, ExerciseId, position));
        }
    }

    public Guid WorkoutSessionId { get; private set; }
    public Guid ExerciseId { get; private set; }
    public int Position { get; private set; }
    public string ExerciseName { get; private set; } = string.Empty;
    public ExerciseTrackingMode TrackingMode { get; private set; }
    public string[] PrimaryMuscles { get; private set; } = [];
    public int PlannedSets { get; private set; }
    public int? MinimumRepetitions { get; private set; }
    public int? MaximumRepetitions { get; private set; }
    public decimal? TargetLoadKilograms { get; private set; }
    public int? TargetDurationSeconds { get; private set; }
    public decimal? TargetDistanceMetres { get; private set; }
    public bool IsSkipped { get; private set; }
    public string? Notes { get; private set; }
    public ICollection<WorkoutSessionSet> Sets { get; } = [];

    public void Update(WorkoutSessionExerciseInput input)
    {
        IsSkipped = input.IsSkipped;
        Notes = input.Notes;

        var desiredIds = input.Sets.Select(item => item.SetId).ToHashSet();
        foreach (var existing in Sets.Where(item => !desiredIds.Contains(item.Id)).ToArray())
        {
            Sets.Remove(existing);
        }

        for (var position = 0; position < input.Sets.Count; position++)
        {
            var desired = input.Sets[position];
            var existing = Sets.SingleOrDefault(item => item.Id == desired.SetId);
            if (existing is null)
            {
                Sets.Add(WorkoutSessionSet.Create(WorkoutSessionId, ExerciseId, position, desired));
            }
            else
            {
                existing.Update(position, desired);
            }
        }
    }
}

internal sealed class WorkoutSessionSet
{
    private WorkoutSessionSet()
    {
    }

    private WorkoutSessionSet(Guid id, Guid sessionId, Guid exerciseId, int position)
    {
        Id = id;
        WorkoutSessionId = sessionId;
        ExerciseId = exerciseId;
        Position = position;
    }

    public Guid Id { get; private set; }
    public Guid WorkoutSessionId { get; private set; }
    public Guid ExerciseId { get; private set; }
    public int Position { get; private set; }
    public bool IsCompleted { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public int? ActualRepetitions { get; private set; }
    public decimal? ActualLoadKilograms { get; private set; }
    public int? ActualDurationSeconds { get; private set; }
    public decimal? ActualDistanceMetres { get; private set; }

    public static WorkoutSessionSet Create(Guid sessionId, Guid exerciseId, int position) =>
        new(Guid.NewGuid(), sessionId, exerciseId, position);

    public static WorkoutSessionSet Create(
        Guid sessionId,
        Guid exerciseId,
        int position,
        WorkoutSessionSetInput input)
    {
        var item = new WorkoutSessionSet(input.SetId, sessionId, exerciseId, position);
        item.Update(position, input);
        return item;
    }

    public void Update(int position, WorkoutSessionSetInput input)
    {
        Position = position;
        IsCompleted = input.IsCompleted;
        CompletedAt = input.CompletedAt;
        ActualRepetitions = input.ActualRepetitions;
        ActualLoadKilograms = input.ActualLoadKilograms;
        ActualDurationSeconds = input.ActualDurationSeconds;
        ActualDistanceMetres = input.ActualDistanceMetres;
    }
}

internal sealed record UpdateWorkoutSessionInput(
    Guid ClientMutationId,
    WorkoutSessionStatus Status,
    DateTimeOffset? FinishedAt,
    string? Notes,
    IReadOnlyList<WorkoutSessionExerciseInput> Exercises);

internal sealed record CorrectWorkoutSessionInput(
    string? Notes,
    IReadOnlyList<WorkoutSessionExerciseInput> Exercises);

internal sealed record WorkoutSessionExerciseInput(
    Guid ExerciseId,
    bool IsSkipped,
    string? Notes,
    IReadOnlyList<WorkoutSessionSetInput> Sets);

internal sealed record WorkoutSessionSetInput(
    Guid SetId,
    bool IsCompleted,
    DateTimeOffset? CompletedAt,
    int? ActualRepetitions,
    decimal? ActualLoadKilograms,
    int? ActualDurationSeconds,
    decimal? ActualDistanceMetres);

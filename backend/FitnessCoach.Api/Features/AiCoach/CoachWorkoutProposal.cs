using FitnessCoach.Api.Features.Workouts;

namespace FitnessCoach.Api.Features.AiCoach;

internal sealed class CoachWorkoutProposal
{
    private CoachWorkoutProposal() { }

    public CoachWorkoutProposal(Guid profileId, AiCoachWorkoutProposal proposal, DateTimeOffset createdAt)
    {
        Id = Guid.NewGuid(); ProfileId = profileId; WorkoutId = proposal.WorkoutId;
        ExpectedRevision = proposal.ExpectedRevision; Rationale = proposal.Rationale;
        Name = proposal.Name; Exercises = proposal.Exercises.ToArray(); CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }
    public Guid ProfileId { get; private set; }
    public Guid WorkoutId { get; private set; }
    public int ExpectedRevision { get; private set; }
    public string Rationale { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public WorkoutExerciseRequest[] Exercises { get; private set; } = [];
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? ConfirmedAt { get; private set; }

    public void Confirm(DateTimeOffset confirmedAt) => ConfirmedAt = confirmedAt;
}

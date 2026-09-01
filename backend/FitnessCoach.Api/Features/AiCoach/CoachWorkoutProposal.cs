using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

using FitnessCoach.Api.Features.Workouts;

namespace FitnessCoach.Api.Features.AiCoach;

internal sealed class CoachWorkoutProposal
{
    private CoachWorkoutProposal() { }

    public CoachWorkoutProposal(Guid profileId, AiCoachWorkoutProposal proposal, DateTimeOffset createdAt)
    {
        Id = Guid.NewGuid(); ProfileId = profileId; WorkoutId = proposal.WorkoutId;
        ExpectedRevision = proposal.ExpectedRevision; Rationale = proposal.Rationale;
        Name = proposal.Name;
        ExercisesJson = JsonSerializer.Serialize(proposal.Exercises);
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }
    public Guid ProfileId { get; private set; }
    public Guid WorkoutId { get; private set; }
    public int ExpectedRevision { get; private set; }
    public string Rationale { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string ExercisesJson { get; private set; } = "[]";
    [NotMapped]
    public WorkoutExerciseRequest[] Exercises => JsonSerializer.Deserialize<WorkoutExerciseRequest[]>(ExercisesJson) ?? [];
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? ConfirmedAt { get; private set; }

    public bool IsConfirmed => ConfirmedAt is not null;

    public void Confirm(DateTimeOffset confirmedAt)
    {
        if (ConfirmedAt is not null)
        {
            throw new InvalidOperationException("A proposal can be confirmed only once.");
        }

        ConfirmedAt = confirmedAt;
    }
}

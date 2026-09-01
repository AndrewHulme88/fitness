using FitnessCoach.Api.Features.Workouts;

namespace FitnessCoach.Api.Features.AiCoach;

public sealed record AiCoachWorkoutProposal(
    Guid WorkoutId,
    int ExpectedRevision,
    string Rationale,
    string Name,
    IReadOnlyList<WorkoutExerciseRequest> Exercises);

public sealed record AiCoachProposalResponse(
    Guid Id,
    Guid WorkoutId,
    int ExpectedRevision,
    string Rationale,
    string Name,
    IReadOnlyList<WorkoutExerciseRequest> Exercises,
    DateTimeOffset CreatedAt);

public sealed record ConfirmAiCoachProposalRequest
{
    public required Guid ProposalId { get; init; }
}

using FitnessCoach.Api.Features.Exercises;
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
    IReadOnlyList<AiCoachProposalChangeResponse> Changes,
    DateTimeOffset CreatedAt);

public enum AiCoachProposalChangeKind
{
    Addition,
    Removal,
    Substitution,
    PrescriptionChange,
}

public sealed record AiCoachProposalExerciseResponse(
    Guid ExerciseId,
    string Name,
    ExerciseTrackingMode TrackingMode,
    int PlannedSets,
    int? MinimumRepetitions,
    int? MaximumRepetitions,
    decimal? TargetLoadKilograms,
    int? TargetDurationSeconds,
    decimal? TargetDistanceMetres);

public sealed record AiCoachProposalChangeResponse(
    AiCoachProposalChangeKind Kind,
    AiCoachProposalExerciseResponse? Current,
    AiCoachProposalExerciseResponse? Proposed);

public sealed record ConfirmAiCoachProposalRequest
{
    public required Guid ProposalId { get; init; }
}

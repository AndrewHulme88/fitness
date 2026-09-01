namespace FitnessCoach.Api.Features.AiCoach;

public sealed record CoachConversationResponse(
    Guid Id,
    IReadOnlyList<CoachMessageResponse> Messages,
    IReadOnlyList<AiCoachProposalResponse> Proposals);

public sealed record CoachMessageResponse(
    Guid Id,
    CoachMessageRoleResponse Role,
    string Content,
    AiCoachResponseKind? ResponseKind,
    IReadOnlyList<string> ContextSources,
    DateTimeOffset CreatedAt);

public enum CoachMessageRoleResponse
{
    User,
    Coach,
}

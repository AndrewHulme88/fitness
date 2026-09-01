using FitnessCoach.Api.Domain;
using FitnessCoach.Api.Features.Profiles;

namespace FitnessCoach.Api.Features.AiCoach;

public sealed record AskAiCoachRequest
{
    public required string Question { get; init; }
}

public sealed record AiCoachResponse(
    AiCoachResponseKind Kind,
    string Message,
    IReadOnlyList<string>? ContextSources = null,
    AiCoachWorkoutProposal? Proposal = null);

public enum AiCoachResponseKind
{
    Advice,
    SafetyLimited,
    Unavailable,
}

internal sealed record AiCoachApprovedContext(
    IReadOnlyList<TrainingGoal> Goals,
    TrainingExperience Experience,
    IReadOnlyList<EquipmentType> AvailableEquipment,
    UnitSystem UnitSystem,
    IReadOnlyList<AiCoachContextFact>? Facts = null,
    IReadOnlyList<AiCoachConversationTurn>? Conversation = null);

internal sealed record AiCoachContextFact(string Source, string Summary);

internal sealed record AiCoachConversationTurn(string Role, string Content);

internal sealed record AiCoachProviderRequest(
    string PromptVersion,
    AiCoachApprovedContext Context,
    string Question,
    int MaximumOutputCharacters,
    string SafetyIdentifier);

internal sealed record AiCoachProviderResponse(
    string? Message,
    AiCoachTokenUsage Usage,
    AiCoachWorkoutProposal? Proposal = null);

internal sealed record AiCoachTokenUsage(int InputTokens, int OutputTokens)
{
    public static AiCoachTokenUsage None { get; } = new(0, 0);
}

internal sealed record AiCoachUsageRecord(
    string Provider,
    string PromptVersion,
    AiCoachResponseKind Outcome,
    TimeSpan Duration,
    AiCoachTokenUsage Usage);

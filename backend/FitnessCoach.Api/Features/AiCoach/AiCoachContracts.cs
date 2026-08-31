using FitnessCoach.Api.Domain;
using FitnessCoach.Api.Features.Profiles;

namespace FitnessCoach.Api.Features.AiCoach;

public sealed record AskAiCoachRequest
{
    public required string Question { get; init; }
}

public sealed record AiCoachResponse(AiCoachResponseKind Kind, string Message);

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
    UnitSystem UnitSystem);

internal sealed record AiCoachProviderRequest(
    string PromptVersion,
    AiCoachApprovedContext Context,
    string Question,
    int MaximumOutputCharacters);

internal sealed record AiCoachProviderResponse(
    string? Message,
    AiCoachTokenUsage Usage);

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

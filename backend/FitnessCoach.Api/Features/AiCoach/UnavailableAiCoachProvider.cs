namespace FitnessCoach.Api.Features.AiCoach;

internal sealed class UnavailableAiCoachProvider : IAiCoachProvider
{
    public string Name => "not-configured";

    public Task<AiCoachProviderResponse> RespondAsync(
        AiCoachProviderRequest request,
        CancellationToken cancellationToken) => throw new NotSupportedException(
            "No AI coach provider is configured.");
}

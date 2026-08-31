namespace FitnessCoach.Api.Features.AiCoach;

internal sealed class DevelopmentFakeAiCoachProvider : IAiCoachProvider
{
    public string Name => "development-fake";

    public Task<AiCoachProviderResponse> RespondAsync(
        AiCoachProviderRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var profile = request.Context.Facts?.FirstOrDefault(item => item.Source == "Training profile")?.Summary;
        var message = profile is null
            ? "This is a deterministic development response. Configure a live provider before relying on coach advice."
            : $"This is a deterministic development response. Your approved context is: {profile}";
        return Task.FromResult(new AiCoachProviderResponse(message, new AiCoachTokenUsage(24, 20)));
    }
}

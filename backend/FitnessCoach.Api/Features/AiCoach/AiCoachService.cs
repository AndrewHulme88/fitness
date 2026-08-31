using System.Diagnostics;

namespace FitnessCoach.Api.Features.AiCoach;

internal interface IAiCoachProvider
{
    string Name { get; }

    Task<AiCoachProviderResponse> RespondAsync(
        AiCoachProviderRequest request,
        CancellationToken cancellationToken);
}

internal interface IAiCoachContextAssembler
{
    Task<AiCoachApprovedContext?> AssembleAsync(
        Guid profileId,
        string question,
        CancellationToken cancellationToken);
}

internal interface IAiCoachUsageRecorder
{
    void Record(AiCoachUsageRecord record);
}

internal sealed class AiCoachService(
    IAiCoachContextAssembler contextAssembler,
    IAiCoachProvider provider,
    IAiCoachUsageRecorder usageRecorder)
{
    private const string PromptVersion = "v1";
    private const int MaximumQuestionLength = 1_000;
    private const int MaximumOutputCharacters = 2_000;
    private static readonly TimeSpan ProviderTimeout = TimeSpan.FromSeconds(15);

    public async Task<AiCoachResponse> AskAsync(
        Guid profileId,
        AskAiCoachRequest request,
        CancellationToken cancellationToken) => await AskAsync(profileId, request, [], cancellationToken);

    public async Task<AiCoachResponse> AskAsync(
        Guid profileId,
        AskAiCoachRequest request,
        IReadOnlyList<AiCoachConversationTurn> conversation,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var question = request.Question?.Trim();
        if (string.IsNullOrWhiteSpace(question) || question.Length > MaximumQuestionLength)
        {
            Record(provider.Name, AiCoachResponseKind.Unavailable, TimeSpan.Zero, AiCoachTokenUsage.None);
            return new AiCoachResponse(
                AiCoachResponseKind.Unavailable,
                "I couldn’t process that question. Please try a shorter fitness question.");
        }

        var safetyResponse = AiCoachSafetyPreCheck.Evaluate(question);
        if (safetyResponse is not null)
        {
            Record(provider.Name, AiCoachResponseKind.SafetyLimited, TimeSpan.Zero, AiCoachTokenUsage.None);
            return safetyResponse;
        }

        var stopwatch = Stopwatch.StartNew();
        var usage = AiCoachTokenUsage.None;
        var outcome = AiCoachResponseKind.Unavailable;
        try
        {
            var context = await contextAssembler.AssembleAsync(profileId, question, cancellationToken);
            if (context is null)
            {
                return new AiCoachResponse(
                    AiCoachResponseKind.Unavailable,
                    "The coach is unavailable for this profile right now.");
            }

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(ProviderTimeout);
            AiCoachProviderResponse providerResponse;
            try
            {
                providerResponse = await provider.RespondAsync(
                    new AiCoachProviderRequest(
                        PromptVersion,
                        context with { Conversation = conversation },
                        question,
                        MaximumOutputCharacters),
                    timeout.Token);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return UnavailableResponse();
            }
            catch (Exception) when (!cancellationToken.IsCancellationRequested)
            {
                return UnavailableResponse();
            }

            usage = providerResponse.Usage;
            var advice = providerResponse.Message;
            if (advice is null || !IsValidAdvice(advice))
            {
                return UnavailableResponse();
            }

            outcome = AiCoachResponseKind.Advice;
            return new AiCoachResponse(
                AiCoachResponseKind.Advice,
                advice.Trim(),
                context.Facts?.Select(item => item.Source).ToArray() ?? []);
        }
        finally
        {
            stopwatch.Stop();
            Record(provider.Name, outcome, stopwatch.Elapsed, usage);
        }
    }

    private static bool IsValidAdvice(string? message) => !string.IsNullOrWhiteSpace(message)
        && message.Length <= MaximumOutputCharacters;

    private static AiCoachResponse UnavailableResponse() => new(
        AiCoachResponseKind.Unavailable,
        "The coach is unavailable right now. Your workouts and plans are still available.");

    private void Record(
        string providerName,
        AiCoachResponseKind outcome,
        TimeSpan duration,
        AiCoachTokenUsage usage) => usageRecorder.Record(
            new AiCoachUsageRecord(providerName, PromptVersion, outcome, duration, usage));
}

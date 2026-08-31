using Microsoft.Extensions.Logging;

namespace FitnessCoach.Api.Features.AiCoach;

internal sealed class LoggerAiCoachUsageRecorder(ILogger<LoggerAiCoachUsageRecorder> logger)
    : IAiCoachUsageRecorder
{
    private static readonly Action<ILogger, string, string, AiCoachResponseKind, double, int, int, Exception?>
        LogCoachCompleted = LoggerMessage.Define<string, string, AiCoachResponseKind, double, int, int>(
            LogLevel.Information,
            new EventId(1, "AiCoachCompleted"),
            "AI coach completed with provider {Provider}, prompt version {PromptVersion}, outcome {Outcome}, duration {DurationMs} ms, input tokens {InputTokens}, and output tokens {OutputTokens}.");

    public void Record(AiCoachUsageRecord record)
    {
        LogCoachCompleted(
            logger,
            record.Provider,
            record.PromptVersion,
            record.Outcome,
            record.Duration.TotalMilliseconds,
            record.Usage.InputTokens,
            record.Usage.OutputTokens,
            null);
    }
}

namespace FitnessCoach.Api.Features.AiCoach;

internal static class AiCoachSafetyPreCheck
{
    private static readonly string[] UrgentSignals =
    [
        "chest pain", "fainted", "fainting", "severe breathing", "sudden weakness",
    ];

    private static readonly string[] ProfessionalSupportSignals =
    [
        "acute pain", "severe pain", "unexplained pain", "head injury", "neck injury",
        "spinal injury", "post-operative", "post operative", "pregnan", "medication",
        "supplement dose", "purging", "starvation", "self-harm", "self harm", "diagnose",
        "diagnosis", "rehabilitation", "rehab",
    ];

    public static AiCoachResponse? Evaluate(string question)
    {
        if (ContainsSignal(question, UrgentSignals))
        {
            return new AiCoachResponse(
                AiCoachResponseKind.SafetyLimited,
                "I can’t assess urgent symptoms. Please stop exercising and seek urgent medical help or contact local emergency services.");
        }

        if (ContainsSignal(question, ProfessionalSupportSignals))
        {
            return new AiCoachResponse(
                AiCoachResponseKind.SafetyLimited,
                "I can’t provide guidance for that situation. Please avoid the potentially harmful activity and speak with an appropriately qualified health professional.");
        }

        return null;
    }

    private static bool ContainsSignal(string question, IEnumerable<string> signals) => signals.Any(
        signal => question.Contains(signal, StringComparison.OrdinalIgnoreCase));
}

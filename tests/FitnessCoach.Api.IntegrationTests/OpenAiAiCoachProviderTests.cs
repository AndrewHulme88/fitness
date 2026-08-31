using System.Text.Json;

using FitnessCoach.Api.Features.AiCoach;

namespace FitnessCoach.Api.IntegrationTests;

public sealed class OpenAiAiCoachProviderTests
{
    [Fact]
    public void ExtractOutputTextUsesNestedResponseContentWhenShortcutIsAbsent()
    {
        using var document = JsonDocument.Parse("""
            {"output":[{"type":"message","content":[{"type":"output_text","text":"Synthetic advice."}]}]}
            """);

        var text = OpenAiAiCoachProvider.ExtractOutputText(document.RootElement);

        Assert.Equal("Synthetic advice.", text);
    }
}

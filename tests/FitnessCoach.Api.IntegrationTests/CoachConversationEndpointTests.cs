using System.Net;
using System.Net.Http.Json;

using Microsoft.AspNetCore.Mvc.Testing;

namespace FitnessCoach.Api.IntegrationTests;

public sealed class CoachConversationEndpointTests : IClassFixture<PostgreSqlApiFixture>
{
    private static readonly string[] Goals = ["generalFitness"];
    private static readonly string[] Equipment = ["bodyweight"];
    private readonly PostgreSqlApiFixture fixture;

    public CoachConversationEndpointTests(PostgreSqlApiFixture fixture) => this.fixture = fixture;

    [Fact]
    public async Task AuthenticatedOwnerCanRetainAndDeleteAReadOnlyCoachConversation()
    {
        using var factory = fixture.Factory.WithTestAuthentication();
        using var owner = CreateClient(factory, "coach-owner");
        using var other = CreateClient(factory, "coach-other");
        var profileId = await CreateProfileAsync(owner);

        using var send = await owner.PostAsJsonAsync(
            $"/profiles/{profileId}/coach/conversation/messages",
            new { question = "What does progressive overload mean?" },
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, send.StatusCode);
        var conversation = await send.Content.ReadFromJsonAsync<ConversationDocument>(
            TestContext.Current.CancellationToken);
        Assert.NotNull(conversation);
        Assert.Equal(2, conversation.Messages.Count);
        Assert.Equal("user", conversation.Messages[0].Role);
        Assert.Equal("coach", conversation.Messages[1].Role);
        Assert.Equal("advice", conversation.Messages[1].ResponseKind);

        using var hidden = await other.GetAsync(
            $"/profiles/{profileId}/coach/conversation", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, hidden.StatusCode);

        using var deleted = await owner.DeleteAsync(
            $"/profiles/{profileId}/coach/conversation", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        using var missing = await owner.GetAsync(
            $"/profiles/{profileId}/coach/conversation", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    private static HttpClient CreateClient(WebApplicationFactory<Program> factory, string subject)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.SubjectHeader, subject);
        return client;
    }

    private static async Task<Guid> CreateProfileAsync(HttpClient client)
    {
        using var response = await client.PostAsJsonAsync(
            "/profiles",
            new
            {
                goals = Goals,
                experience = "beginner",
                availableEquipment = Equipment,
                unitSystem = "metric",
            },
            TestContext.Current.CancellationToken);
        var profile = await response.Content.ReadFromJsonAsync<ProfileDocument>(
            TestContext.Current.CancellationToken);
        return profile?.Id ?? throw new InvalidOperationException("Expected a profile.");
    }

    private sealed record ProfileDocument(Guid Id);
    private sealed record ConversationDocument(IReadOnlyList<MessageDocument> Messages);
    private sealed record MessageDocument(string Role, string? ResponseKind);
}

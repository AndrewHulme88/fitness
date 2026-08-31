using System.Net;
using System.Net.Http.Json;

using FitnessCoach.Api.Features.AiCoach;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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

        using var followUp = await owner.PostAsJsonAsync(
            $"/profiles/{profileId}/coach/conversation/messages",
            new { question = "How should I warm up?" },
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, followUp.StatusCode);
        var updatedConversation = await followUp.Content.ReadFromJsonAsync<ConversationDocument>(
            TestContext.Current.CancellationToken);
        Assert.NotNull(updatedConversation);
        Assert.Equal(4, updatedConversation.Messages.Count);

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

    [Fact]
    public async Task SendingWhileTheConversationIsDeletedReturnsAConflictInsteadOfAnUnhandledError()
    {
        using var initialFactory = fixture.Factory.WithTestAuthentication();
        using var initialClient = CreateClient(initialFactory, "coach-delete-race");
        var profileId = await CreateProfileAsync(initialClient);
        using var initialSend = await initialClient.PostAsJsonAsync(
            $"/profiles/{profileId}/coach/conversation/messages",
            new { question = "What is progressive overload?" },
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, initialSend.StatusCode);

        var provider = new BlockingProvider();
        using var factory = initialFactory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IAiCoachProvider>();
                services.AddSingleton<IAiCoachProvider>(provider);
            }));
        using var sender = CreateClient(factory, "coach-delete-race");
        using var deleter = CreateClient(factory, "coach-delete-race");

        var sendTask = sender.PostAsJsonAsync(
            $"/profiles/{profileId}/coach/conversation/messages",
            new { question = "How should I warm up?" },
            TestContext.Current.CancellationToken);
        await provider.Started.Task.WaitAsync(TestContext.Current.CancellationToken);

        using var deleted = await deleter.DeleteAsync(
            $"/profiles/{profileId}/coach/conversation", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

        provider.Release.TrySetResult(true);
        using var send = await sendTask;
        Assert.Equal(HttpStatusCode.Conflict, send.StatusCode);
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

    private sealed class BlockingProvider : IAiCoachProvider
    {
        public TaskCompletionSource<bool> Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<bool> Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public string Name => "blocking-test";

        public async Task<AiCoachProviderResponse> RespondAsync(
            AiCoachProviderRequest request,
            CancellationToken cancellationToken)
        {
            Started.TrySetResult(true);
            await Release.Task.WaitAsync(cancellationToken);
            return new AiCoachProviderResponse(
                "Start with a short, gradual warm-up.", new AiCoachTokenUsage(1, 1));
        }
    }
}

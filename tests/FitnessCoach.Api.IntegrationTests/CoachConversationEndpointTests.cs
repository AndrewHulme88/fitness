using System.Net;
using System.Net.Http.Json;

using FitnessCoach.Api.Features.AiCoach;
using FitnessCoach.Api.Features.Exercises;
using FitnessCoach.Api.Features.Workouts;

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

    [Fact]
    public async Task ValidProposalIsPersistedAndUpdatesItsWorkoutOnlyAfterConfirmation()
    {
        using var initialFactory = fixture.Factory.WithTestAuthentication();
        using var initialClient = CreateClient(initialFactory, "coach-proposal");
        var profileId = await CreateProfileAsync(initialClient);
        var exerciseId = ExerciseCatalogueManifestLoader.Load().Exercises
            .Single(item => item.Slug == "barbell-bench-press").Id;
        var workout = await CreateWorkoutAsync(initialClient, profileId, exerciseId);
        var provider = new ProposalProvider(new AiCoachWorkoutProposal(
            workout.Id, workout.Revision, "A small, reviewable progression.", "Upper strength",
            [new WorkoutExerciseRequest(exerciseId, 3, 8, 10, 52.5m, null, null)]));
        using var factory = initialFactory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IAiCoachProvider>();
            services.AddSingleton<IAiCoachProvider>(provider);
        }));
        using var client = CreateClient(factory, "coach-proposal");

        using var sent = await client.PostAsJsonAsync(
            $"/profiles/{profileId}/coach/conversation/messages",
            new { question = "Please adjust my workout.", workoutId = workout.Id }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, sent.StatusCode);
        var conversation = await sent.Content.ReadFromJsonAsync<ConversationDocument>(TestContext.Current.CancellationToken);
        var proposal = Assert.Single(conversation?.Proposals ?? throw new InvalidOperationException("Expected proposal."));

        using var confirmation = await client.PostAsync(
            $"/profiles/{profileId}/coach/conversation/proposals/{proposal.Id}/confirm",
            null, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, confirmation.StatusCode);
        using var updatedResponse = await client.GetAsync(
            $"/profiles/{profileId}/workouts/{workout.Id}", TestContext.Current.CancellationToken);
        var updated = await updatedResponse.Content.ReadFromJsonAsync<WorkoutDocument>(TestContext.Current.CancellationToken);
        Assert.Equal(2, updated?.Revision);
        Assert.Equal(52.5m, Assert.Single(updated?.Exercises ?? []).TargetLoadKilograms);
    }

    [Fact]
    public async Task NamedWorkoutReviewUsesOnlyTheSelectedSnapshotAndReturnsAnExerciseDiff()
    {
        using var initialFactory = fixture.Factory.WithTestAuthentication();
        using var initialClient = CreateClient(initialFactory, "coach-named-review");
        var profileId = await CreateProfileAsync(initialClient);
        var catalogue = ExerciseCatalogueManifestLoader.Load().Exercises;
        var benchPressId = catalogue.Single(item => item.Slug == "barbell-bench-press").Id;
        var pushUpId = catalogue.Single(item => item.Slug == "push-up").Id;
        var workout = await CreateWorkoutAsync(initialClient, profileId, benchPressId);
        var provider = new CapturingProposalProvider(new AiCoachWorkoutProposal(
            workout.Id, workout.Revision, "A lower-equipment alternative.", workout.Name,
            [new WorkoutExerciseRequest(pushUpId, 3, 8, 10, null, null, null)]));
        using var factory = initialFactory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IAiCoachProvider>();
            services.AddSingleton<IAiCoachProvider>(provider);
        }));
        using var client = CreateClient(factory, "coach-named-review");

        using var sent = await client.PostAsJsonAsync(
            $"/profiles/{profileId}/coach/conversation/messages",
            new { question = "Review this named workout and suggest a conservative swap.", workoutId = workout.Id },
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, sent.StatusCode);
        var conversation = await sent.Content.ReadFromJsonAsync<ConversationDocument>(TestContext.Current.CancellationToken);
        var proposal = Assert.Single(conversation?.Proposals ?? throw new InvalidOperationException("Expected proposal."));
        var change = Assert.Single(proposal.Changes);
        Assert.Equal("substitution", change.Kind);
        Assert.Equal("Barbell Bench Press", change.Current?.Name);
        Assert.Equal("Push-Up", change.Proposed?.Name);
        Assert.Equal(workout.Id, provider.Request?.Context.Workout?.Id);
        Assert.Single(provider.Request?.Context.Workout?.Exercises ?? []);
    }

    [Fact]
    public async Task ProgressReviewRejectsCombinedOrUnboundedScopes()
    {
        using var factory = fixture.Factory.WithTestAuthentication();
        using var client = CreateClient(factory, "coach-progress-scope");
        var profileId = await CreateProfileAsync(client);

        using var combined = await client.PostAsJsonAsync(
            $"/profiles/{profileId}/coach/conversation/messages",
            new { question = "Review my progress.", progressExerciseId = Guid.NewGuid(), progressPeriodDays = 28 },
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, combined.StatusCode);

        using var unbounded = await client.PostAsJsonAsync(
            $"/profiles/{profileId}/coach/conversation/messages",
            new { question = "Review my progress.", progressPeriodDays = 90 },
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, unbounded.StatusCode);
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

    private static async Task<WorkoutDocument> CreateWorkoutAsync(HttpClient client, Guid profileId, Guid exerciseId)
    {
        using var response = await client.PostAsJsonAsync(
            $"/profiles/{profileId}/workouts",
            new
            {
                name = "Upper strength",
                exercises = new[]
                {
                    new
                    {
                        exerciseId, plannedSets = 3, minimumRepetitions = 8, maximumRepetitions = 10,
                        targetLoadKilograms = 50m, targetDurationSeconds = (int?)null, targetDistanceMetres = (decimal?)null,
                    },
                },
            }, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<WorkoutDocument>(TestContext.Current.CancellationToken)
            ?? throw new InvalidOperationException("Expected workout.");
    }

    private sealed record ProfileDocument(Guid Id);
    private sealed record ConversationDocument(
        IReadOnlyList<MessageDocument> Messages,
        IReadOnlyList<ProposalDocument> Proposals);
    private sealed record MessageDocument(string Role, string? ResponseKind);
    private sealed record ProposalDocument(Guid Id, IReadOnlyList<ProposalChangeDocument> Changes);
    private sealed record ProposalChangeDocument(string Kind, ProposalExerciseDocument? Current, ProposalExerciseDocument? Proposed);
    private sealed record ProposalExerciseDocument(string Name);
    private sealed record WorkoutDocument(Guid Id, int Revision, string Name, IReadOnlyList<WorkoutExerciseDocument> Exercises);
    private sealed record WorkoutExerciseDocument(decimal? TargetLoadKilograms);

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

    private sealed class ProposalProvider(AiCoachWorkoutProposal proposal) : IAiCoachProvider
    {
        public string Name => "proposal-test";

        public Task<AiCoachProviderResponse> RespondAsync(
            AiCoachProviderRequest request,
            CancellationToken cancellationToken) => Task.FromResult(
                new AiCoachProviderResponse(
                    "Here is a reviewable change.", new AiCoachTokenUsage(1, 1), proposal));
    }

    private sealed class CapturingProposalProvider(AiCoachWorkoutProposal proposal) : IAiCoachProvider
    {
        public string Name => "capturing-proposal-test";
        public AiCoachProviderRequest? Request { get; private set; }

        public Task<AiCoachProviderResponse> RespondAsync(
            AiCoachProviderRequest request,
            CancellationToken cancellationToken)
        {
            Request = request;
            return Task.FromResult(new AiCoachProviderResponse(
                "Here is a reviewable substitution.", new AiCoachTokenUsage(1, 1), proposal));
        }
    }
}

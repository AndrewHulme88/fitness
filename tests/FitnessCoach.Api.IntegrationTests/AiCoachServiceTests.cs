using FitnessCoach.Api.Domain;
using FitnessCoach.Api.Features.AiCoach;
using FitnessCoach.Api.Features.Profiles;

namespace FitnessCoach.Api.IntegrationTests;

public sealed class AiCoachServiceTests
{
    [Fact]
    public async Task SendsOnlyApprovedContextToProviderAndRecordsUsage()
    {
        var context = new AiCoachApprovedContext(
            [TrainingGoal.BuildStrength],
            TrainingExperience.Beginner,
            [EquipmentType.Dumbbells],
            UnitSystem.Metric);
        var contextAssembler = new FakeContextAssembler(context);
        var provider = new FakeProvider(new AiCoachProviderResponse(
            "Focus on a comfortable, repeatable progression.", new AiCoachTokenUsage(42, 18)));
        var usageRecorder = new RecordingUsageRecorder();
        var service = new AiCoachService(contextAssembler, provider, usageRecorder);

        var response = await service.AskAsync(
            Guid.NewGuid(),
            new AskAiCoachRequest { Question = "How should I approach my next dumbbell workout?" },
            TestContext.Current.CancellationToken);

        Assert.Equal(AiCoachResponseKind.Advice, response.Kind);
        Assert.Equal("Focus on a comfortable, repeatable progression.", response.Message);
        var providerRequest = Assert.IsType<AiCoachProviderRequest>(provider.Request);
        Assert.Equal("v2", providerRequest.PromptVersion);
        Assert.Equal(2_000, providerRequest.MaximumOutputCharacters);
        Assert.Matches("^[A-F0-9]{64}$", providerRequest.SafetyIdentifier);
        Assert.Equal(context.Goals, providerRequest.Context.Goals);
        Assert.Equal(context.Experience, providerRequest.Context.Experience);
        Assert.Equal(context.AvailableEquipment, providerRequest.Context.AvailableEquipment);
        Assert.Equal(context.UnitSystem, providerRequest.Context.UnitSystem);
        Assert.Empty(providerRequest.Context.Conversation ?? []);
        var usage = Assert.Single(usageRecorder.Records);
        Assert.Equal(AiCoachResponseKind.Advice, usage.Outcome);
        Assert.Equal(new AiCoachTokenUsage(42, 18), usage.Usage);
    }

    [Fact]
    public async Task HighRiskQuestionStopsBeforeContextAssemblyOrProviderCall()
    {
        var contextAssembler = new FakeContextAssembler(CreateContext());
        var provider = new FakeProvider(new AiCoachProviderResponse("Ignored.", AiCoachTokenUsage.None));
        var usageRecorder = new RecordingUsageRecorder();
        var service = new AiCoachService(contextAssembler, provider, usageRecorder);

        var response = await service.AskAsync(
            Guid.NewGuid(),
            new AskAiCoachRequest { Question = "Can you diagnose my severe pain?" },
            TestContext.Current.CancellationToken);

        Assert.Equal(AiCoachResponseKind.SafetyLimited, response.Kind);
        Assert.False(contextAssembler.WasCalled);
        Assert.Null(provider.Request);
        Assert.Equal(AiCoachResponseKind.SafetyLimited, Assert.Single(usageRecorder.Records).Outcome);
    }

    [Fact]
    public async Task RejectsMalformedProviderOutputAndReturnsSafeUnavailableState()
    {
        var provider = new FakeProvider(new AiCoachProviderResponse(" ", new AiCoachTokenUsage(12, 0)));
        var usageRecorder = new RecordingUsageRecorder();
        var service = new AiCoachService(new FakeContextAssembler(CreateContext()), provider, usageRecorder);

        var response = await service.AskAsync(
            Guid.NewGuid(),
            new AskAiCoachRequest { Question = "What does progressive overload mean?" },
            TestContext.Current.CancellationToken);

        Assert.Equal(AiCoachResponseKind.Unavailable, response.Kind);
        Assert.Equal(AiCoachResponseKind.Unavailable, Assert.Single(usageRecorder.Records).Outcome);
        Assert.Equal(new AiCoachTokenUsage(12, 0), usageRecorder.Records[0].Usage);
    }

    [Fact]
    public async Task PropagatesCallerCancellationToTheProvider()
    {
        var provider = new CancellingProvider();
        var service = new AiCoachService(
            new FakeContextAssembler(CreateContext()), provider, new RecordingUsageRecorder());
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.AskAsync(
            Guid.NewGuid(),
            new AskAiCoachRequest { Question = "What is a sensible warm-up?" },
            cancellation.Token));

        Assert.True(provider.ReceivedCancellation);
    }

    private static AiCoachApprovedContext CreateContext() => new(
        [TrainingGoal.GeneralFitness],
        TrainingExperience.Intermediate,
        [EquipmentType.Bodyweight],
        UnitSystem.Imperial);

    private sealed class FakeContextAssembler(AiCoachApprovedContext? context) : IAiCoachContextAssembler
    {
        public bool WasCalled { get; private set; }

        public Task<AiCoachApprovedContext?> AssembleAsync(
            Guid profileId,
            string question,
            Guid? workoutId,
            CancellationToken cancellationToken)
        {
            WasCalled = true;
            return Task.FromResult(context);
        }
    }

    private sealed class FakeProvider(AiCoachProviderResponse response) : IAiCoachProvider
    {
        public string Name => "fake";

        public AiCoachProviderRequest? Request { get; private set; }

        public Task<AiCoachProviderResponse> RespondAsync(
            AiCoachProviderRequest request,
            CancellationToken cancellationToken)
        {
            Request = request;
            return Task.FromResult(response);
        }
    }

    private sealed class CancellingProvider : IAiCoachProvider
    {
        public string Name => "fake";

        public bool ReceivedCancellation { get; private set; }

        public Task<AiCoachProviderResponse> RespondAsync(
            AiCoachProviderRequest request,
            CancellationToken cancellationToken)
        {
            ReceivedCancellation = cancellationToken.IsCancellationRequested;
            return Task.FromCanceled<AiCoachProviderResponse>(cancellationToken);
        }
    }

    private sealed class RecordingUsageRecorder : IAiCoachUsageRecorder
    {
        public List<AiCoachUsageRecord> Records { get; } = [];

        public void Record(AiCoachUsageRecord record) => Records.Add(record);
    }
}

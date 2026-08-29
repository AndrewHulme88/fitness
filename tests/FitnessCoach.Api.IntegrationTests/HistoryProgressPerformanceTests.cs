using System.Diagnostics;
using System.Net.Http.Json;

using FitnessCoach.Api.Features.Exercises;

using Microsoft.AspNetCore.Mvc.Testing;

namespace FitnessCoach.Api.IntegrationTests;

public sealed class HistoryProgressPerformanceTests : IClassFixture<PostgreSqlApiFixture>
{
    private const int SessionCount = 200;
    private const int WarmupCount = 5;
    private const int SampleCount = 30;
    private static readonly string[] BenchmarkGoals = ["buildStrength"];
    private static readonly string[] BenchmarkEquipment = ["bodyweight"];

    private readonly PostgreSqlApiFixture fixture;

    public HistoryProgressPerformanceTests(PostgreSqlApiFixture fixture)
    {
        this.fixture = fixture;
    }

    [Fact]
    [Trait("Category", "Performance")]
    public async Task BoundedHistoryAndProgressEndpointBaseline()
    {
        using var client = fixture.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost"),
        });
        var profileId = await CreateProfileAsync(client);
        var workoutId = await CreateWorkoutAsync(client, profileId);
        for (var index = 0; index < SessionCount; index++)
        {
            await CompleteSessionAsync(client, profileId, workoutId);
        }

        var historyPath = $"/profiles/{profileId}/workout-sessions/history?limit=20&offset=0";
        var progressPath = $"/profiles/{profileId}/progress";
        await WarmAsync(client, historyPath);
        await WarmAsync(client, progressPath);

        var history = await MeasureAsync(client, historyPath);
        var progress = await MeasureAsync(client, progressPath);

        TestContext.Current.TestOutputHelper?.WriteLine(
            "Dataset: {0} completed one-exercise/one-set sessions. "
            + "History median {1:F2} ms, p95 {2:F2} ms. "
            + "Progress median {3:F2} ms, p95 {4:F2} ms. "
            + "In-process ASP.NET test host with PostgreSQL Testcontainers; "
            + "30 sequential warm-cache samples after 5 warmups.",
            SessionCount,
            Median(history),
            Percentile95(history),
            Median(progress),
            Percentile95(progress));
    }

    private static async Task<double[]> MeasureAsync(HttpClient client, string path)
    {
        var samples = new double[SampleCount];
        for (var index = 0; index < samples.Length; index++)
        {
            var startedAt = Stopwatch.GetTimestamp();
            using var response = await client.GetAsync(
                path,
                TestContext.Current.CancellationToken);
            response.EnsureSuccessStatusCode();
            samples[index] = Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds;
        }
        Array.Sort(samples);
        return samples;
    }

    private static async Task WarmAsync(HttpClient client, string path)
    {
        for (var index = 0; index < WarmupCount; index++)
        {
            using var response = await client.GetAsync(
                path,
                TestContext.Current.CancellationToken);
            response.EnsureSuccessStatusCode();
        }
    }

    private static double Median(double[] sorted) =>
        (sorted[(sorted.Length / 2) - 1] + sorted[sorted.Length / 2]) / 2;

    private static double Percentile95(double[] sorted) =>
        sorted[(int)Math.Ceiling(sorted.Length * 0.95) - 1];

    private static async Task<Guid> CreateProfileAsync(HttpClient client)
    {
        using var response = await client.PostAsJsonAsync(
            "/profiles",
            new
            {
                goals = BenchmarkGoals,
                experience = "intermediate",
                availableEquipment = BenchmarkEquipment,
                unitSystem = "metric",
            },
            TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        var profile = await response.Content.ReadFromJsonAsync<ProfileDocument>(
            TestContext.Current.CancellationToken);
        return profile?.Id ?? throw new InvalidOperationException("Expected a profile.");
    }

    private static async Task<Guid> CreateWorkoutAsync(HttpClient client, Guid profileId)
    {
        var exerciseId = ExerciseCatalogueManifestLoader.Load().Exercises
            .Single(item => item.Slug == "push-up")
            .Id;
        using var response = await client.PostAsJsonAsync(
            $"/profiles/{profileId}/workouts",
            new
            {
                name = "Benchmark workout",
                exercises = new[]
                {
                    new
                    {
                        exerciseId,
                        plannedSets = 1,
                        minimumRepetitions = 8,
                        maximumRepetitions = 12,
                        targetLoadKilograms = (decimal?)null,
                        targetDurationSeconds = (int?)null,
                        targetDistanceMetres = (decimal?)null,
                    },
                },
            },
            TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        var workout = await response.Content.ReadFromJsonAsync<WorkoutDocument>(
            TestContext.Current.CancellationToken);
        return workout?.Id ?? throw new InvalidOperationException("Expected a workout.");
    }

    private static async Task CompleteSessionAsync(
        HttpClient client,
        Guid profileId,
        Guid workoutId)
    {
        var sessionId = Guid.NewGuid();
        using var startResponse = await client.PostAsJsonAsync(
            $"/profiles/{profileId}/workout-sessions",
            new { sessionId, workoutPlanId = workoutId },
            TestContext.Current.CancellationToken);
        startResponse.EnsureSuccessStatusCode();
        var session = await startResponse.Content.ReadFromJsonAsync<SessionDocument>(
            TestContext.Current.CancellationToken)
            ?? throw new InvalidOperationException("Expected a session.");
        var finishedAt = DateTimeOffset.UtcNow;
        using var finishResponse = await client.PutAsJsonAsync(
            $"/profiles/{profileId}/workout-sessions/{session.Id}",
            new
            {
                expectedRevision = session.Revision,
                clientMutationId = Guid.NewGuid(),
                status = "completed",
                finishedAt,
                notes = (string?)null,
                exercises = session.Exercises.Select(exercise => new
                {
                    exerciseId = exercise.ExerciseId,
                    isSkipped = false,
                    notes = (string?)null,
                    sets = exercise.Sets.Select(set => new
                    {
                        setId = set.SetId,
                        isCompleted = true,
                        completedAt = finishedAt,
                        actualRepetitions = 10,
                        actualLoadKilograms = (decimal?)null,
                        actualDurationSeconds = (int?)null,
                        actualDistanceMetres = (decimal?)null,
                    }),
                }),
            },
            TestContext.Current.CancellationToken);
        finishResponse.EnsureSuccessStatusCode();
    }

    private sealed record ProfileDocument(Guid Id);
    private sealed record WorkoutDocument(Guid Id);
    private sealed record SessionDocument(
        Guid Id,
        int Revision,
        SessionExerciseDocument[] Exercises);
    private sealed record SessionExerciseDocument(Guid ExerciseId, SessionSetDocument[] Sets);
    private sealed record SessionSetDocument(Guid SetId);
}

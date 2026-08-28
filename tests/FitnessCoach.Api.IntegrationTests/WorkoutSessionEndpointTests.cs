using System.Net;
using System.Net.Http.Json;

using FitnessCoach.Api.Features.Exercises;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;

namespace FitnessCoach.Api.IntegrationTests;

public sealed class WorkoutSessionEndpointTests : IClassFixture<PostgreSqlApiFixture>
{
    private static readonly string[] DefaultGoals = ["buildStrength"];
    private static readonly string[] DefaultEquipment = ["bodyweight", "barbell", "bench"];

    private readonly PostgreSqlApiFixture fixture;

    public WorkoutSessionEndpointTests(PostgreSqlApiFixture fixture)
    {
        this.fixture = fixture;
    }

    [Fact]
    public async Task StartSessionCreatesAnImmutableSnapshotWithEmptyActuals()
    {
        using var client = CreateClient();
        var profileId = await CreateProfileAsync(client);
        var workout = await CreateWorkoutAsync(client, profileId, "Original name");
        var sessionId = Guid.NewGuid();

        using var response = await StartSessionAsync(client, profileId, sessionId, workout.Id);
        var session = await ReadSessionAsync(response);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(sessionId, session.Id);
        Assert.Equal("Original name", session.WorkoutName);
        Assert.Equal(workout.Revision, session.WorkoutPlanRevision);
        Assert.Equal("active", session.Status);
        Assert.Equal(2, session.Exercises.Length);
        Assert.All(session.Exercises.SelectMany(item => item.Sets), set =>
        {
            Assert.False(set.IsCompleted);
            Assert.Null(set.ActualRepetitions);
            Assert.Null(set.ActualLoadKilograms);
        });

        using var editResponse = await client.PutAsJsonAsync(
            $"/profiles/{profileId}/workouts/{workout.Id}",
            new
            {
                name = "Changed template",
                expectedRevision = workout.Revision,
                exercises = new[]
                {
                    RepetitionPlan(ExerciseId("push-up"), 1, 12, 12, null),
                },
            },
            TestContext.Current.CancellationToken);
        editResponse.EnsureSuccessStatusCode();

        using var getResponse = await client.GetAsync(
            $"/profiles/{profileId}/workout-sessions/{sessionId}",
            TestContext.Current.CancellationToken);
        var persisted = await ReadSessionAsync(getResponse);
        Assert.Equal("Original name", persisted.WorkoutName);
        Assert.Equal(2, persisted.Exercises.Length);
    }

    [Fact]
    public async Task StartIsIdempotentAndOnlyOneSessionCanBeActive()
    {
        using var client = CreateClient();
        var profileId = await CreateProfileAsync(client);
        var workout = await CreateWorkoutAsync(client, profileId, "Workout");
        var sessionId = Guid.NewGuid();

        using var first = await StartSessionAsync(client, profileId, sessionId, workout.Id);
        using var retry = await StartSessionAsync(client, profileId, sessionId, workout.Id);
        using var second = await StartSessionAsync(client, profileId, Guid.NewGuid(), workout.Id);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, retry.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);

        using var activeResponse = await client.GetAsync(
            $"/profiles/{profileId}/workout-sessions/active",
            TestContext.Current.CancellationToken);
        Assert.Equal(sessionId, (await ReadSessionAsync(activeResponse)).Id);
    }

    [Fact]
    public async Task UpdatePersistsCanonicalActualsNotesAddedSetsAndSkippedExercise()
    {
        using var client = CreateClient();
        var profileId = await CreateProfileAsync(client);
        var workout = await CreateWorkoutAsync(client, profileId, "Mixed workout");
        var session = await StartAndReadAsync(client, profileId, workout.Id);
        var strength = session.Exercises[0];
        var duration = session.Exercises[1];
        var addedSetId = Guid.NewGuid();
        var completedAt = DateTimeOffset.UtcNow;

        using var response = await client.PutAsJsonAsync(
            $"/profiles/{profileId}/workout-sessions/{session.Id}",
            new
            {
                expectedRevision = session.Revision,
                clientMutationId = Guid.NewGuid(),
                status = "active",
                finishedAt = (DateTimeOffset?)null,
                notes = "Felt controlled.",
                exercises = new object[]
                {
                    new
                    {
                        exerciseId = strength.ExerciseId,
                        isSkipped = false,
                        notes = "Pause on the chest.",
                        sets = new object[]
                        {
                            CompletedSet(strength.Sets[0].SetId, completedAt, 9, 60m),
                            IncompleteSet(strength.Sets[1].SetId),
                            IncompleteSet(strength.Sets[2].SetId),
                            CompletedSet(addedSetId, completedAt, 8, 60m),
                        },
                    },
                    new
                    {
                        exerciseId = duration.ExerciseId,
                        isSkipped = true,
                        notes = (string?)null,
                        sets = duration.Sets.Select(set => IncompleteSet(set.SetId)).ToArray(),
                    },
                },
            },
            TestContext.Current.CancellationToken);
        var updated = await ReadSessionAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, updated.Revision);
        Assert.Equal("Felt controlled.", updated.Notes);
        Assert.Equal("Pause on the chest.", updated.Exercises[0].Notes);
        Assert.Equal(4, updated.Exercises[0].Sets.Length);
        Assert.Equal(addedSetId, updated.Exercises[0].Sets[3].SetId);
        Assert.Equal(9, updated.Exercises[0].Sets[0].ActualRepetitions);
        Assert.Equal(60m, updated.Exercises[0].Sets[0].ActualLoadKilograms);
        Assert.True(updated.Exercises[1].IsSkipped);
    }

    [Fact]
    public async Task CompletedSetsRequireTheCanonicalValuesForAllTrackingModes()
    {
        using var client = CreateClient();
        var profileId = await CreateProfileAsync(client);
        var workout = await CreateWorkoutWithPlansAsync(
            client,
            profileId,
            "Every tracking mode",
            [
                RepetitionPlan(ExerciseId("push-up"), 1, 10, 12, null),
                RepetitionPlan(ExerciseId("barbell-bench-press"), 1, 8, 10, 50m),
                DurationPlan(ExerciseId("front-plank"), 1, 45),
                DistancePlan(ExerciseId("rowing-machine"), 1, 1_000m, 300),
                CarryPlan(ExerciseId("dumbbell-farmer-carry"), 1, 30m, 60, 20m),
            ]);
        var session = await StartAndReadAsync(client, profileId, workout.Id);
        var completedAt = DateTimeOffset.UtcNow;

        using var response = await client.PutAsJsonAsync(
            $"/profiles/{profileId}/workout-sessions/{session.Id}",
            new
            {
                expectedRevision = session.Revision,
                clientMutationId = Guid.NewGuid(),
                status = "active",
                finishedAt = (DateTimeOffset?)null,
                notes = (string?)null,
                exercises = session.Exercises.Select(exercise => new
                {
                    exerciseId = exercise.ExerciseId,
                    isSkipped = false,
                    notes = (string?)null,
                    sets = new[] { ActualForMode(exercise, completedAt) },
                }).ToArray(),
            },
            TestContext.Current.CancellationToken);
        var updated = await ReadSessionAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.All(updated.Exercises, exercise => Assert.True(exercise.Sets[0].IsCompleted));
        Assert.Equal(12, updated.Exercises[0].Sets[0].ActualRepetitions);
        Assert.Equal(50m, updated.Exercises[1].Sets[0].ActualLoadKilograms);
        Assert.Equal(45, updated.Exercises[2].Sets[0].ActualDurationSeconds);
        Assert.Equal(1_000m, updated.Exercises[3].Sets[0].ActualDistanceMetres);
        Assert.Equal(20m, updated.Exercises[4].Sets[0].ActualLoadKilograms);
    }

    [Fact]
    public async Task RepeatedMutationIsIdempotentAndStaleDifferentMutationConflicts()
    {
        using var client = CreateClient();
        var profileId = await CreateProfileAsync(client);
        var workout = await CreateWorkoutAsync(client, profileId, "Workout");
        var session = await StartAndReadAsync(client, profileId, workout.Id);
        var mutationId = Guid.NewGuid();
        var request = UpdateRequest(session, mutationId);

        using var first = await client.PutAsJsonAsync(
            $"/profiles/{profileId}/workout-sessions/{session.Id}",
            request,
            TestContext.Current.CancellationToken);
        using var retry = await client.PutAsJsonAsync(
            $"/profiles/{profileId}/workout-sessions/{session.Id}",
            request,
            TestContext.Current.CancellationToken);
        using var stale = await client.PutAsJsonAsync(
            $"/profiles/{profileId}/workout-sessions/{session.Id}",
            UpdateRequest(session, Guid.NewGuid()),
            TestContext.Current.CancellationToken);

        Assert.Equal(2, (await ReadSessionAsync(first)).Revision);
        Assert.Equal(2, (await ReadSessionAsync(retry)).Revision);
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
    }

    [Fact]
    public async Task UpdateRejectsMissingModeValuesAndChangedSnapshot()
    {
        using var client = CreateClient();
        var profileId = await CreateProfileAsync(client);
        var workout = await CreateWorkoutAsync(client, profileId, "Workout");
        var session = await StartAndReadAsync(client, profileId, workout.Id);
        var strength = session.Exercises[0];

        using var response = await client.PutAsJsonAsync(
            $"/profiles/{profileId}/workout-sessions/{session.Id}",
            new
            {
                expectedRevision = session.Revision,
                clientMutationId = Guid.NewGuid(),
                status = "active",
                finishedAt = (DateTimeOffset?)null,
                notes = (string?)null,
                exercises = new[]
                {
                    new
                    {
                        exerciseId = strength.ExerciseId,
                        isSkipped = false,
                        notes = (string?)null,
                        sets = new[]
                        {
                            new
                            {
                                setId = strength.Sets[0].SetId,
                                isCompleted = true,
                                completedAt = DateTimeOffset.UtcNow,
                                actualRepetitions = 8,
                                actualLoadKilograms = (decimal?)null,
                                actualDurationSeconds = (int?)null,
                                actualDistanceMetres = (decimal?)null,
                            },
                        },
                    },
                },
            },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(
            TestContext.Current.CancellationToken);
        Assert.NotNull(problem);
        Assert.Contains("exercises", problem.Errors.Keys);
    }

    [Fact]
    public async Task UpdateRejectsNullItemsWithoutExposingAHandlerException()
    {
        using var client = CreateClient();
        var profileId = await CreateProfileAsync(client);
        var workout = await CreateWorkoutAsync(client, profileId, "Workout");
        var session = await StartAndReadAsync(client, profileId, workout.Id);
        using var content = new StringContent(
            $$"""
            {
              "expectedRevision": {{session.Revision}},
              "clientMutationId": "{{Guid.NewGuid()}}",
              "status": "active",
              "finishedAt": null,
              "notes": null,
              "exercises": [null]
            }
            """,
            System.Text.Encoding.UTF8,
            "application/json");

        using var response = await client.PutAsync(
            $"/profiles/{profileId}/workout-sessions/{session.Id}",
            content,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(
            TestContext.Current.CancellationToken);
        Assert.NotNull(problem);
        Assert.Contains("exercises", problem.Errors.Keys);
    }

    [Fact]
    public async Task CompletedSessionIsImmutableAndAnotherSessionCanStart()
    {
        using var client = CreateClient();
        var profileId = await CreateProfileAsync(client);
        var workout = await CreateWorkoutAsync(client, profileId, "Workout");
        var session = await StartAndReadAsync(client, profileId, workout.Id);
        var completedAt = DateTimeOffset.UtcNow;
        var completedRequest = UpdateRequest(session, Guid.NewGuid(), completedAt);

        using var finishResponse = await client.PutAsJsonAsync(
            $"/profiles/{profileId}/workout-sessions/{session.Id}",
            completedRequest,
            TestContext.Current.CancellationToken);
        var completed = await ReadSessionAsync(finishResponse);
        using var changeResponse = await client.PutAsJsonAsync(
            $"/profiles/{profileId}/workout-sessions/{session.Id}",
            UpdateRequest(completed, Guid.NewGuid()),
            TestContext.Current.CancellationToken);
        using var nextResponse = await StartSessionAsync(
            client,
            profileId,
            Guid.NewGuid(),
            workout.Id);

        Assert.Equal("completed", completed.Status);
        Assert.NotNull(completed.FinishedAt);
        Assert.Equal(HttpStatusCode.Conflict, changeResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Created, nextResponse.StatusCode);
    }

    [Fact]
    public async Task SessionRoutesRespectProfileAndEnvironmentBoundaries()
    {
        using var client = CreateClient();
        var owner = await CreateProfileAsync(client);
        var other = await CreateProfileAsync(client);
        var workout = await CreateWorkoutAsync(client, owner, "Owner workout");
        var session = await StartAndReadAsync(client, owner, workout.Id);

        using var crossProfile = await client.GetAsync(
            $"/profiles/{other}/workout-sessions/{session.Id}",
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, crossProfile.StatusCode);

        using var productionFactory = fixture.Factory.WithWebHostBuilder(builder =>
            builder.UseEnvironment("Production"));
        using var productionClient = productionFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost"),
        });
        using var productionResponse = await productionClient.GetAsync(
            $"/profiles/{owner}/workout-sessions/active",
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, productionResponse.StatusCode);
    }

    private static object UpdateRequest(
        SessionDocument session,
        Guid mutationId,
        DateTimeOffset? finishedAt = null)
    {
        var completedAt = finishedAt ?? DateTimeOffset.UtcNow;
        return new
        {
            expectedRevision = session.Revision,
            clientMutationId = mutationId,
            status = finishedAt is null ? "active" : "completed",
            finishedAt,
            notes = (string?)null,
            exercises = session.Exercises.Select((exercise, exerciseIndex) => new
            {
                exerciseId = exercise.ExerciseId,
                isSkipped = false,
                notes = (string?)null,
                sets = exercise.Sets.Select((set, setIndex) =>
                    exerciseIndex == 0 && setIndex == 0
                        ? CompletedSet(set.SetId, completedAt, 8, 50m)
                        : IncompleteSet(set.SetId)).ToArray(),
            }).ToArray(),
        };
    }

    private static object CompletedSet(
        Guid setId,
        DateTimeOffset completedAt,
        int repetitions,
        decimal load) => new
        {
            setId,
            isCompleted = true,
            completedAt,
            actualRepetitions = (int?)repetitions,
            actualLoadKilograms = (decimal?)load,
            actualDurationSeconds = (int?)null,
            actualDistanceMetres = (decimal?)null,
        };

    private static object IncompleteSet(Guid setId) => new
    {
        setId,
        isCompleted = false,
        completedAt = (DateTimeOffset?)null,
        actualRepetitions = (int?)null,
        actualLoadKilograms = (decimal?)null,
        actualDurationSeconds = (int?)null,
        actualDistanceMetres = (decimal?)null,
    };

    private static object ActualForMode(
        SessionExerciseDocument exercise,
        DateTimeOffset completedAt) => new
        {
            setId = exercise.Sets[0].SetId,
            isCompleted = true,
            completedAt,
            actualRepetitions = exercise.TrackingMode is "repetitions" or "repetitionsAndLoad"
            ? 12
            : (int?)null,
            actualLoadKilograms = exercise.TrackingMode is "repetitionsAndLoad"
            or "distanceDurationAndLoad"
            ? exercise.TrackingMode == "repetitionsAndLoad" ? 50m : 20m
            : (decimal?)null,
            actualDurationSeconds = exercise.TrackingMode is "duration"
            or "distanceAndDuration"
            or "distanceDurationAndLoad"
            ? exercise.TrackingMode == "duration" ? 45 : 300
            : (int?)null,
            actualDistanceMetres = exercise.TrackingMode is "distanceAndDuration"
            or "distanceDurationAndLoad"
            ? exercise.TrackingMode == "distanceAndDuration" ? 1_000m : 30m
            : (decimal?)null,
        };

    private static async Task<SessionDocument> StartAndReadAsync(
        HttpClient client,
        Guid profileId,
        Guid workoutId)
    {
        using var response = await StartSessionAsync(client, profileId, Guid.NewGuid(), workoutId);
        return await ReadSessionAsync(response);
    }

    private static Task<HttpResponseMessage> StartSessionAsync(
        HttpClient client,
        Guid profileId,
        Guid sessionId,
        Guid workoutId) => client.PostAsJsonAsync(
            $"/profiles/{profileId}/workout-sessions",
            new { sessionId, workoutPlanId = workoutId },
            TestContext.Current.CancellationToken);

    private static async Task<SessionDocument> ReadSessionAsync(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            throw new HttpRequestException(
                $"Expected a workout session response, received {(int)response.StatusCode}: {body}");
        }
        return await response.Content.ReadFromJsonAsync<SessionDocument>(
            TestContext.Current.CancellationToken)
            ?? throw new InvalidOperationException("Expected a workout session document.");
    }

    private static Guid ExerciseId(string slug) => ExerciseCatalogueManifestLoader.Load().Exercises
        .Single(item => item.Slug == slug).Id;

    private static async Task<Guid> CreateProfileAsync(HttpClient client)
    {
        using var response = await client.PostAsJsonAsync(
            "/profiles",
            new
            {
                goals = DefaultGoals,
                experience = "intermediate",
                availableEquipment = DefaultEquipment,
                unitSystem = "metric",
            },
            TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        var profile = await response.Content.ReadFromJsonAsync<ProfileDocument>(
            TestContext.Current.CancellationToken);
        return profile?.Id ?? throw new InvalidOperationException("Expected a profile.");
    }

    private static async Task<WorkoutDocument> CreateWorkoutAsync(
        HttpClient client,
        Guid profileId,
        string name)
    {
        using var response = await client.PostAsJsonAsync(
            $"/profiles/{profileId}/workouts",
            new
            {
                name,
                exercises = new object[]
                {
                    RepetitionPlan(ExerciseId("barbell-bench-press"), 3, 8, 10, 50m),
                    DurationPlan(ExerciseId("front-plank"), 2, 45),
                },
            },
            TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<WorkoutDocument>(
            TestContext.Current.CancellationToken)
            ?? throw new InvalidOperationException("Expected a workout.");
    }

    private static async Task<WorkoutDocument> CreateWorkoutWithPlansAsync(
        HttpClient client,
        Guid profileId,
        string name,
        object[] exercises)
    {
        using var response = await client.PostAsJsonAsync(
            $"/profiles/{profileId}/workouts",
            new { name, exercises },
            TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<WorkoutDocument>(
            TestContext.Current.CancellationToken)
            ?? throw new InvalidOperationException("Expected a workout.");
    }

    private static object RepetitionPlan(
        Guid exerciseId,
        int sets,
        int minimumRepetitions,
        int maximumRepetitions,
        decimal? load) => new
        {
            exerciseId,
            plannedSets = sets,
            minimumRepetitions,
            maximumRepetitions,
            targetLoadKilograms = load,
            targetDurationSeconds = (int?)null,
            targetDistanceMetres = (decimal?)null,
        };

    private static object DurationPlan(Guid exerciseId, int sets, int duration) => new
    {
        exerciseId,
        plannedSets = sets,
        minimumRepetitions = (int?)null,
        maximumRepetitions = (int?)null,
        targetLoadKilograms = (decimal?)null,
        targetDurationSeconds = duration,
        targetDistanceMetres = (decimal?)null,
    };

    private static object DistancePlan(
        Guid exerciseId,
        int sets,
        decimal distance,
        int duration) => new
        {
            exerciseId,
            plannedSets = sets,
            minimumRepetitions = (int?)null,
            maximumRepetitions = (int?)null,
            targetLoadKilograms = (decimal?)null,
            targetDurationSeconds = duration,
            targetDistanceMetres = distance,
        };

    private static object CarryPlan(
        Guid exerciseId,
        int sets,
        decimal distance,
        int duration,
        decimal load) => new
        {
            exerciseId,
            plannedSets = sets,
            minimumRepetitions = (int?)null,
            maximumRepetitions = (int?)null,
            targetLoadKilograms = load,
            targetDurationSeconds = duration,
            targetDistanceMetres = distance,
        };

    private HttpClient CreateClient() => fixture.Factory.CreateClient(
        new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost"),
        });

    private sealed record ProfileDocument(Guid Id);
    private sealed record WorkoutDocument(Guid Id, int Revision);
    private sealed record SessionDocument(
        Guid Id,
        string WorkoutName,
        int WorkoutPlanRevision,
        int Revision,
        string Status,
        DateTimeOffset? FinishedAt,
        string? Notes,
        SessionExerciseDocument[] Exercises);
    private sealed record SessionExerciseDocument(
        Guid ExerciseId,
        string TrackingMode,
        bool IsSkipped,
        string? Notes,
        SessionSetDocument[] Sets);
    private sealed record SessionSetDocument(
        Guid SetId,
        bool IsCompleted,
        int? ActualRepetitions,
        decimal? ActualLoadKilograms,
        int? ActualDurationSeconds,
        decimal? ActualDistanceMetres);
}

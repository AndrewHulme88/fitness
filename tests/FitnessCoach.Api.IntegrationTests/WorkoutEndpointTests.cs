using System.Net;
using System.Net.Http.Json;
using System.Text;

using FitnessCoach.Api.Features.Exercises;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;

namespace FitnessCoach.Api.IntegrationTests;

public sealed class WorkoutEndpointTests : IClassFixture<PostgreSqlApiFixture>
{
    private static readonly string[] DefaultGoals = ["buildStrength"];
    private static readonly string[] DefaultEquipment =
        ["bodyweight", "barbell", "bench", "cardioEquipment"];

    private readonly PostgreSqlApiFixture fixture;

    public WorkoutEndpointTests(PostgreSqlApiFixture fixture)
    {
        this.fixture = fixture;
    }

    [Fact]
    public async Task CreateWorkoutPersistsOrderedTrackingSpecificPrescriptions()
    {
        using var client = CreateClient();
        var profileId = await CreateProfileAsync(client);
        var benchPressId = ExerciseId("barbell-bench-press");
        var plankId = ExerciseId("front-plank");
        var cyclingId = ExerciseId("stationary-cycling");

        using var response = await client.PostAsJsonAsync(
            $"/profiles/{profileId}/workouts",
            new
            {
                name = "Upper strength and conditioning",
                exercises = new object[]
                {
                    RepetitionExercise(benchPressId, 3, 8, 10, 60m),
                    DurationExercise(plankId, 3, 45),
                    DistanceExercise(cyclingId, 1, 10_000m, 1_800),
                },
            },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var workout = await response.Content.ReadFromJsonAsync<WorkoutDocument>(
            TestContext.Current.CancellationToken);
        Assert.NotNull(workout);
        Assert.Equal(profileId, workout.ProfileId);
        Assert.Equal("Upper strength and conditioning", workout.Name);
        Assert.Equal(1, workout.Revision);
        Assert.Equal([benchPressId, plankId, cyclingId],
            workout.Exercises.Select(exercise => exercise.ExerciseId));
        Assert.Equal([0, 1, 2], workout.Exercises.Select(exercise => exercise.Position));
        Assert.Equal("repetitionsAndLoad", workout.Exercises[0].TrackingMode);
        Assert.Equal(60m, workout.Exercises[0].TargetLoadKilograms);
        Assert.Equal("duration", workout.Exercises[1].TrackingMode);
        Assert.Equal(45, workout.Exercises[1].TargetDurationSeconds);
        Assert.Equal("distanceAndDuration", workout.Exercises[2].TrackingMode);
        Assert.Equal(10_000m, workout.Exercises[2].TargetDistanceMetres);
        Assert.Equal(TimeSpan.Zero, workout.CreatedAt.Offset);
        Assert.Equal(workout.CreatedAt, workout.UpdatedAt);
        Assert.Equal(
            $"/profiles/{profileId}/workouts/{workout.Id}",
            response.Headers.Location?.OriginalString);

        using var getResponse = await client.GetAsync(
            $"/profiles/{profileId}/workouts/{workout.Id}",
            TestContext.Current.CancellationToken);
        var persisted = await getResponse.Content.ReadFromJsonAsync<WorkoutDocument>(
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        Assert.NotNull(persisted);
        Assert.Equal(workout.Id, persisted.Id);
        Assert.Equal(workout.Name, persisted.Name);
        Assert.Equal(
            workout.Exercises.Select(exercise => exercise.ExerciseId),
            persisted.Exercises.Select(exercise => exercise.ExerciseId));
    }

    [Fact]
    public async Task ListWorkoutsIsProfileScopedBoundedAndMostRecentlyUpdatedFirst()
    {
        using var client = CreateClient();
        var profileId = await CreateProfileAsync(client);
        var otherProfileId = await CreateProfileAsync(client);

        var first = await CreateWorkoutAsync(client, profileId, "First workout");
        var second = await CreateWorkoutAsync(client, profileId, "Second workout");
        await CreateWorkoutAsync(client, otherProfileId, "Other profile workout");

        using var response = await client.GetAsync(
            $"/profiles/{profileId}/workouts?limit=1",
            TestContext.Current.CancellationToken);
        var result = await response.Content.ReadFromJsonAsync<WorkoutListDocument>(
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);
        var summary = Assert.Single(result.Items);
        Assert.Equal(second.Id, summary.Id);
        Assert.Equal(1, summary.ExerciseCount);
        Assert.Equal(3, summary.PlannedSetCount);
        Assert.Equal(1, result.NextOffset);

        using var secondPageResponse = await client.GetAsync(
            $"/profiles/{profileId}/workouts?limit=1&offset=1",
            TestContext.Current.CancellationToken);
        var secondPage = await secondPageResponse.Content.ReadFromJsonAsync<WorkoutListDocument>(
            TestContext.Current.CancellationToken);

        Assert.NotNull(secondPage);
        Assert.Equal(first.Id, Assert.Single(secondPage.Items).Id);
        Assert.Null(secondPage.NextOffset);
    }

    [Fact]
    public async Task UpdateWorkoutReordersExercisesAndIncrementsRevision()
    {
        using var client = CreateClient();
        var profileId = await CreateProfileAsync(client);
        var benchPressId = ExerciseId("barbell-bench-press");
        var plankId = ExerciseId("front-plank");
        var workout = await CreateWorkoutAsync(
            client,
            profileId,
            "Original workout",
            [
                RepetitionExercise(benchPressId, 3, 8, 10, 50m),
                DurationExercise(plankId, 2, 30),
            ]);

        using var response = await client.PutAsJsonAsync(
            $"/profiles/{profileId}/workouts/{workout.Id}",
            new
            {
                name = "Updated workout",
                expectedRevision = workout.Revision,
                exercises = new object[]
                {
                    DurationExercise(plankId, 3, 45),
                    RepetitionExercise(benchPressId, 4, 6, 8, 62.5m),
                },
            },
            TestContext.Current.CancellationToken);
        var updated = await response.Content.ReadFromJsonAsync<WorkoutDocument>(
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(updated);
        Assert.Equal("Updated workout", updated.Name);
        Assert.Equal(2, updated.Revision);
        Assert.Equal([plankId, benchPressId],
            updated.Exercises.Select(exercise => exercise.ExerciseId));
        Assert.Equal([0, 1], updated.Exercises.Select(exercise => exercise.Position));
        Assert.Equal(45, updated.Exercises[0].TargetDurationSeconds);
        Assert.Equal(62.5m, updated.Exercises[1].TargetLoadKilograms);
        Assert.True(updated.UpdatedAt >= workout.UpdatedAt);
    }

    [Fact]
    public async Task UpdateWorkoutWithStaleRevisionReturnsConflictWithoutChangingIt()
    {
        using var client = CreateClient();
        var profileId = await CreateProfileAsync(client);
        var workout = await CreateWorkoutAsync(client, profileId, "Stable workout");
        var exercise = RepetitionExercise(
            ExerciseId("barbell-bench-press"),
            3,
            8,
            10,
            50m);

        using var firstUpdate = await client.PutAsJsonAsync(
            $"/profiles/{profileId}/workouts/{workout.Id}",
            new
            {
                name = "Latest workout",
                expectedRevision = 1,
                exercises = new[] { exercise },
            },
            TestContext.Current.CancellationToken);
        using var staleUpdate = await client.PutAsJsonAsync(
            $"/profiles/{profileId}/workouts/{workout.Id}",
            new
            {
                name = "Stale overwrite",
                expectedRevision = 1,
                exercises = new[] { exercise },
            },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, firstUpdate.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, staleUpdate.StatusCode);
        Assert.Equal("application/problem+json", staleUpdate.Content.Headers.ContentType?.MediaType);

        using var getResponse = await client.GetAsync(
            $"/profiles/{profileId}/workouts/{workout.Id}",
            TestContext.Current.CancellationToken);
        var persisted = await getResponse.Content.ReadFromJsonAsync<WorkoutDocument>(
            TestContext.Current.CancellationToken);
        Assert.NotNull(persisted);
        Assert.Equal("Latest workout", persisted.Name);
        Assert.Equal(2, persisted.Revision);
    }

    [Theory]
    [InlineData("empty", "exercises")]
    [InlineData("duplicate", "exercises")]
    [InlineData("wrongTrackingMode", "exercises[0].targetDurationSeconds")]
    [InlineData("unknownExercise", "exercises[0].exerciseId")]
    public async Task CreateWorkoutRejectsInvalidPlans(string scenario, string invalidField)
    {
        using var client = CreateClient();
        var profileId = await CreateProfileAsync(client);
        var benchPressId = ExerciseId("barbell-bench-press");
        var validExercise = RepetitionExercise(benchPressId, 3, 8, 10, 50m);
        object[] exercises = scenario switch
        {
            "empty" => [],
            "duplicate" => [validExercise, validExercise],
            "wrongTrackingMode" =>
            [
                new
                {
                    exerciseId = benchPressId,
                    plannedSets = 3,
                    minimumRepetitions = 8,
                    maximumRepetitions = 10,
                    targetLoadKilograms = 50m,
                    targetDurationSeconds = 60,
                    targetDistanceMetres = (decimal?)null,
                },
            ],
            "unknownExercise" =>
            [RepetitionExercise(Guid.NewGuid(), 3, 8, 10, 50m)],
            _ => throw new InvalidOperationException("Unsupported test scenario."),
        };

        using var response = await client.PostAsJsonAsync(
            $"/profiles/{profileId}/workouts",
            new { name = "Invalid workout", exercises },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(
            TestContext.Current.CancellationToken);
        Assert.NotNull(problem);
        Assert.Contains(invalidField, problem.Errors.Keys);
    }

    [Theory]
    [InlineData("limit=51", "limit")]
    [InlineData("limit=many", "limit")]
    [InlineData("offset=-1", "offset")]
    public async Task ListWorkoutsRejectsInvalidPagination(string query, string invalidField)
    {
        using var client = CreateClient();
        var profileId = await CreateProfileAsync(client);

        using var response = await client.GetAsync(
            $"/profiles/{profileId}/workouts?{query}",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(
            TestContext.Current.CancellationToken);
        Assert.NotNull(problem);
        Assert.Contains(invalidField, problem.Errors.Keys);
    }

    [Fact]
    public async Task WorkoutLookupDoesNotCrossProfileBoundary()
    {
        using var client = CreateClient();
        var ownerProfileId = await CreateProfileAsync(client);
        var otherProfileId = await CreateProfileAsync(client);
        var workout = await CreateWorkoutAsync(client, ownerProfileId, "Owner workout");

        using var getResponse = await client.GetAsync(
            $"/profiles/{otherProfileId}/workouts/{workout.Id}",
            TestContext.Current.CancellationToken);
        using var updateResponse = await client.PutAsJsonAsync(
            $"/profiles/{otherProfileId}/workouts/{workout.Id}",
            new
            {
                name = "Cross-profile update",
                expectedRevision = 1,
                exercises = new[]
                {
                    RepetitionExercise(
                        ExerciseId("barbell-bench-press"),
                        3,
                        8,
                        10,
                        50m),
                },
            },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, updateResponse.StatusCode);
    }

    [Fact]
    public async Task WorkoutEndpointsOutsideDevelopmentReturnNotFound()
    {
        using var productionFactory = fixture.Factory.WithWebHostBuilder(builder =>
            builder.UseEnvironment("Production"));
        using var client = productionFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost"),
        });

        using var response = await client.GetAsync(
            $"/profiles/{Guid.NewGuid()}/workouts",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static Guid ExerciseId(string slug)
    {
        return ExerciseCatalogueManifestLoader.Load().Exercises
            .Single(exercise => exercise.Slug == slug)
            .Id;
    }

    private static object RepetitionExercise(
        Guid exerciseId,
        int plannedSets,
        int minimumRepetitions,
        int maximumRepetitions,
        decimal? targetLoadKilograms)
    {
        return new
        {
            exerciseId,
            plannedSets,
            minimumRepetitions,
            maximumRepetitions,
            targetLoadKilograms,
            targetDurationSeconds = (int?)null,
            targetDistanceMetres = (decimal?)null,
        };
    }

    private static object DurationExercise(
        Guid exerciseId,
        int plannedSets,
        int targetDurationSeconds)
    {
        return new
        {
            exerciseId,
            plannedSets,
            minimumRepetitions = (int?)null,
            maximumRepetitions = (int?)null,
            targetLoadKilograms = (decimal?)null,
            targetDurationSeconds,
            targetDistanceMetres = (decimal?)null,
        };
    }

    private static object DistanceExercise(
        Guid exerciseId,
        int plannedSets,
        decimal targetDistanceMetres,
        int targetDurationSeconds)
    {
        return new
        {
            exerciseId,
            plannedSets,
            minimumRepetitions = (int?)null,
            maximumRepetitions = (int?)null,
            targetLoadKilograms = (decimal?)null,
            targetDurationSeconds,
            targetDistanceMetres,
        };
    }

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
        var profile = await response.Content.ReadFromJsonAsync<ProfileIdentifierDocument>(
            TestContext.Current.CancellationToken);
        return profile?.Id
            ?? throw new InvalidOperationException("Expected a created profile identifier.");
    }

    private static Task<WorkoutDocument> CreateWorkoutAsync(
        HttpClient client,
        Guid profileId,
        string name,
        object[]? exercises = null)
    {
        exercises ??=
        [
            RepetitionExercise(
                ExerciseId("barbell-bench-press"),
                3,
                8,
                10,
                50m),
        ];

        return CreateWorkoutCoreAsync(client, profileId, name, exercises);
    }

    private static async Task<WorkoutDocument> CreateWorkoutCoreAsync(
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
            ?? throw new InvalidOperationException("Expected a created workout.");
    }

    private HttpClient CreateClient()
    {
        return fixture.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost"),
        });
    }

    private sealed record ProfileIdentifierDocument(Guid Id);

    private sealed record WorkoutListDocument(
        WorkoutSummaryDocument[] Items,
        int? NextOffset);

    private sealed record WorkoutSummaryDocument(
        Guid Id,
        string Name,
        int ExerciseCount,
        int PlannedSetCount,
        int Revision,
        DateTimeOffset UpdatedAt);

    private sealed record WorkoutDocument(
        Guid Id,
        Guid ProfileId,
        string Name,
        int Revision,
        WorkoutExerciseDocument[] Exercises,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt);

    private sealed record WorkoutExerciseDocument(
        Guid ExerciseId,
        int Position,
        string ExerciseName,
        string TrackingMode,
        string[] PrimaryMuscles,
        int PlannedSets,
        int? MinimumRepetitions,
        int? MaximumRepetitions,
        decimal? TargetLoadKilograms,
        int? TargetDurationSeconds,
        decimal? TargetDistanceMetres);
}

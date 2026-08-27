using System.Net;
using System.Net.Http.Json;

using FitnessCoach.Api.Features.Exercises;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace FitnessCoach.Api.IntegrationTests;

public sealed class ExerciseEndpointTests : IClassFixture<PostgreSqlApiFixture>
{
    private readonly PostgreSqlApiFixture fixture;

    public ExerciseEndpointTests(PostgreSqlApiFixture fixture)
    {
        this.fixture = fixture;
    }

    [Fact]
    public async Task SearchByAliasReturnsTheMatchingCuratedExercise()
    {
        using var client = CreateClient();

        using var response = await client.GetAsync(
            "/exercises?query=DB%20RDL",
            TestContext.Current.CancellationToken);
        var result = await response.Content.ReadFromJsonAsync<ExerciseSearchDocument>(
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);
        var exercise = Assert.Single(result.Items);
        Assert.Equal("dumbbell-romanian-deadlift", exercise.Slug);
        Assert.Equal("hinge", exercise.MovementPattern);
        Assert.Equal("repetitionsAndLoad", exercise.TrackingMode);
    }

    [Fact]
    public async Task SearchTreatsWildcardCharactersAsLiteralText()
    {
        using var client = CreateClient();

        using var response = await client.GetAsync(
            "/exercises?query=%25",
            TestContext.Current.CancellationToken);
        var result = await response.Content.ReadFromJsonAsync<ExerciseSearchDocument>(
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task SearchWithAvailableEquipmentReturnsOnlyUsableExercises()
    {
        using var client = CreateClient();
        var availableEquipment = new HashSet<string>(StringComparer.Ordinal)
        {
            "barbell",
            "bench",
            "squatRack",
        };

        using var response = await client.GetAsync(
            "/exercises?availableEquipment=barbell&availableEquipment=bench"
            + "&availableEquipment=squatRack&limit=50",
            TestContext.Current.CancellationToken);
        var result = await response.Content.ReadFromJsonAsync<ExerciseSearchDocument>(
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);
        Assert.Contains(result.Items, exercise => exercise.Slug == "barbell-bench-press");
        Assert.All(result.Items, exercise =>
            Assert.All(exercise.RequiredEquipment, equipment =>
                Assert.Contains(equipment, availableEquipment)));
    }

    [Fact]
    public async Task SearchCombinesTaxonomyFilters()
    {
        using var client = CreateClient();

        using var response = await client.GetAsync(
            "/exercises?category=strength&movementPattern=horizontalPush"
            + "&primaryMuscle=chest&availableEquipment=dumbbells"
            + "&availableEquipment=bench",
            TestContext.Current.CancellationToken);
        var result = await response.Content.ReadFromJsonAsync<ExerciseSearchDocument>(
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);
        var exercise = Assert.Single(result.Items);
        Assert.Equal("dumbbell-bench-press", exercise.Slug);
    }

    [Fact]
    public async Task SearchUsesBoundedStablePagination()
    {
        using var client = CreateClient();

        using var firstResponse = await client.GetAsync(
            "/exercises?limit=2",
            TestContext.Current.CancellationToken);
        var firstPage = await firstResponse.Content.ReadFromJsonAsync<ExerciseSearchDocument>(
            TestContext.Current.CancellationToken);
        using var secondResponse = await client.GetAsync(
            "/exercises?limit=2&offset=2",
            TestContext.Current.CancellationToken);
        var secondPage = await secondResponse.Content.ReadFromJsonAsync<ExerciseSearchDocument>(
            TestContext.Current.CancellationToken);

        Assert.NotNull(firstPage);
        Assert.NotNull(secondPage);
        Assert.Equal(2, firstPage.Items.Length);
        Assert.Equal(2, firstPage.NextOffset);
        Assert.DoesNotContain(
            secondPage.Items,
            second => firstPage.Items.Any(first => first.Id == second.Id));
    }

    [Theory]
    [InlineData("/exercises?limit=51", "limit")]
    [InlineData("/exercises?limit=lots", "limit")]
    [InlineData("/exercises?category=0", "category")]
    [InlineData(
        "/exercises?availableEquipment=bodyweight&availableEquipment=bodyweight",
        "availableEquipment")]
    public async Task SearchRejectsInvalidOrUnboundedFilters(string requestUri, string invalidField)
    {
        using var client = CreateClient();

        using var response = await client.GetAsync(
            requestUri,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(
            TestContext.Current.CancellationToken);
        Assert.NotNull(problem);
        Assert.Contains(invalidField, problem.Errors.Keys);
    }

    [Fact]
    public async Task GetExerciseReturnsTheCuratedDetail()
    {
        using var client = CreateClient();
        var manifestExercise = ExerciseCatalogueManifestLoader.Load().Exercises
            .Single(exercise => exercise.Slug == "barbell-back-squat");

        using var response = await client.GetAsync(
            $"/exercises/{manifestExercise.Id}",
            TestContext.Current.CancellationToken);
        var exercise = await response.Content.ReadFromJsonAsync<ExerciseDetailDocument>(
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(exercise);
        Assert.Equal("Barbell Back Squat", exercise.Name);
        Assert.Equal(["barbell", "squatRack"], exercise.RequiredEquipment);
        Assert.Equal(["quadriceps", "glutes"], exercise.PrimaryMuscles);
        Assert.NotEmpty(exercise.Setup);
        Assert.NotEmpty(exercise.Execution);
        Assert.NotEmpty(exercise.Safety);
    }

    [Fact]
    public async Task GetUnknownExerciseReturnsNotFound()
    {
        using var client = CreateClient();

        using var response = await client.GetAsync(
            $"/exercises/{Guid.NewGuid()}",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ReimportingTheSameCatalogueIsAnUnchangedOperation()
    {
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var importer = scope.ServiceProvider.GetRequiredService<ExerciseCatalogueImporter>();

        var result = await importer.ImportAsync(TestContext.Current.CancellationToken);

        Assert.Equal(ExerciseCatalogueImportStatus.Unchanged, result.Status);
        Assert.Equal(35, result.ExerciseCount);
        Assert.Equal(0, result.AddedCount);
    }

    [Fact]
    public async Task ExerciseEndpointsOutsideDevelopmentReturnNotFound()
    {
        using var productionFactory = fixture.Factory.WithWebHostBuilder(builder =>
            builder.UseEnvironment("Production"));
        using var client = productionFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost"),
        });

        using var response = await client.GetAsync(
            "/exercises",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private HttpClient CreateClient()
    {
        return fixture.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost"),
        });
    }

    private sealed record ExerciseSearchDocument(
        ExerciseSummaryDocument[] Items,
        int? NextOffset);

    private sealed record ExerciseSummaryDocument(
        Guid Id,
        string Slug,
        string Name,
        string Category,
        string MovementPattern,
        string TrackingMode,
        string[] RequiredEquipment,
        string[] PrimaryMuscles);

    private sealed record ExerciseDetailDocument(
        Guid Id,
        string Slug,
        string Name,
        string Category,
        string MovementPattern,
        string TrackingMode,
        string[] RequiredEquipment,
        string[] PrimaryMuscles,
        string[] SecondaryMuscles,
        string Setup,
        string Execution,
        string Safety);
}

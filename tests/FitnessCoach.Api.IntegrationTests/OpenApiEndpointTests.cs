using System.Net;
using System.Text.Json;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace FitnessCoach.Api.IntegrationTests;

public sealed class OpenApiEndpointTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory factory;

    public OpenApiEndpointTests(ApiWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task GetOpenApiInDevelopmentReturnsVersionedHealthContract()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(
            "/openapi/v1.json",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        await using var content = await response.Content.ReadAsStreamAsync(
            TestContext.Current.CancellationToken);
        using var document = await JsonDocument.ParseAsync(
            content,
            cancellationToken: TestContext.Current.CancellationToken);

        var root = document.RootElement;
        Assert.Equal("3.1.1", root.GetProperty("openapi").GetString());
        Assert.Equal("v1", root.GetProperty("info").GetProperty("version").GetString());

        var healthOperation = root
            .GetProperty("paths")
            .GetProperty("/health")
            .GetProperty("get");

        Assert.Equal("GetHealth", healthOperation.GetProperty("operationId").GetString());
        var responses = healthOperation.GetProperty("responses");
        Assert.True(responses.TryGetProperty("200", out _));
        Assert.True(responses.TryGetProperty("503", out _));

        var exerciseOperation = root
            .GetProperty("paths")
            .GetProperty("/exercises")
            .GetProperty("get");
        Assert.Equal(
            "SearchExercises",
            exerciseOperation.GetProperty("operationId").GetString());

        var parameters = exerciseOperation.GetProperty("parameters");
        var categoryParameter = parameters.EnumerateArray().Single(parameter =>
            parameter.GetProperty("name").GetString() == "category");
        Assert.Equal(
            ["strength", "cardio"],
            categoryParameter
                .GetProperty("schema")
                .GetProperty("enum")
                .EnumerateArray()
                .Select(value => value.GetString()
                    ?? throw new InvalidOperationException("Expected a string enum value."))
                .ToArray());

        var equipmentParameter = parameters.EnumerateArray().Single(parameter =>
            parameter.GetProperty("name").GetString() == "availableEquipment");
        Assert.Contains(
            equipmentParameter
                .GetProperty("schema")
                .GetProperty("items")
                .GetProperty("enum")
                .EnumerateArray()
                .Select(value => value.GetString()),
            value => value == "cardioEquipment");

        var limitParameter = parameters.EnumerateArray().Single(parameter =>
            parameter.GetProperty("name").GetString() == "limit");
        Assert.Equal(
            "integer",
            limitParameter.GetProperty("schema").GetProperty("type").GetString());
        Assert.Equal(
            50,
            limitParameter.GetProperty("schema").GetProperty("maximum").GetInt32());

        var workoutCollection = root
            .GetProperty("paths")
            .GetProperty("/profiles/{profileId}/workouts");
        Assert.Equal(
            "CreateWorkout",
            workoutCollection.GetProperty("post").GetProperty("operationId").GetString());
        var listWorkouts = workoutCollection.GetProperty("get");
        Assert.Equal("ListWorkouts", listWorkouts.GetProperty("operationId").GetString());
        var workoutLimit = listWorkouts
            .GetProperty("parameters")
            .EnumerateArray()
            .Single(parameter => parameter.GetProperty("name").GetString() == "limit");
        Assert.Equal(
            "integer",
            workoutLimit.GetProperty("schema").GetProperty("type").GetString());
        Assert.Equal(
            50,
            workoutLimit.GetProperty("schema").GetProperty("maximum").GetInt32());

        var workoutItem = root
            .GetProperty("paths")
            .GetProperty("/profiles/{profileId}/workouts/{workoutId}");
        Assert.Equal(
            "GetWorkout",
            workoutItem.GetProperty("get").GetProperty("operationId").GetString());
        Assert.Equal(
            "UpdateWorkout",
            workoutItem.GetProperty("put").GetProperty("operationId").GetString());
    }

    [Fact]
    public async Task GetOpenApiOutsideDevelopmentReturnsNotFound()
    {
        using var productionFactory = factory.WithWebHostBuilder(builder =>
            builder.UseEnvironment("Production"));
        using var client = productionFactory.CreateClient();

        using var response = await client.GetAsync(
            "/openapi/v1.json",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

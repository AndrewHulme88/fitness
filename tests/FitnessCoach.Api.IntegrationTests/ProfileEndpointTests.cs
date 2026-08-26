using System.Net;
using System.Net.Http.Json;
using System.Text;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace FitnessCoach.Api.IntegrationTests;

public sealed class ProfileEndpointTests : IClassFixture<PostgreSqlApiFixture>
{
    private readonly PostgreSqlApiFixture fixture;

    public ProfileEndpointTests(PostgreSqlApiFixture fixture)
    {
        this.fixture = fixture;
    }

    [Fact]
    public async Task CreateProfileWithSupportedChoicesPersistsAndReturnsTheProfile()
    {
        using var client = CreateClient();
        var request = new
        {
            goals = new[] { "buildStrength", "buildMuscle", "generalFitness" },
            experience = "advanced",
            availableEquipment = new[]
            {
                "bodyweight",
                "dumbbells",
                "barbell",
                "bench",
                "squatRack",
                "cableMachine",
                "resistanceBands",
                "cardioEquipment",
            },
            unitSystem = "imperial",
        };

        using var response = await client.PostAsJsonAsync(
            "/profiles",
            request,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var createdProfile = await response.Content.ReadFromJsonAsync<ProfileDocument>(
            TestContext.Current.CancellationToken);
        Assert.NotNull(createdProfile);
        Assert.Equal($"/profiles/{createdProfile.Id}", response.Headers.Location?.OriginalString);
        Assert.Equal(["buildStrength", "buildMuscle", "generalFitness"], createdProfile.Goals);
        Assert.Equal("advanced", createdProfile.Experience);
        Assert.Equal(
            [
                "bodyweight",
                "dumbbells",
                "barbell",
                "bench",
                "squatRack",
                "cableMachine",
                "resistanceBands",
                "cardioEquipment",
            ],
            createdProfile.AvailableEquipment);
        Assert.Equal("imperial", createdProfile.UnitSystem);
        Assert.Equal(TimeSpan.Zero, createdProfile.CreatedAt.Offset);

        using var getResponse = await client.GetAsync(
            $"/profiles/{createdProfile.Id}",
            TestContext.Current.CancellationToken);
        var persistedProfile = await getResponse.Content.ReadFromJsonAsync<ProfileDocument>(
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        Assert.NotNull(persistedProfile);
        Assert.Equal(createdProfile.Id, persistedProfile.Id);
        Assert.Equal(createdProfile.Goals, persistedProfile.Goals);
        Assert.Equal(createdProfile.Experience, persistedProfile.Experience);
        Assert.Equal(createdProfile.AvailableEquipment, persistedProfile.AvailableEquipment);
        Assert.Equal(createdProfile.UnitSystem, persistedProfile.UnitSystem);
        Assert.Equal(createdProfile.CreatedAt, persistedProfile.CreatedAt);
    }

    [Theory]
    [InlineData(
        "{\"goals\":[],\"experience\":\"beginner\",\"availableEquipment\":[\"bodyweight\"],\"unitSystem\":\"metric\"}",
        "goals")]
    [InlineData(
        "{\"goals\":[\"buildMuscle\",\"buildMuscle\"],\"experience\":\"beginner\",\"availableEquipment\":[\"bodyweight\"],\"unitSystem\":\"metric\"}",
        "goals")]
    public async Task CreateProfileWithInvalidSelectionsReturnsValidationProblem(
        string requestJson,
        string invalidField)
    {
        using var client = CreateClient();
        using var content = new StringContent(requestJson, Encoding.UTF8, "application/json");

        using var response = await client.PostAsync(
            "/profiles",
            content,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(
            TestContext.Current.CancellationToken);
        Assert.NotNull(problem);
        Assert.Contains(invalidField, problem.Errors.Keys);
    }

    [Theory]
    [InlineData(
        "{\"goals\":[\"buildStrength\"],\"experience\":0,\"availableEquipment\":[\"bodyweight\"],\"unitSystem\":\"metric\"}")]
    [InlineData(
        "{\"goals\":[\"buildStrength\"],\"experience\":\"beginner\",\"availableEquipment\":[\"bodyweight\"],\"unitSystem\":\"metric\",\"medicalNotes\":\"private detail\"}")]
    public async Task CreateProfileWithUnsupportedJsonReturnsBadRequest(string requestJson)
    {
        using var client = CreateClient();
        using var content = new StringContent(requestJson, Encoding.UTF8, "application/json");

        using var response = await client.PostAsync(
            "/profiles",
            content,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var responseBody = await response.Content.ReadAsStringAsync(
            TestContext.Current.CancellationToken);
        Assert.DoesNotContain("private detail", responseBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetUnknownProfileReturnsNotFound()
    {
        using var client = CreateClient();

        using var response = await client.GetAsync(
            $"/profiles/{Guid.NewGuid()}",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ProfileEndpointsOutsideDevelopmentReturnNotFound()
    {
        using var productionFactory = fixture.Factory.WithWebHostBuilder(builder =>
            builder.UseEnvironment("Production"));
        using var client = productionFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost"),
        });

        using var response = await client.GetAsync(
            $"/profiles/{Guid.NewGuid()}",
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

    private sealed record ProfileDocument(
        Guid Id,
        string[] Goals,
        string Experience,
        string[] AvailableEquipment,
        string UnitSystem,
        DateTimeOffset CreatedAt);
}

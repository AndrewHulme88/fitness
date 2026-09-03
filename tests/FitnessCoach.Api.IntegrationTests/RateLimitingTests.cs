using System.Net;
using System.Net.Http.Json;

using Microsoft.AspNetCore.Mvc.Testing;

namespace FitnessCoach.Api.IntegrationTests;

public sealed class RateLimitingTests : IClassFixture<PostgreSqlApiFixture>
{
    private static readonly ProfileRequest CreateProfileRequest = new(
        ["buildStrength"], "beginner", ["bodyweight"], "metric");

    private readonly PostgreSqlApiFixture fixture;

    public RateLimitingTests(PostgreSqlApiFixture fixture) => this.fixture = fixture;

    [Fact]
    public async Task StandardPolicyLimitsOneAuthenticatedAccountWithoutLoggingItsIdentifier()
    {
        using var factory = fixture.Factory.WithTestAuthentication("Testing");
        using var client = CreateAuthenticatedClient(factory);

        for (var requestNumber = 0; requestNumber < 120; requestNumber++)
        {
            using var response = await client.GetAsync("/account", TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        using var rejected = await client.GetAsync("/account", TestContext.Current.CancellationToken);

        Assert.Equal((HttpStatusCode)429, rejected.StatusCode);
        Assert.NotNull(rejected.Headers.RetryAfter);
        Assert.Equal("application/problem+json", rejected.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task CoachMessagesUseTheirTighterPerAccountPolicy()
    {
        using var factory = fixture.Factory.WithTestAuthentication("Testing");
        using var client = CreateAuthenticatedClient(factory);
        var profileId = await CreateProfileAsync(client);
        var request = new { question = "How should I think about rest between sets?" };

        for (var requestNumber = 0; requestNumber < 6; requestNumber++)
        {
            using var response = await client.PostAsJsonAsync(
                $"/profiles/{profileId}/coach/conversation/messages",
                request,
                TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        using var rejected = await client.PostAsJsonAsync(
            $"/profiles/{profileId}/coach/conversation/messages",
            request,
            TestContext.Current.CancellationToken);

        Assert.Equal((HttpStatusCode)429, rejected.StatusCode);
        Assert.NotNull(rejected.Headers.RetryAfter);
        Assert.Equal("application/problem+json", rejected.Content.Headers.ContentType?.MediaType);
    }

    private static HttpClient CreateAuthenticatedClient(WebApplicationFactory<Program> factory)
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost"),
        });
        client.DefaultRequestHeaders.Add(
            TestAuthenticationHandler.SubjectHeader,
            $"rate-limit-{Guid.NewGuid():N}");
        return client;
    }

    private static async Task<Guid> CreateProfileAsync(HttpClient client)
    {
        using var response = await client.PostAsJsonAsync(
            "/profiles",
            CreateProfileRequest,
            TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        var profile = await response.Content.ReadFromJsonAsync<ProfileDocument>(
            TestContext.Current.CancellationToken);
        return profile?.Id ?? throw new InvalidOperationException("Expected a profile.");
    }

    private sealed record ProfileDocument(Guid Id);
    private sealed record ProfileRequest(
        string[] Goals,
        string Experience,
        string[] AvailableEquipment,
        string UnitSystem);
}

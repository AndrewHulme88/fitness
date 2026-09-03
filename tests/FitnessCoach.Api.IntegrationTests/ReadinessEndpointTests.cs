using System.Net;

using Microsoft.AspNetCore.Mvc.Testing;

namespace FitnessCoach.Api.IntegrationTests;

public sealed class ReadinessEndpointTests : IClassFixture<PostgreSqlApiFixture>
{
    private readonly PostgreSqlApiFixture fixture;

    public ReadinessEndpointTests(PostgreSqlApiFixture fixture) => this.fixture = fixture;

    [Fact]
    public async Task GetReadinessReturnsReadyWhenPostgreSqlIsAvailable()
    {
        using var client = fixture.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost"),
        });

        using var response = await client.GetAsync("/health/ready", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(true, response.Headers.CacheControl?.NoStore);
        Assert.Equal(
            "Ready",
            await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }
}

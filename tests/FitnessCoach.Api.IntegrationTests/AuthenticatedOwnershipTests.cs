using System.Net;
using System.Net.Http.Json;

using Microsoft.AspNetCore.Mvc.Testing;

namespace FitnessCoach.Api.IntegrationTests;

public sealed class AuthenticatedOwnershipTests : IClassFixture<PostgreSqlApiFixture>
{
    private static readonly ProfileRequest Request = new(
        ["buildStrength"], "beginner", ["bodyweight"], "metric");

    private readonly PostgreSqlApiFixture fixture;

    public AuthenticatedOwnershipTests(PostgreSqlApiFixture fixture) => this.fixture = fixture;

    [Fact]
    public async Task ProtectedProfilesRejectAnonymousAndHideAnotherAccountsProfile()
    {
        using var factory = fixture.Factory.WithTestAuthentication();
        using var owner = CreateClient(factory, "account-owner");
        using var anonymous = CreateClient(factory);
        using var otherAccount = CreateClient(factory, "other-account");

        using var anonymousResponse = await anonymous.GetAsync(
            "/profiles/00000000-0000-0000-0000-000000000001", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, anonymousResponse.StatusCode);

        using var createResponse = await owner.PostAsJsonAsync(
            "/profiles", Request, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var profile = await createResponse.Content.ReadFromJsonAsync<ProfileDocument>(
            TestContext.Current.CancellationToken);
        Assert.NotNull(profile);

        using var crossAccountResponse = await otherAccount.GetAsync(
            $"/profiles/{profile.Id}", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, crossAccountResponse.StatusCode);

        using var accountResponse = await owner.GetAsync("/account", TestContext.Current.CancellationToken);
        var account = await accountResponse.Content.ReadFromJsonAsync<AccountDocument>(TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, accountResponse.StatusCode);
        Assert.Equal(profile.Id, account?.ProfileId);
    }

    private static HttpClient CreateClient(WebApplicationFactory<Program> factory, string? subject = null)
    {
        var client = factory.CreateClient();
        if (subject is not null) client.DefaultRequestHeaders.Add(TestAuthenticationHandler.SubjectHeader, subject);
        return client;
    }

    private sealed record ProfileDocument(Guid Id);
    private sealed record AccountDocument(Guid? ProfileId);
    private sealed record ProfileRequest(
        string[] Goals,
        string Experience,
        string[] AvailableEquipment,
        string UnitSystem);
}

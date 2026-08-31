using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FitnessCoach.Api.IntegrationTests;

public sealed class ApiWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string? postgresConnectionString;

    public ApiWebApplicationFactory()
    {
    }

    internal ApiWebApplicationFactory(string postgresConnectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(postgresConnectionString);
        this.postgresConnectionString = postgresConnectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        if (postgresConnectionString is null)
        {
            return;
        }

        builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Postgres"] = postgresConnectionString,
            }));
    }

    internal WebApplicationFactory<Program> WithTestAuthentication(string environmentName = "Development")
    {
        return WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(environmentName);
            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Cognito:Region"] = "ap-southeast-2",
                    ["Cognito:UserPoolId"] = "ap-southeast-2_testPool",
                    ["Cognito:AppClientId"] = "test-client",
                    ["Cognito:RequiredScope"] = "fitness-coach-api/access",
                }));
            builder.ConfigureTestServices(services =>
                services.AddAuthentication(TestAuthenticationHandler.SubjectHeader)
                    .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                        TestAuthenticationHandler.SubjectHeader,
                        _ => { }));
        });
    }
}

using FitnessCoach.Api.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Testcontainers.PostgreSql;

namespace FitnessCoach.Api.IntegrationTests;

public sealed class PostgreSqlApiFixture : IAsyncLifetime
{
    private const string PostgreSqlImage =
        "postgres:18.6-alpine3.24@sha256:d3e1620b530c944afa6e887d22eb899824da68e19c52024bf98f5220c88a65b2";

    private readonly PostgreSqlContainer postgres = new PostgreSqlBuilder(PostgreSqlImage)
        .WithDatabase("fitness_coach_profile_tests")
        .WithUsername("fitness_coach_profile_tests")
        .WithPassword("test-only-password")
        .Build();
    private ApiWebApplicationFactory? factory;

    public ApiWebApplicationFactory Factory => factory
        ?? throw new InvalidOperationException("The PostgreSQL API fixture has not been initialized.");

    public async ValueTask InitializeAsync()
    {
        await postgres.StartAsync();
        factory = new ApiWebApplicationFactory(postgres.GetConnectionString());

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FitnessCoachDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (factory is not null)
        {
            await factory.DisposeAsync();
        }

        await postgres.DisposeAsync();
    }
}

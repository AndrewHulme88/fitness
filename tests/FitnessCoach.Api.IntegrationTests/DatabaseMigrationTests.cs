using FitnessCoach.Api.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Testcontainers.PostgreSql;

namespace FitnessCoach.Api.IntegrationTests;

public sealed class DatabaseMigrationTests
{
    private const string PostgreSqlImage =
        "postgres:18.6-alpine3.24@sha256:d3e1620b530c944afa6e887d22eb899824da68e19c52024bf98f5220c88a65b2";

    [Fact]
    public void DbContextRequiresThePostgresEnvironmentConfiguration()
    {
        using var factory = new ApiWebApplicationFactory();
        using var scope = factory.Services.CreateScope();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            scope.ServiceProvider.GetRequiredService<FitnessCoachDbContext>());

        Assert.Contains("ConnectionStrings__Postgres", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task InitialMigrationCanBeAppliedToPostgreSql()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var postgres = new PostgreSqlBuilder(PostgreSqlImage)
            .WithDatabase("fitness_coach_tests")
            .WithUsername("fitness_coach_tests")
            .WithPassword("test-only-password")
            .Build();

        await postgres.StartAsync(cancellationToken);

        using var factory = new ApiWebApplicationFactory(postgres.GetConnectionString());
        await using var scope = factory.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<FitnessCoachDbContext>().Database;

        await database.MigrateAsync(cancellationToken);

        Assert.True(await database.CanConnectAsync(cancellationToken));
        Assert.Contains(
            await database.GetAppliedMigrationsAsync(cancellationToken),
            migration => migration.EndsWith("_InitialInfrastructure", StringComparison.Ordinal));
        Assert.Empty(await database.GetPendingMigrationsAsync(cancellationToken));
    }
}

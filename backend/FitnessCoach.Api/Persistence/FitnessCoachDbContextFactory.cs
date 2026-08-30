using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FitnessCoach.Api.Persistence;

public sealed class FitnessCoachDbContextFactory : IDesignTimeDbContextFactory<FitnessCoachDbContext>
{
    public FitnessCoachDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Postgres");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings__Postgres is required to create migrations.");
        }

        var options = new DbContextOptionsBuilder<FitnessCoachDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new FitnessCoachDbContext(options);
    }
}

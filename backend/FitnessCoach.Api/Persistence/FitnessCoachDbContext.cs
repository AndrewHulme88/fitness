using Microsoft.EntityFrameworkCore;

namespace FitnessCoach.Api.Persistence;

public sealed class FitnessCoachDbContext(DbContextOptions<FitnessCoachDbContext> options)
    : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FitnessCoachDbContext).Assembly);
    }
}

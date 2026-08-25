using Microsoft.EntityFrameworkCore;

namespace FitnessCoach.Api.Persistence;

public sealed class FitnessCoachDbContext(DbContextOptions<FitnessCoachDbContext> options)
    : DbContext(options);

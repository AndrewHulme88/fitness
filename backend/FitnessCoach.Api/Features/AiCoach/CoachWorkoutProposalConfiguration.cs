using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FitnessCoach.Api.Features.AiCoach;

internal sealed class CoachWorkoutProposalConfiguration : IEntityTypeConfiguration<CoachWorkoutProposal>
{
    public void Configure(EntityTypeBuilder<CoachWorkoutProposal> builder)
    {
        builder.ToTable("coach_workout_proposals"); builder.HasKey(item => item.Id);
        builder.Property(item => item.Exercises).HasColumnType("jsonb");
        builder.Property(item => item.Rationale).HasMaxLength(600);
        builder.Property(item => item.Name).HasMaxLength(80);
        builder.HasIndex(item => new { item.ProfileId, item.CreatedAt });
    }
}

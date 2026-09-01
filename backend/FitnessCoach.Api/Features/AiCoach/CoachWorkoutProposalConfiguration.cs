using FitnessCoach.Api.Features.Profiles;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FitnessCoach.Api.Features.AiCoach;

internal sealed class CoachWorkoutProposalConfiguration : IEntityTypeConfiguration<CoachWorkoutProposal>
{
    public void Configure(EntityTypeBuilder<CoachWorkoutProposal> builder)
    {
        builder.ToTable("coach_workout_proposals");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).HasColumnName("id");
        builder.Property(item => item.ProfileId).HasColumnName("profile_id");
        builder.Property(item => item.WorkoutId).HasColumnName("workout_id");
        builder.Property(item => item.ExpectedRevision).HasColumnName("expected_revision");
        builder.Property(item => item.ExercisesJson).HasColumnName("exercises").HasColumnType("jsonb");
        builder.Property(item => item.Rationale).HasColumnName("rationale").HasMaxLength(600);
        builder.Property(item => item.Name).HasColumnName("name").HasMaxLength(80);
        builder.Property(item => item.CreatedAt).HasColumnName("created_at");
        builder.Property(item => item.ConfirmedAt).HasColumnName("confirmed_at");
        builder.HasIndex(item => new { item.ProfileId, item.CreatedAt });
        builder.HasOne<TrainingProfile>()
            .WithMany()
            .HasForeignKey(item => item.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

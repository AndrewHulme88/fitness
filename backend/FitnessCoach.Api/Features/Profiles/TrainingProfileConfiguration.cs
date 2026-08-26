using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FitnessCoach.Api.Features.Profiles;

internal sealed class TrainingProfileConfiguration : IEntityTypeConfiguration<TrainingProfile>
{
    public void Configure(EntityTypeBuilder<TrainingProfile> builder)
    {
        builder.ToTable("training_profiles", table =>
        {
            table.HasCheckConstraint(
                "CK_training_profiles_experience",
                "experience IN ('Beginner', 'Intermediate', 'Advanced')");
            table.HasCheckConstraint(
                "CK_training_profiles_unit_system",
                "unit_system IN ('Metric', 'Imperial')");
        });

        builder.HasKey(profile => profile.Id);

        builder.Property(profile => profile.Id).HasColumnName("id");
        builder.Property(profile => profile.Experience)
            .HasColumnName("experience")
            .HasConversion<string>()
            .HasMaxLength(32);
        builder.Property(profile => profile.UnitSystem)
            .HasColumnName("unit_system")
            .HasConversion<string>()
            .HasMaxLength(16);
        builder.Property(profile => profile.CreatedAt).HasColumnName("created_at");

        builder.HasMany(profile => profile.Goals)
            .WithOne()
            .HasForeignKey(goal => goal.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(profile => profile.AvailableEquipment)
            .WithOne()
            .HasForeignKey(equipment => equipment.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class TrainingProfileGoalConfiguration
    : IEntityTypeConfiguration<TrainingProfileGoal>
{
    public void Configure(EntityTypeBuilder<TrainingProfileGoal> builder)
    {
        builder.ToTable("training_profile_goals", table =>
            table.HasCheckConstraint(
                "CK_training_profile_goals_goal",
                "goal IN ('BuildStrength', 'BuildMuscle', 'GeneralFitness')"));

        builder.HasKey(goal => new { goal.ProfileId, goal.Goal });
        builder.Property(goal => goal.ProfileId).HasColumnName("profile_id");
        builder.Property(goal => goal.Goal)
            .HasColumnName("goal")
            .HasConversion<string>()
            .HasMaxLength(32);
    }
}

internal sealed class TrainingProfileEquipmentConfiguration
    : IEntityTypeConfiguration<TrainingProfileEquipment>
{
    public void Configure(EntityTypeBuilder<TrainingProfileEquipment> builder)
    {
        builder.ToTable("training_profile_equipment", table =>
            table.HasCheckConstraint(
                "CK_training_profile_equipment_equipment",
                "equipment IN ('Bodyweight', 'Dumbbells', 'Barbell', 'Bench', 'SquatRack', "
                + "'CableMachine', 'ResistanceBands', 'CardioEquipment')"));

        builder.HasKey(equipment => new { equipment.ProfileId, equipment.Equipment });
        builder.Property(equipment => equipment.ProfileId).HasColumnName("profile_id");
        builder.Property(equipment => equipment.Equipment)
            .HasColumnName("equipment")
            .HasConversion<string>()
            .HasMaxLength(32);
    }
}

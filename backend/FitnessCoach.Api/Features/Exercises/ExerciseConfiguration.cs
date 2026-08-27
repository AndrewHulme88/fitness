using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FitnessCoach.Api.Features.Exercises;

internal sealed class ExerciseConfiguration : IEntityTypeConfiguration<Exercise>
{
    public void Configure(EntityTypeBuilder<Exercise> builder)
    {
        builder.ToTable("exercises", table =>
        {
            table.HasCheckConstraint(
                "CK_exercises_category",
                "category IN ('Strength', 'Cardio')");
            table.HasCheckConstraint(
                "CK_exercises_movement_pattern",
                "movement_pattern IN ('Squat', 'Hinge', 'Lunge', 'HorizontalPush', "
                + "'VerticalPush', 'HorizontalPull', 'VerticalPull', 'Carry', "
                + "'CoreStability', 'ElbowFlexion', 'ElbowExtension', 'CalfRaise', "
                + "'Locomotion')");
            table.HasCheckConstraint(
                "CK_exercises_tracking_mode",
                "tracking_mode IN ('Repetitions', 'RepetitionsAndLoad', 'Duration', "
                + "'DistanceAndDuration', 'DistanceDurationAndLoad')");
        });

        builder.HasKey(exercise => exercise.Id);
        builder.HasIndex(exercise => exercise.Slug).IsUnique();
        builder.HasIndex(exercise => exercise.Name).IsUnique();

        builder.Property(exercise => exercise.Id).HasColumnName("id");
        builder.Property(exercise => exercise.Slug)
            .HasColumnName("slug")
            .HasMaxLength(80);
        builder.Property(exercise => exercise.Name)
            .HasColumnName("name")
            .HasMaxLength(120);
        builder.Property(exercise => exercise.Category)
            .HasColumnName("category")
            .HasConversion<string>()
            .HasMaxLength(16);
        builder.Property(exercise => exercise.MovementPattern)
            .HasColumnName("movement_pattern")
            .HasConversion<string>()
            .HasMaxLength(32);
        builder.Property(exercise => exercise.TrackingMode)
            .HasColumnName("tracking_mode")
            .HasConversion<string>()
            .HasMaxLength(32);
        builder.Property(exercise => exercise.Setup)
            .HasColumnName("setup")
            .HasMaxLength(500);
        builder.Property(exercise => exercise.Execution)
            .HasColumnName("execution")
            .HasMaxLength(700);
        builder.Property(exercise => exercise.Safety)
            .HasColumnName("safety")
            .HasMaxLength(500);

        builder.HasMany(exercise => exercise.Aliases)
            .WithOne()
            .HasForeignKey(alias => alias.ExerciseId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(exercise => exercise.RequiredEquipment)
            .WithOne()
            .HasForeignKey(equipment => equipment.ExerciseId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(exercise => exercise.Muscles)
            .WithOne()
            .HasForeignKey(muscle => muscle.ExerciseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class ExerciseAliasConfiguration : IEntityTypeConfiguration<ExerciseAlias>
{
    public void Configure(EntityTypeBuilder<ExerciseAlias> builder)
    {
        builder.ToTable("exercise_aliases");
        builder.HasKey(alias => new { alias.ExerciseId, alias.Alias });
        builder.Property(alias => alias.ExerciseId).HasColumnName("exercise_id");
        builder.Property(alias => alias.Alias)
            .HasColumnName("alias")
            .HasMaxLength(100);
    }
}

internal sealed class ExerciseEquipmentConfiguration
    : IEntityTypeConfiguration<ExerciseEquipment>
{
    public void Configure(EntityTypeBuilder<ExerciseEquipment> builder)
    {
        builder.ToTable("exercise_equipment", table =>
            table.HasCheckConstraint(
                "CK_exercise_equipment_equipment",
                "equipment IN ('Bodyweight', 'Dumbbells', 'Barbell', 'Bench', 'SquatRack', "
                + "'CableMachine', 'ResistanceBands', 'CardioEquipment')"));

        builder.HasKey(equipment => new { equipment.ExerciseId, equipment.Equipment });
        builder.Property(equipment => equipment.ExerciseId).HasColumnName("exercise_id");
        builder.Property(equipment => equipment.Equipment)
            .HasColumnName("equipment")
            .HasConversion<string>()
            .HasMaxLength(32);
    }
}

internal sealed class ExerciseMuscleConfiguration : IEntityTypeConfiguration<ExerciseMuscle>
{
    public void Configure(EntityTypeBuilder<ExerciseMuscle> builder)
    {
        builder.ToTable("exercise_muscles", table =>
        {
            table.HasCheckConstraint(
                "CK_exercise_muscles_muscle",
                "muscle IN ('Quadriceps', 'Hamstrings', 'Glutes', 'Calves', 'Chest', "
                + "'Back', 'Shoulders', 'Biceps', 'Triceps', 'Forearms', 'Core')");
            table.HasCheckConstraint(
                "CK_exercise_muscles_role",
                "role IN ('Primary', 'Secondary')");
        });

        builder.HasKey(muscle => new { muscle.ExerciseId, muscle.Muscle });
        builder.Property(muscle => muscle.ExerciseId).HasColumnName("exercise_id");
        builder.Property(muscle => muscle.Muscle)
            .HasColumnName("muscle")
            .HasConversion<string>()
            .HasMaxLength(24);
        builder.Property(muscle => muscle.Role)
            .HasColumnName("role")
            .HasConversion<string>()
            .HasMaxLength(16);
    }
}

internal sealed class ExerciseCatalogueStateConfiguration
    : IEntityTypeConfiguration<ExerciseCatalogueState>
{
    public void Configure(EntityTypeBuilder<ExerciseCatalogueState> builder)
    {
        builder.ToTable("exercise_catalogue_state", table =>
        {
            table.HasCheckConstraint(
                "CK_exercise_catalogue_state_singleton",
                "id = 1");
            table.HasCheckConstraint(
                "CK_exercise_catalogue_state_version",
                "catalogue_version > 0");
            table.HasCheckConstraint(
                "CK_exercise_catalogue_state_review_status",
                "review_status IN ('RequiresQualifiedReview', 'QualifiedReviewComplete')");
        });

        builder.HasKey(state => state.Id);
        builder.Property(state => state.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(state => state.CatalogueVersion).HasColumnName("catalogue_version");
        builder.Property(state => state.ContentHash)
            .HasColumnName("content_hash")
            .HasMaxLength(64)
            .IsFixedLength();
        builder.Property(state => state.ReviewStatus)
            .HasColumnName("review_status")
            .HasConversion<string>()
            .HasMaxLength(32);
        builder.Property(state => state.ImportedAt).HasColumnName("imported_at");
    }
}

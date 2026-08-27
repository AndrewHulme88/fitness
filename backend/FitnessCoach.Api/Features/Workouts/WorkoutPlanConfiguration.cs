using FitnessCoach.Api.Features.Exercises;
using FitnessCoach.Api.Features.Profiles;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FitnessCoach.Api.Features.Workouts;

internal sealed class WorkoutPlanConfiguration : IEntityTypeConfiguration<WorkoutPlan>
{
    public void Configure(EntityTypeBuilder<WorkoutPlan> builder)
    {
        builder.ToTable("workout_plans", table =>
        {
            table.HasCheckConstraint(
                "CK_workout_plans_name",
                "length(btrim(name)) > 0");
            table.HasCheckConstraint(
                "CK_workout_plans_revision",
                "revision > 0");
        });

        builder.HasKey(workout => workout.Id);
        builder.HasIndex(workout => new { workout.ProfileId, workout.UpdatedAt });

        builder.Property(workout => workout.Id).HasColumnName("id");
        builder.Property(workout => workout.ProfileId).HasColumnName("profile_id");
        builder.Property(workout => workout.Name)
            .HasColumnName("name")
            .HasMaxLength(80);
        builder.Property(workout => workout.Revision)
            .HasColumnName("revision")
            .IsConcurrencyToken();
        builder.Property(workout => workout.CreatedAt).HasColumnName("created_at");
        builder.Property(workout => workout.UpdatedAt).HasColumnName("updated_at");

        builder.HasOne<TrainingProfile>()
            .WithMany()
            .HasForeignKey(workout => workout.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(workout => workout.Exercises)
            .WithOne()
            .HasForeignKey(exercise => exercise.WorkoutPlanId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class WorkoutPlanExerciseConfiguration
    : IEntityTypeConfiguration<WorkoutPlanExercise>
{
    public void Configure(EntityTypeBuilder<WorkoutPlanExercise> builder)
    {
        builder.ToTable("workout_plan_exercises", table =>
        {
            table.HasCheckConstraint(
                "CK_workout_plan_exercises_position",
                "position >= 0 AND position < 20");
            table.HasCheckConstraint(
                "CK_workout_plan_exercises_planned_sets",
                "planned_sets >= 1 AND planned_sets <= 20");
            table.HasCheckConstraint(
                "CK_workout_plan_exercises_repetitions",
                "(minimum_repetitions IS NULL OR minimum_repetitions BETWEEN 1 AND 1000) "
                + "AND (maximum_repetitions IS NULL OR maximum_repetitions BETWEEN 1 AND 1000) "
                + "AND (minimum_repetitions IS NULL OR maximum_repetitions IS NULL "
                + "OR minimum_repetitions <= maximum_repetitions)");
            table.HasCheckConstraint(
                "CK_workout_plan_exercises_target_load",
                "target_load_kilograms IS NULL "
                + "OR target_load_kilograms > 0 AND target_load_kilograms <= 2000");
            table.HasCheckConstraint(
                "CK_workout_plan_exercises_target_duration",
                "target_duration_seconds IS NULL "
                + "OR target_duration_seconds BETWEEN 1 AND 86400");
            table.HasCheckConstraint(
                "CK_workout_plan_exercises_target_distance",
                "target_distance_metres IS NULL "
                + "OR target_distance_metres > 0 AND target_distance_metres <= 1000000");
        });

        builder.HasKey(exercise => new { exercise.WorkoutPlanId, exercise.ExerciseId });
        builder.Property(exercise => exercise.WorkoutPlanId).HasColumnName("workout_plan_id");
        builder.Property(exercise => exercise.ExerciseId).HasColumnName("exercise_id");
        builder.Property(exercise => exercise.Position).HasColumnName("position");
        builder.Property(exercise => exercise.PlannedSets).HasColumnName("planned_sets");
        builder.Property(exercise => exercise.MinimumRepetitions)
            .HasColumnName("minimum_repetitions");
        builder.Property(exercise => exercise.MaximumRepetitions)
            .HasColumnName("maximum_repetitions");
        builder.Property(exercise => exercise.TargetLoadKilograms)
            .HasColumnName("target_load_kilograms")
            .HasPrecision(12, 2);
        builder.Property(exercise => exercise.TargetDurationSeconds)
            .HasColumnName("target_duration_seconds");
        builder.Property(exercise => exercise.TargetDistanceMetres)
            .HasColumnName("target_distance_metres")
            .HasPrecision(12, 2);

        builder.HasOne<Exercise>()
            .WithMany()
            .HasForeignKey(exercise => exercise.ExerciseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

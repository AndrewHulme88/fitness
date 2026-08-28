using FitnessCoach.Api.Features.Exercises;
using FitnessCoach.Api.Features.Profiles;
using FitnessCoach.Api.Features.Workouts;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FitnessCoach.Api.Features.Sessions;

internal sealed class WorkoutSessionConfiguration : IEntityTypeConfiguration<WorkoutSession>
{
    public void Configure(EntityTypeBuilder<WorkoutSession> builder)
    {
        builder.ToTable("workout_sessions", table =>
        {
            table.HasCheckConstraint("CK_workout_sessions_revision", "revision > 0");
            table.HasCheckConstraint(
                "CK_workout_sessions_name",
                "length(btrim(workout_name)) > 0");
            table.HasCheckConstraint(
                "CK_workout_sessions_status",
                "status IN ('Active', 'Completed')");
            table.HasCheckConstraint(
                "CK_workout_sessions_finished",
                "(status = 'Active' AND finished_at IS NULL) "
                + "OR (status = 'Completed' AND finished_at IS NOT NULL)");
        });

        builder.HasKey(item => item.Id);
        builder.HasIndex(item => new { item.ProfileId, item.Status })
            .IsUnique()
            .HasFilter("status = 'Active'");
        builder.HasIndex(item => new { item.ProfileId, item.StartedAt });

        builder.Property(item => item.Id).HasColumnName("id");
        builder.Property(item => item.ProfileId).HasColumnName("profile_id");
        builder.Property(item => item.WorkoutPlanId).HasColumnName("workout_plan_id");
        builder.Property(item => item.WorkoutPlanRevision).HasColumnName("workout_plan_revision");
        builder.Property(item => item.WorkoutName)
            .HasColumnName("workout_name")
            .HasMaxLength(80);
        builder.Property(item => item.Revision)
            .HasColumnName("revision")
            .IsConcurrencyToken();
        builder.Property(item => item.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(16);
        builder.Property(item => item.LastMutationId).HasColumnName("last_mutation_id");
        builder.Property(item => item.StartedAt).HasColumnName("started_at");
        builder.Property(item => item.UpdatedAt).HasColumnName("updated_at");
        builder.Property(item => item.FinishedAt).HasColumnName("finished_at");
        builder.Property(item => item.Notes)
            .HasColumnName("notes")
            .HasMaxLength(2000);

        builder.HasOne<TrainingProfile>()
            .WithMany()
            .HasForeignKey(item => item.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<WorkoutPlan>()
            .WithMany()
            .HasForeignKey(item => item.WorkoutPlanId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(item => item.Exercises)
            .WithOne()
            .HasForeignKey(item => item.WorkoutSessionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class WorkoutSessionExerciseConfiguration
    : IEntityTypeConfiguration<WorkoutSessionExercise>
{
    public void Configure(EntityTypeBuilder<WorkoutSessionExercise> builder)
    {
        builder.ToTable("workout_session_exercises", table =>
        {
            table.HasCheckConstraint(
                "CK_workout_session_exercises_position",
                "position >= 0 AND position < 20");
            table.HasCheckConstraint(
                "CK_workout_session_exercises_planned_sets",
                "planned_sets BETWEEN 1 AND 20");
            table.HasCheckConstraint(
                "CK_workout_session_exercises_tracking_mode",
                "tracking_mode IN ('Repetitions', 'RepetitionsAndLoad', 'Duration', "
                + "'DistanceAndDuration', 'DistanceDurationAndLoad')");
        });

        builder.HasKey(item => new { item.WorkoutSessionId, item.ExerciseId });
        builder.Property(item => item.WorkoutSessionId).HasColumnName("workout_session_id");
        builder.Property(item => item.ExerciseId).HasColumnName("exercise_id");
        builder.Property(item => item.Position).HasColumnName("position");
        builder.Property(item => item.ExerciseName)
            .HasColumnName("exercise_name")
            .HasMaxLength(80);
        builder.Property(item => item.TrackingMode)
            .HasColumnName("tracking_mode")
            .HasConversion<string>()
            .HasMaxLength(32);
        builder.Property(item => item.PrimaryMuscles).HasColumnName("primary_muscles");
        builder.Property(item => item.PlannedSets).HasColumnName("planned_sets");
        builder.Property(item => item.MinimumRepetitions).HasColumnName("minimum_repetitions");
        builder.Property(item => item.MaximumRepetitions).HasColumnName("maximum_repetitions");
        builder.Property(item => item.TargetLoadKilograms)
            .HasColumnName("target_load_kilograms")
            .HasPrecision(12, 2);
        builder.Property(item => item.TargetDurationSeconds).HasColumnName("target_duration_seconds");
        builder.Property(item => item.TargetDistanceMetres)
            .HasColumnName("target_distance_metres")
            .HasPrecision(12, 2);
        builder.Property(item => item.IsSkipped).HasColumnName("is_skipped");
        builder.Property(item => item.Notes)
            .HasColumnName("notes")
            .HasMaxLength(1000);

        builder.HasOne<Exercise>()
            .WithMany()
            .HasForeignKey(item => item.ExerciseId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(item => item.Sets)
            .WithOne()
            .HasForeignKey(item => new { item.WorkoutSessionId, item.ExerciseId })
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class WorkoutSessionSetConfiguration : IEntityTypeConfiguration<WorkoutSessionSet>
{
    public void Configure(EntityTypeBuilder<WorkoutSessionSet> builder)
    {
        builder.ToTable("workout_session_sets", table =>
        {
            table.HasCheckConstraint(
                "CK_workout_session_sets_position",
                "position >= 0 AND position < 20");
            table.HasCheckConstraint(
                "CK_workout_session_sets_completion",
                "(is_completed AND completed_at IS NOT NULL) "
                + "OR (NOT is_completed AND completed_at IS NULL)");
            table.HasCheckConstraint(
                "CK_workout_session_sets_repetitions",
                "actual_repetitions IS NULL OR actual_repetitions BETWEEN 1 AND 1000");
            table.HasCheckConstraint(
                "CK_workout_session_sets_load",
                "actual_load_kilograms IS NULL "
                + "OR actual_load_kilograms > 0 AND actual_load_kilograms <= 2000");
            table.HasCheckConstraint(
                "CK_workout_session_sets_duration",
                "actual_duration_seconds IS NULL OR actual_duration_seconds BETWEEN 1 AND 86400");
            table.HasCheckConstraint(
                "CK_workout_session_sets_distance",
                "actual_distance_metres IS NULL "
                + "OR actual_distance_metres > 0 AND actual_distance_metres <= 1000000");
        });

        builder.HasKey(item => item.Id);
        builder.HasIndex(item => new { item.WorkoutSessionId, item.ExerciseId, item.Position });
        builder.Property(item => item.Id).HasColumnName("id");
        builder.Property(item => item.WorkoutSessionId).HasColumnName("workout_session_id");
        builder.Property(item => item.ExerciseId).HasColumnName("exercise_id");
        builder.Property(item => item.Position).HasColumnName("position");
        builder.Property(item => item.IsCompleted).HasColumnName("is_completed");
        builder.Property(item => item.CompletedAt).HasColumnName("completed_at");
        builder.Property(item => item.ActualRepetitions).HasColumnName("actual_repetitions");
        builder.Property(item => item.ActualLoadKilograms)
            .HasColumnName("actual_load_kilograms")
            .HasPrecision(12, 2);
        builder.Property(item => item.ActualDurationSeconds).HasColumnName("actual_duration_seconds");
        builder.Property(item => item.ActualDistanceMetres)
            .HasColumnName("actual_distance_metres")
            .HasPrecision(12, 2);
    }
}

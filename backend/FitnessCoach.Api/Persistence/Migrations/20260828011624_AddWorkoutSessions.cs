using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

// EF generates inline arrays for composite migration metadata; they are not a runtime hot path.
#pragma warning disable CA1861

namespace FitnessCoach.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkoutSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "workout_sessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workout_plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workout_plan_revision = table.Column<int>(type: "integer", nullable: false),
                    workout_name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    revision = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    last_mutation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    finished_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workout_sessions", x => x.id);
                    table.CheckConstraint("CK_workout_sessions_finished", "(status = 'Active' AND finished_at IS NULL) OR (status = 'Completed' AND finished_at IS NOT NULL)");
                    table.CheckConstraint("CK_workout_sessions_name", "length(btrim(workout_name)) > 0");
                    table.CheckConstraint("CK_workout_sessions_revision", "revision > 0");
                    table.CheckConstraint("CK_workout_sessions_status", "status IN ('Active', 'Completed')");
                    table.ForeignKey(
                        name: "FK_workout_sessions_training_profiles_profile_id",
                        column: x => x.profile_id,
                        principalTable: "training_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_workout_sessions_workout_plans_workout_plan_id",
                        column: x => x.workout_plan_id,
                        principalTable: "workout_plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "workout_session_exercises",
                columns: table => new
                {
                    workout_session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    exercise_id = table.Column<Guid>(type: "uuid", nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false),
                    exercise_name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    tracking_mode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    primary_muscles = table.Column<string[]>(type: "text[]", nullable: false),
                    planned_sets = table.Column<int>(type: "integer", nullable: false),
                    minimum_repetitions = table.Column<int>(type: "integer", nullable: true),
                    maximum_repetitions = table.Column<int>(type: "integer", nullable: true),
                    target_load_kilograms = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    target_duration_seconds = table.Column<int>(type: "integer", nullable: true),
                    target_distance_metres = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    is_skipped = table.Column<bool>(type: "boolean", nullable: false),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workout_session_exercises", x => new { x.workout_session_id, x.exercise_id });
                    table.CheckConstraint("CK_workout_session_exercises_planned_sets", "planned_sets BETWEEN 1 AND 20");
                    table.CheckConstraint("CK_workout_session_exercises_position", "position >= 0 AND position < 20");
                    table.CheckConstraint("CK_workout_session_exercises_tracking_mode", "tracking_mode IN ('Repetitions', 'RepetitionsAndLoad', 'Duration', 'DistanceAndDuration', 'DistanceDurationAndLoad')");
                    table.ForeignKey(
                        name: "FK_workout_session_exercises_exercises_exercise_id",
                        column: x => x.exercise_id,
                        principalTable: "exercises",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_workout_session_exercises_workout_sessions_workout_session_~",
                        column: x => x.workout_session_id,
                        principalTable: "workout_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "workout_session_sets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    workout_session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    exercise_id = table.Column<Guid>(type: "uuid", nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false),
                    is_completed = table.Column<bool>(type: "boolean", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    actual_repetitions = table.Column<int>(type: "integer", nullable: true),
                    actual_load_kilograms = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    actual_duration_seconds = table.Column<int>(type: "integer", nullable: true),
                    actual_distance_metres = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workout_session_sets", x => x.id);
                    table.CheckConstraint("CK_workout_session_sets_completion", "(is_completed AND completed_at IS NOT NULL) OR (NOT is_completed AND completed_at IS NULL)");
                    table.CheckConstraint("CK_workout_session_sets_distance", "actual_distance_metres IS NULL OR actual_distance_metres > 0 AND actual_distance_metres <= 1000000");
                    table.CheckConstraint("CK_workout_session_sets_duration", "actual_duration_seconds IS NULL OR actual_duration_seconds BETWEEN 1 AND 86400");
                    table.CheckConstraint("CK_workout_session_sets_load", "actual_load_kilograms IS NULL OR actual_load_kilograms > 0 AND actual_load_kilograms <= 2000");
                    table.CheckConstraint("CK_workout_session_sets_position", "position >= 0 AND position < 20");
                    table.CheckConstraint("CK_workout_session_sets_repetitions", "actual_repetitions IS NULL OR actual_repetitions BETWEEN 1 AND 1000");
                    table.ForeignKey(
                        name: "FK_workout_session_sets_workout_session_exercises_workout_sess~",
                        columns: x => new { x.workout_session_id, x.exercise_id },
                        principalTable: "workout_session_exercises",
                        principalColumns: new[] { "workout_session_id", "exercise_id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_workout_session_exercises_exercise_id",
                table: "workout_session_exercises",
                column: "exercise_id");

            migrationBuilder.CreateIndex(
                name: "IX_workout_session_sets_workout_session_id_exercise_id_position",
                table: "workout_session_sets",
                columns: new[] { "workout_session_id", "exercise_id", "position" });

            migrationBuilder.CreateIndex(
                name: "IX_workout_sessions_profile_id_started_at",
                table: "workout_sessions",
                columns: new[] { "profile_id", "started_at" });

            migrationBuilder.CreateIndex(
                name: "IX_workout_sessions_profile_id_status",
                table: "workout_sessions",
                columns: new[] { "profile_id", "status" },
                unique: true,
                filter: "status = 'Active'");

            migrationBuilder.CreateIndex(
                name: "IX_workout_sessions_workout_plan_id",
                table: "workout_sessions",
                column: "workout_plan_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "workout_session_sets");

            migrationBuilder.DropTable(
                name: "workout_session_exercises");

            migrationBuilder.DropTable(
                name: "workout_sessions");
        }
    }
}

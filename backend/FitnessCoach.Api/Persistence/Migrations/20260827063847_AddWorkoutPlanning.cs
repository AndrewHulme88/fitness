using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitnessCoach.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkoutPlanning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "workout_plans",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    revision = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workout_plans", x => x.id);
                    table.CheckConstraint("CK_workout_plans_name", "length(btrim(name)) > 0");
                    table.CheckConstraint("CK_workout_plans_revision", "revision > 0");
                    table.ForeignKey(
                        name: "FK_workout_plans_training_profiles_profile_id",
                        column: x => x.profile_id,
                        principalTable: "training_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "workout_plan_exercises",
                columns: table => new
                {
                    workout_plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    exercise_id = table.Column<Guid>(type: "uuid", nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false),
                    planned_sets = table.Column<int>(type: "integer", nullable: false),
                    minimum_repetitions = table.Column<int>(type: "integer", nullable: true),
                    maximum_repetitions = table.Column<int>(type: "integer", nullable: true),
                    target_load_kilograms = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    target_duration_seconds = table.Column<int>(type: "integer", nullable: true),
                    target_distance_metres = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workout_plan_exercises", x => new { x.workout_plan_id, x.exercise_id });
                    table.CheckConstraint("CK_workout_plan_exercises_planned_sets", "planned_sets >= 1 AND planned_sets <= 20");
                    table.CheckConstraint("CK_workout_plan_exercises_position", "position >= 0 AND position < 20");
                    table.CheckConstraint("CK_workout_plan_exercises_repetitions", "(minimum_repetitions IS NULL OR minimum_repetitions BETWEEN 1 AND 1000) AND (maximum_repetitions IS NULL OR maximum_repetitions BETWEEN 1 AND 1000) AND (minimum_repetitions IS NULL OR maximum_repetitions IS NULL OR minimum_repetitions <= maximum_repetitions)");
                    table.CheckConstraint("CK_workout_plan_exercises_target_distance", "target_distance_metres IS NULL OR target_distance_metres > 0 AND target_distance_metres <= 1000000");
                    table.CheckConstraint("CK_workout_plan_exercises_target_duration", "target_duration_seconds IS NULL OR target_duration_seconds BETWEEN 1 AND 86400");
                    table.CheckConstraint("CK_workout_plan_exercises_target_load", "target_load_kilograms IS NULL OR target_load_kilograms > 0 AND target_load_kilograms <= 2000");
                    table.ForeignKey(
                        name: "FK_workout_plan_exercises_exercises_exercise_id",
                        column: x => x.exercise_id,
                        principalTable: "exercises",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_workout_plan_exercises_workout_plans_workout_plan_id",
                        column: x => x.workout_plan_id,
                        principalTable: "workout_plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_workout_plan_exercises_exercise_id",
                table: "workout_plan_exercises",
                column: "exercise_id");

            migrationBuilder.CreateIndex(
                name: "IX_workout_plans_profile_id_updated_at",
                table: "workout_plans",
                columns: ["profile_id", "updated_at"]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "workout_plan_exercises");

            migrationBuilder.DropTable(
                name: "workout_plans");
        }
    }
}

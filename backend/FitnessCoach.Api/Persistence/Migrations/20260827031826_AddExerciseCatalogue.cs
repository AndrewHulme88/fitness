using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitnessCoach.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddExerciseCatalogue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "exercise_catalogue_state",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    catalogue_version = table.Column<int>(type: "integer", nullable: false),
                    content_hash = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    review_status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    imported_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_exercise_catalogue_state", x => x.id);
                    table.CheckConstraint("CK_exercise_catalogue_state_review_status", "review_status IN ('RequiresQualifiedReview', 'QualifiedReviewComplete')");
                    table.CheckConstraint("CK_exercise_catalogue_state_singleton", "id = 1");
                    table.CheckConstraint("CK_exercise_catalogue_state_version", "catalogue_version > 0");
                });

            migrationBuilder.CreateTable(
                name: "exercises",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    slug = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    category = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    movement_pattern = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    tracking_mode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    setup = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    execution = table.Column<string>(type: "character varying(700)", maxLength: 700, nullable: false),
                    safety = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_exercises", x => x.id);
                    table.CheckConstraint("CK_exercises_category", "category IN ('Strength', 'Cardio')");
                    table.CheckConstraint("CK_exercises_movement_pattern", "movement_pattern IN ('Squat', 'Hinge', 'Lunge', 'HorizontalPush', 'VerticalPush', 'HorizontalPull', 'VerticalPull', 'Carry', 'CoreStability', 'ElbowFlexion', 'ElbowExtension', 'CalfRaise', 'Locomotion')");
                    table.CheckConstraint("CK_exercises_tracking_mode", "tracking_mode IN ('Repetitions', 'RepetitionsAndLoad', 'Duration', 'DistanceAndDuration', 'DistanceDurationAndLoad')");
                });

            migrationBuilder.CreateTable(
                name: "exercise_aliases",
                columns: table => new
                {
                    exercise_id = table.Column<Guid>(type: "uuid", nullable: false),
                    alias = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_exercise_aliases", x => new { x.exercise_id, x.alias });
                    table.ForeignKey(
                        name: "FK_exercise_aliases_exercises_exercise_id",
                        column: x => x.exercise_id,
                        principalTable: "exercises",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "exercise_equipment",
                columns: table => new
                {
                    exercise_id = table.Column<Guid>(type: "uuid", nullable: false),
                    equipment = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_exercise_equipment", x => new { x.exercise_id, x.equipment });
                    table.CheckConstraint("CK_exercise_equipment_equipment", "equipment IN ('Bodyweight', 'Dumbbells', 'Barbell', 'Bench', 'SquatRack', 'CableMachine', 'ResistanceBands', 'CardioEquipment')");
                    table.ForeignKey(
                        name: "FK_exercise_equipment_exercises_exercise_id",
                        column: x => x.exercise_id,
                        principalTable: "exercises",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "exercise_muscles",
                columns: table => new
                {
                    exercise_id = table.Column<Guid>(type: "uuid", nullable: false),
                    muscle = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    role = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_exercise_muscles", x => new { x.exercise_id, x.muscle });
                    table.CheckConstraint("CK_exercise_muscles_muscle", "muscle IN ('Quadriceps', 'Hamstrings', 'Glutes', 'Calves', 'Chest', 'Back', 'Shoulders', 'Biceps', 'Triceps', 'Forearms', 'Core')");
                    table.CheckConstraint("CK_exercise_muscles_role", "role IN ('Primary', 'Secondary')");
                    table.ForeignKey(
                        name: "FK_exercise_muscles_exercises_exercise_id",
                        column: x => x.exercise_id,
                        principalTable: "exercises",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_exercises_name",
                table: "exercises",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_exercises_slug",
                table: "exercises",
                column: "slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "exercise_aliases");

            migrationBuilder.DropTable(
                name: "exercise_catalogue_state");

            migrationBuilder.DropTable(
                name: "exercise_equipment");

            migrationBuilder.DropTable(
                name: "exercise_muscles");

            migrationBuilder.DropTable(
                name: "exercises");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1861

namespace FitnessCoach.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCoachWorkoutProposals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "coach_workout_proposals",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workout_id = table.Column<Guid>(type: "uuid", nullable: false),
                    expected_revision = table.Column<int>(type: "integer", nullable: false),
                    rationale = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: false),
                    name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    exercises = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    confirmed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_coach_workout_proposals", x => x.id);
                    table.ForeignKey(
                        name: "FK_coach_workout_proposals_training_profiles_profile_id",
                        column: x => x.profile_id,
                        principalTable: "training_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_coach_workout_proposals_profile_id_created_at",
                table: "coach_workout_proposals",
                columns: new[] { "profile_id", "created_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "coach_workout_proposals");
        }
    }
}

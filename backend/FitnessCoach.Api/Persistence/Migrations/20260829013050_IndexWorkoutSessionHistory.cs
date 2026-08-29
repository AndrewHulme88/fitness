using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitnessCoach.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class IndexWorkoutSessionHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_workout_sessions_profile_id_status_finished_at",
                table: "workout_sessions",
                columns: ["profile_id", "status", "finished_at"]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_workout_sessions_profile_id_status_finished_at",
                table: "workout_sessions");
        }
    }
}

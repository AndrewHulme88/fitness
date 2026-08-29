using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitnessCoach.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkoutHistoryAndProgress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "corrected_at",
                table: "workout_sessions",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "corrected_at",
                table: "workout_sessions");
        }
    }
}

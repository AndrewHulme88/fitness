using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitnessCoach.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTrainingProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "training_profiles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    experience = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    unit_system = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_training_profiles", x => x.id);
                    table.CheckConstraint("CK_training_profiles_experience", "experience IN ('Beginner', 'Intermediate', 'Advanced')");
                    table.CheckConstraint("CK_training_profiles_unit_system", "unit_system IN ('Metric', 'Imperial')");
                });

            migrationBuilder.CreateTable(
                name: "training_profile_equipment",
                columns: table => new
                {
                    profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    equipment = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_training_profile_equipment", x => new { x.profile_id, x.equipment });
                    table.CheckConstraint("CK_training_profile_equipment_equipment", "equipment IN ('Bodyweight', 'Dumbbells', 'Barbell', 'Bench', 'SquatRack', 'CableMachine', 'ResistanceBands', 'CardioEquipment')");
                    table.ForeignKey(
                        name: "FK_training_profile_equipment_training_profiles_profile_id",
                        column: x => x.profile_id,
                        principalTable: "training_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "training_profile_goals",
                columns: table => new
                {
                    profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    goal = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_training_profile_goals", x => new { x.profile_id, x.goal });
                    table.CheckConstraint("CK_training_profile_goals_goal", "goal IN ('BuildStrength', 'BuildMuscle', 'GeneralFitness')");
                    table.ForeignKey(
                        name: "FK_training_profile_goals_training_profiles_profile_id",
                        column: x => x.profile_id,
                        principalTable: "training_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "training_profile_equipment");

            migrationBuilder.DropTable(
                name: "training_profile_goals");

            migrationBuilder.DropTable(
                name: "training_profiles");
        }
    }
}

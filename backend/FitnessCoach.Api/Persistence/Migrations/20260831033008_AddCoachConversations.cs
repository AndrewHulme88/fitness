using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitnessCoach.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCoachConversations : Migration
    {
        private static readonly string[] CoachMessageIndexColumns = ["conversation_id", "created_at"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "coach_conversations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_coach_conversations", x => x.id);
                    table.ForeignKey(
                        name: "FK_coach_conversations_training_profiles_profile_id",
                        column: x => x.profile_id,
                        principalTable: "training_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "coach_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    conversation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    content = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    response_kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    context_sources = table.Column<string[]>(type: "text[]", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_coach_messages", x => x.id);
                    table.CheckConstraint("CK_coach_messages_role", "role IN ('User', 'Coach')");
                    table.ForeignKey(
                        name: "FK_coach_messages_coach_conversations_conversation_id",
                        column: x => x.conversation_id,
                        principalTable: "coach_conversations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_coach_conversations_profile_id",
                table: "coach_conversations",
                column: "profile_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_coach_messages_conversation_id_created_at",
                table: "coach_messages",
                columns: CoachMessageIndexColumns);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "coach_messages");

            migrationBuilder.DropTable(
                name: "coach_conversations");
        }
    }
}

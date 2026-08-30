using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitnessCoach.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddApplicationAccountOwnership : Migration
    {
        private static readonly string[] AccountIdentityColumns = ["issuer", "subject"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "account_id",
                table: "training_profiles",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "application_accounts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    issuer = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    subject = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_application_accounts", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_training_profiles_account_id",
                table: "training_profiles",
                column: "account_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_application_accounts_issuer_subject",
                table: "application_accounts",
                columns: AccountIdentityColumns,
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_training_profiles_application_accounts_account_id",
                table: "training_profiles",
                column: "account_id",
                principalTable: "application_accounts",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_training_profiles_application_accounts_account_id",
                table: "training_profiles");

            migrationBuilder.DropTable(
                name: "application_accounts");

            migrationBuilder.DropIndex(
                name: "IX_training_profiles_account_id",
                table: "training_profiles");

            migrationBuilder.DropColumn(
                name: "account_id",
                table: "training_profiles");
        }
    }
}

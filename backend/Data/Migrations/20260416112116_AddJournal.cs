using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnowledgeBank.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddJournal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "journal-id",
                table: "resources",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "journals",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_journals", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_resources_journal-id",
                table: "resources",
                column: "journal-id");

            migrationBuilder.CreateIndex(
                name: "IX_journals_name",
                table: "journals",
                column: "name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_resources_journals_journal-id",
                table: "resources",
                column: "journal-id",
                principalTable: "journals",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_resources_journals_journal-id",
                table: "resources");

            migrationBuilder.DropTable(
                name: "journals");

            migrationBuilder.DropIndex(
                name: "IX_resources_journal-id",
                table: "resources");

            migrationBuilder.DropColumn(
                name: "journal-id",
                table: "resources");
        }
    }
}

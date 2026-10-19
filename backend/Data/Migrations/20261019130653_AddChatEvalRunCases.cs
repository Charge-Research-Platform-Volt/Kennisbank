using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnowledgeBank.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddChatEvalRunCases : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "cases",
                table: "chat-eval-runs",
                type: "jsonb",
                nullable: false,
                // Existing runs get an empty case list; "" isn't valid JSON, so Postgres would reject it for jsonb
                defaultValue: "[]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "cases",
                table: "chat-eval-runs");
        }
    }
}

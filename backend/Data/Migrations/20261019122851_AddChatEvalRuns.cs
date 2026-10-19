using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnowledgeBank.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddChatEvalRuns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "chat-eval-runs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    startedat = table.Column<DateTime>(name: "started-at", type: "timestamp with time zone", nullable: false),
                    completedat = table.Column<DateTime>(name: "completed-at", type: "timestamp with time zone", nullable: true),
                    model = table.Column<string>(type: "text", nullable: false),
                    label = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    casestotal = table.Column<int>(name: "cases-total", type: "integer", nullable: false),
                    casesdone = table.Column<int>(name: "cases-done", type: "integer", nullable: false),
                    results = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chat-eval-runs", x => x.id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "chat-eval-runs");
        }
    }
}

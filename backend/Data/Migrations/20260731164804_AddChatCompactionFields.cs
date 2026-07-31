using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnowledgeBank.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddChatCompactionFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "context-summary",
                table: "chats",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "last-context-tokens",
                table: "chats",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "summarized-through-created-on",
                table: "chats",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "context-summary",
                table: "chats");

            migrationBuilder.DropColumn(
                name: "last-context-tokens",
                table: "chats");

            migrationBuilder.DropColumn(
                name: "summarized-through-created-on",
                table: "chats");
        }
    }
}

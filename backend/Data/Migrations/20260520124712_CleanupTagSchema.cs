using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnowledgeBank.Data.Migrations
{
    /// <inheritdoc />
    public partial class CleanupTagSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "approved-by",
                table: "tags");

            migrationBuilder.DropColumn(
                name: "approved-on",
                table: "tags");

            migrationBuilder.DropColumn(
                name: "is-approved",
                table: "tags");

            migrationBuilder.DropColumn(
                name: "is-standardized",
                table: "tags");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "approved-by",
                table: "tags",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "approved-on",
                table: "tags",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is-approved",
                table: "tags",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is-standardized",
                table: "tags",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}

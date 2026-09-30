using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnowledgeBank.Data.Migrations
{
    /// <inheritdoc />
    public partial class EmbeddingRetryTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "embedding-failures",
                table: "resources",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "embedding-last-attempt",
                table: "resources",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "embedding-failures",
                table: "entities",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "embedding-last-attempt",
                table: "entities",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "embedding-failures",
                table: "resources");

            migrationBuilder.DropColumn(
                name: "embedding-last-attempt",
                table: "resources");

            migrationBuilder.DropColumn(
                name: "embedding-failures",
                table: "entities");

            migrationBuilder.DropColumn(
                name: "embedding-last-attempt",
                table: "entities");
        }
    }
}

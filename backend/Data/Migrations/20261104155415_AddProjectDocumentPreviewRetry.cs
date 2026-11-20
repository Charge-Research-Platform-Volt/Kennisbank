using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnowledgeBank.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectDocumentPreviewRetry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "preview-failures",
                table: "project-document-versions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "preview-last-attempt",
                table: "project-document-versions",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "preview-failures",
                table: "project-document-versions");

            migrationBuilder.DropColumn(
                name: "preview-last-attempt",
                table: "project-document-versions");
        }
    }
}

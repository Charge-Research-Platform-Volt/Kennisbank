using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnowledgeBank.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCreatedByResource : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ai-generated-tags",
                table: "resources");

            migrationBuilder.RenameColumn(
                name: "creation-date",
                table: "resources",
                newName: "created-on");

            migrationBuilder.AddColumn<Guid>(
                name: "created-by",
                table: "resources",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "created-by",
                table: "resources");

            migrationBuilder.RenameColumn(
                name: "created-on",
                table: "resources",
                newName: "creation-date");

            migrationBuilder.AddColumn<string>(
                name: "ai-generated-tags",
                table: "resources",
                type: "text",
                nullable: true);
        }
    }
}

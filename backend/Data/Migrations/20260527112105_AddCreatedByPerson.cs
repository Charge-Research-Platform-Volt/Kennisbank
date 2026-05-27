using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnowledgeBank.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCreatedByPerson : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "creation-date",
                table: "entities",
                newName: "created-on");

            migrationBuilder.AddColumn<Guid>(
                name: "created-by",
                table: "entities",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "created-by",
                table: "entities");

            migrationBuilder.RenameColumn(
                name: "created-on",
                table: "entities",
                newName: "creation-date");
        }
    }
}

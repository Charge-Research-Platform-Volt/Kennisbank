using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnowledgeBank.Data.Migrations
{
    /// <inheritdoc />
    public partial class ResourceTypeNotRequired : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_resources_resource-types_type-id",
                table: "resources");

            migrationBuilder.AlterColumn<Guid>(
                name: "type-id",
                table: "resources",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddForeignKey(
                name: "FK_resources_resource-types_type-id",
                table: "resources",
                column: "type-id",
                principalTable: "resource-types",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_resources_resource-types_type-id",
                table: "resources");

            migrationBuilder.AlterColumn<Guid>(
                name: "type-id",
                table: "resources",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_resources_resource-types_type-id",
                table: "resources",
                column: "type-id",
                principalTable: "resource-types",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnowledgeBank.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdatedOrganisationRelations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "OrganisationId",
                table: "resource-author",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_resource-author_OrganisationId",
                table: "resource-author",
                column: "OrganisationId");

            migrationBuilder.AddForeignKey(
                name: "FK_resource-author_organisations_OrganisationId",
                table: "resource-author",
                column: "OrganisationId",
                principalTable: "organisations",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_resource-author_organisations_OrganisationId",
                table: "resource-author");

            migrationBuilder.DropIndex(
                name: "IX_resource-author_OrganisationId",
                table: "resource-author");

            migrationBuilder.DropColumn(
                name: "OrganisationId",
                table: "resource-author");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnowledgeBank.Data.Migrations
{
    /// <inheritdoc />
    public partial class AuthorRelationsUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_resource-author_organisations_OrganisationId",
                table: "resource-author");

            migrationBuilder.RenameColumn(
                name: "OrganisationId",
                table: "resource-author",
                newName: "OrganisationAuthorId");

            migrationBuilder.RenameIndex(
                name: "IX_resource-author_OrganisationId",
                table: "resource-author",
                newName: "IX_resource-author_OrganisationAuthorId");

            migrationBuilder.AddForeignKey(
                name: "FK_resource-author_organisations_OrganisationAuthorId",
                table: "resource-author",
                column: "OrganisationAuthorId",
                principalTable: "organisations",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_resource-author_organisations_OrganisationAuthorId",
                table: "resource-author");

            migrationBuilder.RenameColumn(
                name: "OrganisationAuthorId",
                table: "resource-author",
                newName: "OrganisationId");

            migrationBuilder.RenameIndex(
                name: "IX_resource-author_OrganisationAuthorId",
                table: "resource-author",
                newName: "IX_resource-author_OrganisationId");

            migrationBuilder.AddForeignKey(
                name: "FK_resource-author_organisations_OrganisationId",
                table: "resource-author",
                column: "OrganisationId",
                principalTable: "organisations",
                principalColumn: "id");
        }
    }
}

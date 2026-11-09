using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnowledgeBank.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectDocumentFolders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_project-documents_project-id",
                table: "project-documents");

            migrationBuilder.AddColumn<Guid>(
                name: "folder-id",
                table: "project-documents",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_project-documents_folder-id",
                table: "project-documents",
                column: "folder-id");

            migrationBuilder.CreateIndex(
                name: "IX_project-documents_project-id_folder-id",
                table: "project-documents",
                columns: new[] { "project-id", "folder-id" });

            migrationBuilder.AddForeignKey(
                name: "FK_project-documents_projects_folder-id",
                table: "project-documents",
                column: "folder-id",
                principalTable: "projects",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_project-documents_projects_folder-id",
                table: "project-documents");

            migrationBuilder.DropIndex(
                name: "IX_project-documents_folder-id",
                table: "project-documents");

            migrationBuilder.DropIndex(
                name: "IX_project-documents_project-id_folder-id",
                table: "project-documents");

            migrationBuilder.DropColumn(
                name: "folder-id",
                table: "project-documents");

            migrationBuilder.CreateIndex(
                name: "IX_project-documents_project-id",
                table: "project-documents",
                column: "project-id");
        }
    }
}

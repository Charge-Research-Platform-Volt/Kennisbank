using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Pgvector;

#nullable disable

namespace KnowledgeBank.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectDocumentChunks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "project-document-chunks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    documentid = table.Column<Guid>(name: "document-id", type: "uuid", nullable: false),
                    versionid = table.Column<Guid>(name: "version-id", type: "uuid", nullable: false),
                    chunkpart = table.Column<int>(name: "chunk-part", type: "integer", nullable: false),
                    chunktext = table.Column<string>(name: "chunk-text", type: "text", nullable: false),
                    embedding = table.Column<Vector>(type: "vector(1024)", nullable: true),
                    createdat = table.Column<DateTime>(name: "created-at", type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project-document-chunks", x => x.id);
                    table.ForeignKey(
                        name: "FK_project-document-chunks_project-documents_document-id",
                        column: x => x.documentid,
                        principalTable: "project-documents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_project-document-chunks_document-id",
                table: "project-document-chunks",
                column: "document-id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "project-document-chunks");
        }
    }
}

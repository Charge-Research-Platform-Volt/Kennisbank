using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace KnowledgeBank.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "project-document-types",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    createdby = table.Column<Guid>(name: "created-by", type: "uuid", nullable: true),
                    createdon = table.Column<DateTime>(name: "created-on", type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project-document-types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "project-documents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    projectid = table.Column<Guid>(name: "project-id", type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    typeid = table.Column<Guid>(name: "type-id", type: "uuid", nullable: false),
                    visibility = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    generalaccesstype = table.Column<string>(name: "general-access-type", type: "character varying(10)", maxLength: 10, nullable: false),
                    source = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    createdby = table.Column<string>(name: "created-by", type: "text", nullable: false),
                    createdon = table.Column<DateTime>(name: "created-on", type: "timestamp with time zone", nullable: false),
                    updatedon = table.Column<DateTime>(name: "updated-on", type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project-documents", x => x.id);
                    table.ForeignKey(
                        name: "FK_project-documents_project-document-types_type-id",
                        column: x => x.typeid,
                        principalTable: "project-document-types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_project-documents_projects_project-id",
                        column: x => x.projectid,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "project-document-access",
                columns: table => new
                {
                    documentid = table.Column<Guid>(name: "document-id", type: "uuid", nullable: false),
                    userid = table.Column<string>(name: "user-id", type: "text", nullable: false),
                    accesstype = table.Column<string>(name: "access-type", type: "character varying(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project-document-access", x => new { x.documentid, x.userid });
                    table.ForeignKey(
                        name: "FK_project-document-access_AspNetUsers_user-id",
                        column: x => x.userid,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_project-document-access_project-documents_document-id",
                        column: x => x.documentid,
                        principalTable: "project-documents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "project-document-versions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    documentid = table.Column<Guid>(name: "document-id", type: "uuid", nullable: false),
                    versionnumber = table.Column<int>(name: "version-number", type: "integer", nullable: false),
                    extension = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    originalfilename = table.Column<string>(name: "original-file-name", type: "text", nullable: false),
                    sizebytes = table.Column<long>(name: "size-bytes", type: "bigint", nullable: true),
                    note = table.Column<string>(type: "text", nullable: true),
                    uploadedby = table.Column<string>(name: "uploaded-by", type: "text", nullable: false),
                    uploadedon = table.Column<DateTime>(name: "uploaded-on", type: "timestamp with time zone", nullable: false),
                    previewstatus = table.Column<string>(name: "preview-status", type: "text", nullable: false),
                    embeddingstatus = table.Column<string>(name: "embedding-status", type: "text", nullable: false),
                    embeddingerror = table.Column<string>(name: "embedding-error", type: "text", nullable: true),
                    embeddingfailures = table.Column<int>(name: "embedding-failures", type: "integer", nullable: false),
                    embeddinglastattempt = table.Column<DateTime>(name: "embedding-last-attempt", type: "timestamp with time zone", nullable: true),
                    extractedtext = table.Column<string>(name: "extracted-text", type: "text", nullable: true),
                    externalid = table.Column<string>(name: "external-id", type: "text", nullable: true),
                    externalrevisionid = table.Column<string>(name: "external-revision-id", type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project-document-versions", x => x.id);
                    table.ForeignKey(
                        name: "FK_project-document-versions_project-documents_document-id",
                        column: x => x.documentid,
                        principalTable: "project-documents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "project-document-types",
                columns: new[] { "id", "created-by", "created-on", "name" },
                values: new object[,]
                {
                    { new Guid("7e1fbccd-5af9-4270-8133-0191eae9203d"), null, new DateTime(2026, 10, 29, 0, 0, 0, 0, DateTimeKind.Utc), "Draft research report" },
                    { new Guid("8c4f36d0-0c30-48c6-a073-816a7bfa87d0"), null, new DateTime(2026, 10, 29, 0, 0, 0, 0, DateTimeKind.Utc), "Project plan" },
                    { new Guid("9b3eeb99-8bb2-4d92-9b54-aa27f6e20657"), null, new DateTime(2026, 10, 29, 0, 0, 0, 0, DateTimeKind.Utc), "Minutes" },
                    { new Guid("ea01f3cd-4cbe-43cf-bd39-3bb5ba5f26f8"), null, new DateTime(2026, 10, 29, 0, 0, 0, 0, DateTimeKind.Utc), "Evaluation" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_project-document-access_user-id",
                table: "project-document-access",
                column: "user-id");

            migrationBuilder.CreateIndex(
                name: "IX_project-document-types_name",
                table: "project-document-types",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_project-document-versions_document-id_version-number",
                table: "project-document-versions",
                columns: new[] { "document-id", "version-number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_project-documents_project-id",
                table: "project-documents",
                column: "project-id");

            migrationBuilder.CreateIndex(
                name: "IX_project-documents_type-id",
                table: "project-documents",
                column: "type-id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "project-document-access");

            migrationBuilder.DropTable(
                name: "project-document-versions");

            migrationBuilder.DropTable(
                name: "project-documents");

            migrationBuilder.DropTable(
                name: "project-document-types");
        }
    }
}

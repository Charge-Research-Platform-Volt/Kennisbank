using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnowledgeBank.Data.Migrations
{
    /// <inheritdoc />
    public partial class RefactorProjectSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add resources.abstract before data migration reads it
            migrationBuilder.AddColumn<string>(
                name: "abstract",
                table: "resources",
                type: "text",
                nullable: true);

            // Migrate document abstract → resources.abstract
            migrationBuilder.Sql(@"
                UPDATE resources r
                SET ""abstract"" = dm.abstract
                FROM ""document-metadata"" dm
                WHERE dm.""resource-id"" = r.id
            ");

            // Migrate website URL → resources.source-url (only where not already set)
            migrationBuilder.Sql(@"
                UPDATE resources r
                SET ""source-url"" = wm.url
                FROM ""website-metadata"" wm
                WHERE wm.""resource-id"" = r.id
                AND (r.""source-url"" IS NULL OR r.""source-url"" = '')
            ");

            // Drop old metadata and source tables
            migrationBuilder.DropTable(name: "audio-metadata");
            migrationBuilder.DropTable(name: "document-metadata");
            migrationBuilder.DropTable(name: "video-metadata");
            migrationBuilder.DropTable(name: "website-metadata");

            // Fix projects table: drop deletion-date, rename creation-date → created-on, add created-by
            migrationBuilder.DropColumn(name: "deletion-date", table: "projects");

            migrationBuilder.RenameColumn(
                name: "creation-date",
                table: "projects",
                newName: "created-on");

            migrationBuilder.AddColumn<Guid>(
                name: "created-by",
                table: "projects",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            // Rename project-creator → project-member (preserves existing rows)
            migrationBuilder.RenameTable(
                name: "project-creator",
                newName: "project-member");

            migrationBuilder.RenameColumn(
                name: "creator-id",
                table: "project-member",
                newName: "member-id");

            migrationBuilder.RenameIndex(
                name: "IX_project-creator_creator-id",
                table: "project-member",
                newName: "IX_project-member_member-id");

            migrationBuilder.Sql(@"ALTER TABLE ""project-member"" RENAME CONSTRAINT ""PK_project-creator"" TO ""PK_project-member"";");
            migrationBuilder.Sql(@"ALTER TABLE ""project-member"" RENAME CONSTRAINT ""FK_project-creator_AspNetUsers_creator-id"" TO ""FK_project-member_AspNetUsers_member-id"";");
            migrationBuilder.Sql(@"ALTER TABLE ""project-member"" RENAME CONSTRAINT ""FK_project-creator_projects_project-id"" TO ""FK_project-member_projects_project-id"";");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reverse project-member → project-creator
            migrationBuilder.Sql(@"ALTER TABLE ""project-member"" RENAME CONSTRAINT ""PK_project-member"" TO ""PK_project-creator"";");
            migrationBuilder.Sql(@"ALTER TABLE ""project-member"" RENAME CONSTRAINT ""FK_project-member_AspNetUsers_member-id"" TO ""FK_project-creator_AspNetUsers_creator-id"";");
            migrationBuilder.Sql(@"ALTER TABLE ""project-member"" RENAME CONSTRAINT ""FK_project-member_projects_project-id"" TO ""FK_project-creator_projects_project-id"";");

            migrationBuilder.RenameIndex(
                name: "IX_project-member_member-id",
                table: "project-member",
                newName: "IX_project-creator_creator-id");

            migrationBuilder.RenameColumn(
                name: "member-id",
                table: "project-member",
                newName: "creator-id");

            migrationBuilder.RenameTable(
                name: "project-member",
                newName: "project-creator");

            // Reverse projects column changes
            migrationBuilder.DropColumn(name: "created-by", table: "projects");

            migrationBuilder.RenameColumn(
                name: "created-on",
                table: "projects",
                newName: "creation-date");

            migrationBuilder.AddColumn<DateTime>(
                name: "deletion-date",
                table: "projects",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            // Recreate old metadata tables (data not restored)
            migrationBuilder.CreateTable(
                name: "audio-metadata",
                columns: table => new
                {
                    resourceid = table.Column<Guid>(name: "resource-id", type: "uuid", nullable: false),
                    length = table.Column<decimal>(type: "numeric(20,0)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audio-metadata", x => x.resourceid);
                    table.ForeignKey(
                        name: "FK_audio-metadata_resources_resource-id",
                        column: x => x.resourceid,
                        principalTable: "resources",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "document-metadata",
                columns: table => new
                {
                    resourceid = table.Column<Guid>(name: "resource-id", type: "uuid", nullable: false),
                    @abstract = table.Column<string>(name: "abstract", type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_document-metadata", x => x.resourceid);
                    table.ForeignKey(
                        name: "FK_document-metadata_resources_resource-id",
                        column: x => x.resourceid,
                        principalTable: "resources",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "video-metadata",
                columns: table => new
                {
                    resourceid = table.Column<Guid>(name: "resource-id", type: "uuid", nullable: false),
                    length = table.Column<decimal>(type: "numeric(20,0)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_video-metadata", x => x.resourceid);
                    table.ForeignKey(
                        name: "FK_video-metadata_resources_resource-id",
                        column: x => x.resourceid,
                        principalTable: "resources",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "website-metadata",
                columns: table => new
                {
                    resourceid = table.Column<Guid>(name: "resource-id", type: "uuid", nullable: false),
                    accessedon = table.Column<DateTime>(name: "accessed-on", type: "timestamp with time zone", nullable: true),
                    url = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_website-metadata", x => x.resourceid);
                    table.ForeignKey(
                        name: "FK_website-metadata_resources_resource-id",
                        column: x => x.resourceid,
                        principalTable: "resources",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Drop abstract from resources
            migrationBuilder.DropColumn(name: "abstract", table: "resources");

            migrationBuilder.CreateIndex(
                name: "IX_project-creator_creator-id",
                table: "project-creator",
                column: "creator-id");
        }
    }
}
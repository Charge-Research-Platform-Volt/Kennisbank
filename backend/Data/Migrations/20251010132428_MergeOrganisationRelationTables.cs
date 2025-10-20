using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnowledgeBank.Data.Migrations
{
    /// <inheritdoc />
    public partial class MergeOrganisationRelationTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // First, copy all data from resource-related_organisation into resource-organisation
            migrationBuilder.Sql(@"
                INSERT INTO ""resource-organisation"" (""resource-id"", ""organisation-id"", ""role"")
                SELECT ""resource-id"", ""organisation-id"", ""role""
                FROM ""resource-related_organisation""
                ON CONFLICT (""resource-id"", ""organisation-id"") DO NOTHING;
            ");

            // Then drop the old table
            migrationBuilder.DropTable(
                name: "resource-related_organisation");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "resource-related_organisation",
                columns: table => new
                {
                    resourceid = table.Column<Guid>(name: "resource-id", type: "uuid", nullable: false),
                    organisationid = table.Column<Guid>(name: "organisation-id", type: "uuid", nullable: false),
                    role = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_resource-related_organisation", x => new { x.resourceid, x.organisationid });
                    table.ForeignKey(
                        name: "FK_resource-related_organisation_organisations_organisation-id",
                        column: x => x.organisationid,
                        principalTable: "organisations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_resource-related_organisation_resources_resource-id",
                        column: x => x.resourceid,
                        principalTable: "resources",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_resource-related_organisation_organisation-id",
                table: "resource-related_organisation",
                column: "organisation-id");
        }
    }
}

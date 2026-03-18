using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnowledgeBank.Data.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceProjectResourceWithProjectItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "project-item",
                columns: table => new
                {
                    projectid = table.Column<Guid>(name: "project-id", type: "uuid", nullable: false),
                    itemid = table.Column<Guid>(name: "item-id", type: "uuid", nullable: false),
                    addedby = table.Column<string>(name: "added-by", type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project-item", x => new { x.projectid, x.itemid });
                    table.ForeignKey(
                        name: "FK_project-item_projects_project-id",
                        column: x => x.projectid,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql(@"
                INSERT INTO ""project-item"" (""project-id"", ""item-id"", ""added-by"")
                SELECT ""project-id"", ""resource-id"", ""added-by""
                FROM ""project-resource"";
            ");

            migrationBuilder.DropTable(
                name: "project-resource");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "project-item");

            migrationBuilder.CreateTable(
                name: "project-resource",
                columns: table => new
                {
                    projectid = table.Column<Guid>(name: "project-id", type: "uuid", nullable: false),
                    resourceid = table.Column<Guid>(name: "resource-id", type: "uuid", nullable: false),
                    addedby = table.Column<string>(name: "added-by", type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project-resource", x => new { x.projectid, x.resourceid });
                    table.ForeignKey(
                        name: "FK_project-resource_projects_project-id",
                        column: x => x.projectid,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_project-resource_resources_resource-id",
                        column: x => x.resourceid,
                        principalTable: "resources",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_project-resource_resource-id",
                table: "project-resource",
                column: "resource-id");
        }
    }
}

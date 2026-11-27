using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnowledgeBank.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddResourceTexts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "resource-texts",
                columns: table => new
                {
                    resourceid = table.Column<Guid>(name: "resource-id", type: "uuid", nullable: false),
                    text = table.Column<string>(type: "text", nullable: false),
                    pagestarts = table.Column<int[]>(name: "page-starts", type: "integer[]", nullable: true),
                    extractedat = table.Column<DateTime>(name: "extracted-at", type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_resource-texts", x => x.resourceid);
                    table.ForeignKey(
                        name: "FK_resource-texts_resources_resource-id",
                        column: x => x.resourceid,
                        principalTable: "resources",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "resource-texts");
        }
    }
}

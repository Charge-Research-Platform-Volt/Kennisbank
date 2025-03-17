using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "doc-tag",
                columns: table => new
                {
                    docid = table.Column<Guid>(name: "doc-id", type: "uuid", nullable: false),
                    tagid = table.Column<Guid>(name: "tag-id", type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_doc-tag", x => new { x.docid, x.tagid });
                });

            migrationBuilder.CreateTable(
                name: "files",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    filetype = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_files", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tags",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tags", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tags_name",
                table: "tags",
                column: "name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "doc-tag");

            migrationBuilder.DropTable(
                name: "files");

            migrationBuilder.DropTable(
                name: "tags");
        }
    }
}

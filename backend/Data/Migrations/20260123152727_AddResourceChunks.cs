using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnowledgeBank.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddResourceChunks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "resource-chunks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    resourceid = table.Column<Guid>(name: "resource-id", type: "uuid", nullable: false),
                    chunktype = table.Column<string>(name: "chunk-type", type: "text", nullable: false),
                    chunktext = table.Column<string>(name: "chunk-text", type: "text", nullable: false),
                    chunkpart = table.Column<int>(name: "chunk-part", type: "integer", nullable: false),
                    embedding = table.Column<float[]>(type: "vector(1536)", nullable: true),
                    createdat = table.Column<DateTime>(name: "created-at", type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_resource-chunks", x => x.id);
                    table.ForeignKey(
                        name: "FK_resource-chunks_resources_resource-id",
                        column: x => x.resourceid,
                        principalTable: "resources",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "id_resource_chunks_resource_part",
                table: "resource-chunks",
                columns: new[] { "resource-id", "chunk-part" });

            migrationBuilder.CreateIndex(
                name: "idx_resource_chunks_resource_id",
                table: "resource-chunks",
                column: "resource-id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "resource-chunks");
        }
    }
}

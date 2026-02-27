using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Pgvector;

#nullable disable

namespace KnowledgeBank.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEntityChunks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "entity-chunks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    entityid = table.Column<Guid>(name: "entity-id", type: "uuid", nullable: false),
                    chunktype = table.Column<string>(name: "chunk-type", type: "text", nullable: false),
                    chunktext = table.Column<string>(name: "chunk-text", type: "text", nullable: false),
                    chunkpart = table.Column<int>(name: "chunk-part", type: "integer", nullable: false),
                    embedding = table.Column<Vector>(type: "vector(1536)", nullable: true),
                    createdat = table.Column<DateTime>(name: "created-at", type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_entity-chunks", x => x.id);
                    table.ForeignKey(
                        name: "FK_entity-chunks_entities_entity-id",
                        column: x => x.entityid,
                        principalTable: "entities",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_entity_chunks_entity_id",
                table: "entity-chunks",
                column: "entity-id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "entity-chunks");
        }
    }
}

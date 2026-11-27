using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnowledgeBank.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddResourceChunkPositions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "end-char",
                table: "resource-chunks",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "start-char",
                table: "resource-chunks",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "end-char",
                table: "resource-chunks");

            migrationBuilder.DropColumn(
                name: "start-char",
                table: "resource-chunks");
        }
    }
}

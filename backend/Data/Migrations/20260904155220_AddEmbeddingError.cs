using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnowledgeBank.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEmbeddingError : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "embedding-error",
                table: "resources",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "embedding-error",
                table: "entities",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "embedding-error",
                table: "resources");

            migrationBuilder.DropColumn(
                name: "embedding-error",
                table: "entities");
        }
    }
}

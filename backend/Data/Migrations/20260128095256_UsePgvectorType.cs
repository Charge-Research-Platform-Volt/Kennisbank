using Microsoft.EntityFrameworkCore.Migrations;
using Pgvector;

#nullable disable

namespace KnowledgeBank.Data.Migrations
{
    /// <inheritdoc />
    public partial class UsePgvectorType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Vector>(
                name: "embedding",
                table: "resource-chunks",
                type: "vector(1536)",
                nullable: true,
                oldClrType: typeof(float[]),
                oldType: "real[]",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<float[]>(
                name: "embedding",
                table: "resource-chunks",
                type: "real[]",
                nullable: true,
                oldClrType: typeof(Vector),
                oldType: "vector(1536)",
                oldNullable: true);
        }
    }
}

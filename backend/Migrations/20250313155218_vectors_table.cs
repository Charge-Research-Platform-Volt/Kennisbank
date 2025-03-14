using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class vectors_table : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "file_vectors",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_id = table.Column<Guid>(type: "uuid", nullable: false),
                    vector = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_file_vectors", x => x.id);
                    table.ForeignKey(
                        name: "FK_file_vectors_files_file_id",
                        column: x => x.file_id,
                        principalTable: "files",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_file_vectors_file_id",
                table: "file_vectors",
                column: "file_id",
                unique: true);

            // Convert the vector column from text to tsvector
            migrationBuilder.Sql("ALTER TABLE file_vectors ALTER COLUMN vector SET DATA TYPE tsvector USING vector::tsvector;");

            // Create GIN index for fast search
            migrationBuilder.Sql("CREATE INDEX idx_file_vector ON file_vectors USING GIN(vector);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS idx_file_vector;");

            migrationBuilder.DropTable(
                name: "file_vectors");
        }
    }
}

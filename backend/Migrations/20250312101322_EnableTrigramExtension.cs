using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class EnableTrigramExtension : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pg_trgm;");
            migrationBuilder.Sql("CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_name_trgm ON files USING GIN (name gin_trgm_ops);", suppressTransaction: true);
            migrationBuilder.Sql("CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_description_trgm ON files USING GIN (description gin_trgm_ops);", suppressTransaction: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS idx_name_trgm;", suppressTransaction: true);
            migrationBuilder.Sql("DROP INDEX IF EXISTS idx_description_trgm;", suppressTransaction: true);
            migrationBuilder.Sql("DROP EXTENSION IF EXISTS pg_trgm;");
        }
    }
}

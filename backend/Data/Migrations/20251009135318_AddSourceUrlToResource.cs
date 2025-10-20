using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnowledgeBank.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSourceUrlToResource : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "source-url",
                table: "resources",
                type: "text",
                nullable: true);

            // Migrate existing source URLs from ResourceSourceRelations to Resource.SourceUrl
            // Takes the first source URL for each resource
            migrationBuilder.Sql(@"
                UPDATE resources r
                SET ""source-url"" = s.""url""
                FROM (
                    SELECT DISTINCT ON (""resource-id"") ""resource-id"", ""url""
                    FROM ""resource-source""
                    ORDER BY ""resource-id""
                ) s
                WHERE r.""id"" = s.""resource-id""
            ");

            // Drop the old resource-source table since we now use SourceUrl column
            migrationBuilder.DropTable(
                name: "resource-source");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "source-url",
                table: "resources");
        }
    }
}

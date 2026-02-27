using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnowledgeBank.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixAuthorForeignKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop the old FK that incorrectly pointed to persons — use IF EXISTS in case it was already removed
            migrationBuilder.Sql(@"
                ALTER TABLE ""resource-author"" DROP CONSTRAINT IF EXISTS ""FK_resource-author_persons_author-id"";
            ");

            // Add correct FK pointing to entities — only if it doesn't already exist
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_constraint WHERE conname = 'FK_resource-author_entities_author-id'
                    ) THEN
                        ALTER TABLE ""resource-author"" ADD CONSTRAINT ""FK_resource-author_entities_author-id""
                        FOREIGN KEY (""author-id"") REFERENCES entities (id);
                    END IF;
                END $$;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_resource-author_entities_author-id",
                table: "resource-author");

            migrationBuilder.AddForeignKey(
                name: "FK_resource-author_persons_author-id",
                table: "resource-author",
                column: "author-id",
                principalTable: "persons",
                principalColumn: "id");
        }
    }
}

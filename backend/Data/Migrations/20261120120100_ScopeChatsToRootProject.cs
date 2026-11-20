using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnowledgeBank.Data.Migrations
{
    /// <inheritdoc />
    public partial class ScopeChatsToRootProject : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Chats started on a folder page move to their root project, so they stay in the project's history.
            // Walks each chat's project up the folder tree; the step without a parent is the root. Chats of folders
            // deleted earlier have no parent row left, so they stay as they are (they were already invisible)
            migrationBuilder.Sql("""
                WITH RECURSIVE ancestors AS (
                    SELECT c."id" AS chat_id, c."project-id" AS project_id
                    FROM "chats" c
                    WHERE c."project-id" IS NOT NULL
                    UNION ALL
                    SELECT a.chat_id, f."parent-id"
                    FROM ancestors a
                    JOIN "project-folder" f ON f."child-id" = a.project_id
                )
                UPDATE "chats" c
                SET "project-id" = a.project_id
                FROM ancestors a
                WHERE a.chat_id = c."id"
                  AND NOT EXISTS (SELECT 1 FROM "project-folder" f WHERE f."child-id" = a.project_id)
                  AND c."project-id" <> a.project_id;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Nothing to undo: once moved, there's no record of which folder a chat was started in
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnowledgeBank.Data.Migrations
{
    /// <inheritdoc />
    public partial class UnifyAuthorRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Check and drop the foreign key constraint for OrganisationAuthorId if it exists
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM pg_constraint
                        WHERE conname = 'FK_resource-author_organisations_OrganisationAuthorId'
                    ) THEN
                        ALTER TABLE ""resource-author"" DROP CONSTRAINT ""FK_resource-author_organisations_OrganisationAuthorId"";
                    END IF;
                END $$;
            ");

            // Check and drop the index for OrganisationAuthorId if it exists
            migrationBuilder.Sql(@"
                DROP INDEX IF EXISTS ""IX_resource-author_OrganisationAuthorId"";
            ");

            // Copy any organisation author IDs to the author-id column
            // This migrates existing organisation authors to use the unified author-id column
            migrationBuilder.Sql(@"
                UPDATE ""resource-author""
                SET ""author-id"" = ""OrganisationAuthorId""
                WHERE ""OrganisationAuthorId"" IS NOT NULL;
            ");

            // Drop the OrganisationAuthorId column if it exists
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_name = 'resource-author'
                        AND column_name = 'OrganisationAuthorId'
                    ) THEN
                        ALTER TABLE ""resource-author"" DROP COLUMN ""OrganisationAuthorId"";
                    END IF;
                END $$;
            ");

            // Note: author-id column now contains IDs from both persons and organisations tables
            // The ResourceGridItem materialized view will be used for joins/navigation
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Re-add the OrganisationAuthorId column if it doesn't exist
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_name = 'resource-author'
                        AND column_name = 'OrganisationAuthorId'
                    ) THEN
                        ALTER TABLE ""resource-author"" ADD COLUMN ""OrganisationAuthorId"" uuid NULL;
                    END IF;
                END $$;
            ");

            // Re-create the index if it doesn't exist
            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ""IX_resource-author_OrganisationAuthorId""
                ON ""resource-author"" (""OrganisationAuthorId"");
            ");

            // Re-add the foreign key constraints if they don't exist
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_constraint
                        WHERE conname = 'FK_resource-author_organisations_OrganisationAuthorId'
                    ) THEN
                        ALTER TABLE ""resource-author""
                        ADD CONSTRAINT ""FK_resource-author_organisations_OrganisationAuthorId""
                        FOREIGN KEY (""OrganisationAuthorId"")
                        REFERENCES ""organisations"" (""id"");
                    END IF;
                END $$;
            ");

            // Note: This down migration does NOT migrate data back from author-id to OrganisationAuthorId
            // You would need to manually identify which authors are organisations if you need to roll back
        }
    }
}

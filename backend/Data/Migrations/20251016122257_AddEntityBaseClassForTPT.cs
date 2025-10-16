using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnowledgeBank.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEntityBaseClassForTPT : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Drop old foreign keys and indexes from resource-author (if they exist)
            // These may not exist if previous migrations already removed them
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM pg_constraint
                        WHERE conname = 'FK_resource-author_organisations_OrganisationId'
                    ) THEN
                        ALTER TABLE ""resource-author"" DROP CONSTRAINT ""FK_resource-author_organisations_OrganisationId"";
                    END IF;
                END $$;
            ");

            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM pg_constraint
                        WHERE conname = 'FK_resource-author_persons_PersonId'
                    ) THEN
                        ALTER TABLE ""resource-author"" DROP CONSTRAINT ""FK_resource-author_persons_PersonId"";
                    END IF;
                END $$;
            ");

            migrationBuilder.Sql(@"
                DROP INDEX IF EXISTS ""IX_resource-author_OrganisationId"";
                DROP INDEX IF EXISTS ""IX_resource-author_PersonId"";
            ");

            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_name = 'resource-author'
                        AND column_name = 'OrganisationId'
                    ) THEN
                        ALTER TABLE ""resource-author"" DROP COLUMN ""OrganisationId"";
                    END IF;
                END $$;
            ");

            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_name = 'resource-author'
                        AND column_name = 'PersonId'
                    ) THEN
                        ALTER TABLE ""resource-author"" DROP COLUMN ""PersonId"";
                    END IF;
                END $$;
            ");

            // Step 2: Create the entities table
            migrationBuilder.CreateTable(
                name: "entities",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    emailaddress = table.Column<string>(name: "email-address", type: "text", nullable: true),
                    creationdate = table.Column<DateTime>(name: "creation-date", type: "timestamp with time zone", nullable: false),
                    trashed = table.Column<bool>(type: "boolean", nullable: false),
                    trashdate = table.Column<DateTime>(name: "trash-date", type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_entities", x => x.id);
                });

            // Step 3: Migrate data from persons to entities
            migrationBuilder.Sql(@"
                INSERT INTO entities (id, name, description, ""email-address"", ""creation-date"", trashed, ""trash-date"")
                SELECT id, name, description, ""email-address"", ""creation-date"", trashed, ""trash-date""
                FROM persons;
            ");

            // Step 4: Migrate data from organisations to entities
            migrationBuilder.Sql(@"
                INSERT INTO entities (id, name, description, ""email-address"", ""creation-date"", trashed, ""trash-date"")
                SELECT id, name, description, ""email-address"", ""creation-date"", trashed, ""trash-date""
                FROM organisations;
            ");

            // Step 4.5: Drop materialized views that depend on person/organisation columns
            migrationBuilder.Sql(@"
                DROP TRIGGER IF EXISTS refresh_grid_on_resource_change ON ""resources"";
                DROP TRIGGER IF EXISTS refresh_grid_on_person_change ON ""persons"";
                DROP TRIGGER IF EXISTS refresh_grid_on_organisation_change ON ""organisations"";
                DROP FUNCTION IF EXISTS refresh_resource_grid_view();
                DROP MATERIALIZED VIEW IF EXISTS ResourceGridView;

                DROP TRIGGER IF EXISTS refresh_trash_on_resource_change ON ""resources"";
                DROP TRIGGER IF EXISTS refresh_trash_on_person_change ON ""persons"";
                DROP TRIGGER IF EXISTS refresh_trash_on_organisation_change ON ""organisations"";
                DROP FUNCTION IF EXISTS refresh_resource_trash_view();
                DROP MATERIALIZED VIEW IF EXISTS ResourceTrashView;
            ");

            // Step 5: Drop unique indexes on name from persons and organisations
            migrationBuilder.DropIndex(
                name: "IX_persons_name",
                table: "persons");

            migrationBuilder.DropIndex(
                name: "IX_organisations_name",
                table: "organisations");

            // Step 6: Drop common columns from persons table
            migrationBuilder.DropColumn(
                name: "creation-date",
                table: "persons");

            migrationBuilder.DropColumn(
                name: "description",
                table: "persons");

            migrationBuilder.DropColumn(
                name: "email-address",
                table: "persons");

            migrationBuilder.DropColumn(
                name: "name",
                table: "persons");

            migrationBuilder.DropColumn(
                name: "trash-date",
                table: "persons");

            migrationBuilder.DropColumn(
                name: "trashed",
                table: "persons");

            // Step 7: Drop common columns from organisations table
            migrationBuilder.DropColumn(
                name: "creation-date",
                table: "organisations");

            migrationBuilder.DropColumn(
                name: "description",
                table: "organisations");

            migrationBuilder.DropColumn(
                name: "email-address",
                table: "organisations");

            migrationBuilder.DropColumn(
                name: "name",
                table: "organisations");

            migrationBuilder.DropColumn(
                name: "trash-date",
                table: "organisations");

            migrationBuilder.DropColumn(
                name: "trashed",
                table: "organisations");

            // Step 8: Create unique index on name in entities table
            migrationBuilder.CreateIndex(
                name: "IX_entities_name",
                table: "entities",
                column: "name",
                unique: true);

            // Step 9: Add foreign keys to link persons and organisations to entities
            migrationBuilder.AddForeignKey(
                name: "FK_organisations_entities_id",
                table: "organisations",
                column: "id",
                principalTable: "entities",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_persons_entities_id",
                table: "persons",
                column: "id",
                principalTable: "entities",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            // Step 10: Add foreign key from resource-author to entities
            migrationBuilder.AddForeignKey(
                name: "FK_resource-author_entities_author-id",
                table: "resource-author",
                column: "author-id",
                principalTable: "entities",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_organisations_entities_id",
                table: "organisations");

            migrationBuilder.DropForeignKey(
                name: "FK_persons_entities_id",
                table: "persons");

            migrationBuilder.DropForeignKey(
                name: "FK_resource-author_entities_author-id",
                table: "resource-author");

            migrationBuilder.DropTable(
                name: "entities");

            migrationBuilder.AddColumn<Guid>(
                name: "OrganisationId",
                table: "resource-author",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PersonId",
                table: "resource-author",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "creation-date",
                table: "persons",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "description",
                table: "persons",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "email-address",
                table: "persons",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "name",
                table: "persons",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "trash-date",
                table: "persons",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "trashed",
                table: "persons",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "creation-date",
                table: "organisations",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "description",
                table: "organisations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "email-address",
                table: "organisations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "name",
                table: "organisations",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "trash-date",
                table: "organisations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "trashed",
                table: "organisations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_resource-author_OrganisationId",
                table: "resource-author",
                column: "OrganisationId");

            migrationBuilder.CreateIndex(
                name: "IX_resource-author_PersonId",
                table: "resource-author",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_persons_name",
                table: "persons",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_organisations_name",
                table: "organisations",
                column: "name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_resource-author_organisations_OrganisationId",
                table: "resource-author",
                column: "OrganisationId",
                principalTable: "organisations",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_resource-author_persons_PersonId",
                table: "resource-author",
                column: "PersonId",
                principalTable: "persons",
                principalColumn: "id");
        }
    }
}

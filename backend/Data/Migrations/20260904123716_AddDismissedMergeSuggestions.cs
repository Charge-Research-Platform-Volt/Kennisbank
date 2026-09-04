using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnowledgeBank.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDismissedMergeSuggestions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "dismissed-merge-suggestions",
                columns: table => new
                {
                    entitytype = table.Column<string>(name: "entity-type", type: "text", nullable: false),
                    id1 = table.Column<Guid>(type: "uuid", nullable: false),
                    id2 = table.Column<Guid>(type: "uuid", nullable: false),
                    dismissedon = table.Column<DateTime>(name: "dismissed-on", type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dismissed-merge-suggestions", x => new { x.entitytype, x.id1, x.id2 });
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "dismissed-merge-suggestions");
        }
    }
}

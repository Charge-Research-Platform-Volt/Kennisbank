using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnowledgeBank.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenameCreationDateToCreatedOn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "creation-date",
                table: "messages",
                newName: "created-on");

            migrationBuilder.RenameColumn(
                name: "creation-date",
                table: "chats",
                newName: "created-on");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "created-on",
                table: "messages",
                newName: "creation-date");

            migrationBuilder.RenameColumn(
                name: "created-on",
                table: "chats",
                newName: "creation-date");
        }
    }
}

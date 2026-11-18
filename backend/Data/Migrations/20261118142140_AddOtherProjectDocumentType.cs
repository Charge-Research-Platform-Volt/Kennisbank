using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnowledgeBank.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOtherProjectDocumentType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "project-document-types",
                columns: new[] { "id", "created-by", "created-on", "name" },
                values: new object[] { new Guid("61d50652-f25f-4cf6-9263-529b501d4086"), null, new DateTime(2026, 11, 18, 0, 0, 0, 0, DateTimeKind.Utc), "Other" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "project-document-types",
                keyColumn: "id",
                keyValue: new Guid("61d50652-f25f-4cf6-9263-529b501d4086"));
        }
    }
}

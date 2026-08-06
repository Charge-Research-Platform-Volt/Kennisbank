using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnowledgeBank.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMessageAttachments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "message-attachments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    chatid = table.Column<Guid>(name: "chat-id", type: "uuid", nullable: false),
                    messageid = table.Column<Guid>(name: "message-id", type: "uuid", nullable: true),
                    filename = table.Column<string>(name: "file-name", type: "text", nullable: false),
                    extension = table.Column<string>(type: "text", nullable: false),
                    extractedtext = table.Column<string>(name: "extracted-text", type: "text", nullable: true),
                    ischunked = table.Column<bool>(name: "is-chunked", type: "boolean", nullable: false),
                    createdon = table.Column<DateTime>(name: "created-on", type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_message-attachments", x => x.id);
                    table.ForeignKey(
                        name: "FK_message-attachments_chats_chat-id",
                        column: x => x.chatid,
                        principalTable: "chats",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_message-attachments_messages_message-id",
                        column: x => x.messageid,
                        principalTable: "messages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_message-attachments_chat-id",
                table: "message-attachments",
                column: "chat-id");

            migrationBuilder.CreateIndex(
                name: "IX_message-attachments_message-id",
                table: "message-attachments",
                column: "message-id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "message-attachments");
        }
    }
}

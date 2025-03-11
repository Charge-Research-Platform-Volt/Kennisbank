using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "files",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    filetype = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_files", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "permissions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_permissions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tags",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tags", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_tags",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    is_approved = table.Column<bool>(type: "boolean", nullable: false),
                    user = table.Column<string>(type: "text", nullable: false),
                    created_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    approved_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_tags", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "text", nullable: false),
                    first_name = table.Column<string>(type: "text", nullable: false),
                    last_name = table.Column<string>(type: "text", nullable: false),
                    password = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "role-permission",
                columns: table => new
                {
                    roleid = table.Column<int>(name: "role-id", type: "integer", nullable: false),
                    permissionid = table.Column<int>(name: "permission-id", type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_role-permission", x => new { x.roleid, x.permissionid });
                    table.ForeignKey(
                        name: "FK_role-permission_permissions_permission-id",
                        column: x => x.permissionid,
                        principalTable: "permissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_role-permission_roles_role-id",
                        column: x => x.roleid,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user-role",
                columns: table => new
                {
                    userid = table.Column<int>(name: "user-id", type: "integer", nullable: false),
                    roleid = table.Column<int>(name: "role-id", type: "integer", nullable: false),
                    UserId1 = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user-role", x => new { x.userid, x.roleid });
                    table.ForeignKey(
                        name: "FK_user-role_roles_role-id",
                        column: x => x.roleid,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user-role_users_UserId1",
                        column: x => x.UserId1,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "permissions",
                columns: new[] { "id", "description", "name" },
                values: new object[,]
                {
                    { 1, "Can upload files.", "Permissions.Files.Upload" },
                    { 2, "Can download files.", "Permissions.Files.Download" },
                    { 3, "Can edit files.", "Permissions.Files.Edit" },
                    { 4, "Can delete files.", "Permissions.Files.Delete" },
                    { 5, "Can view files.", "Permissions.Files.View" },
                    { 6, "Can view users.", "Permissions.Users.View" },
                    { 7, "Can add new users.", "Permissions.Users.Create" },
                    { 8, "Can delete users.", "Permissions.Users.Delete" },
                    { 9, "Can edit users.", "Permissions.Users.Edit" }
                });

            migrationBuilder.InsertData(
                table: "roles",
                columns: new[] { "id", "description", "name" },
                values: new object[,]
                {
                    { 1, "Administrator with full priviledges.", "Admin" },
                    { 2, "Regular user.", "User" }
                });

            migrationBuilder.InsertData(
                table: "role-permission",
                columns: new[] { "permission-id", "role-id" },
                values: new object[,]
                {
                    { 1, 1 },
                    { 2, 1 },
                    { 3, 1 },
                    { 4, 1 },
                    { 5, 1 },
                    { 6, 1 },
                    { 7, 1 },
                    { 8, 1 },
                    { 9, 1 },
                    { 1, 2 },
                    { 2, 2 },
                    { 5, 2 },
                    { 6, 2 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_role-permission_permission-id",
                table: "role-permission",
                column: "permission-id");

            migrationBuilder.CreateIndex(
                name: "IX_tags_name",
                table: "tags",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_tags_name",
                table: "user_tags",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user-role_role-id",
                table: "user-role",
                column: "role-id");

            migrationBuilder.CreateIndex(
                name: "IX_user-role_UserId1",
                table: "user-role",
                column: "UserId1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "files");

            migrationBuilder.DropTable(
                name: "role-permission");

            migrationBuilder.DropTable(
                name: "tags");

            migrationBuilder.DropTable(
                name: "user_tags");

            migrationBuilder.DropTable(
                name: "user-role");

            migrationBuilder.DropTable(
                name: "permissions");

            migrationBuilder.DropTable(
                name: "roles");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}

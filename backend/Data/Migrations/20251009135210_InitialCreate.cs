using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace KnowledgeBank.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CustomAvatarVersion = table.Column<int>(type: "integer", nullable: false),
                    HasCustom = table.Column<bool>(type: "boolean", nullable: false),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    SecurityStamp = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "chats",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    userid = table.Column<Guid>(name: "user-id", type: "uuid", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    creationdate = table.Column<DateTime>(name: "creation-date", type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chats", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "invitations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "text", nullable: false),
                    token = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_invitations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "organisations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    website = table.Column<string>(type: "text", nullable: true),
                    emailaddress = table.Column<string>(name: "email-address", type: "text", nullable: true),
                    creationdate = table.Column<DateTime>(name: "creation-date", type: "timestamp with time zone", nullable: false),
                    trashed = table.Column<bool>(type: "boolean", nullable: false),
                    trashdate = table.Column<DateTime>(name: "trash-date", type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_organisations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "persons",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    occupation = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    emailaddress = table.Column<string>(name: "email-address", type: "text", nullable: true),
                    linkedin = table.Column<string>(type: "text", nullable: true),
                    creationdate = table.Column<DateTime>(name: "creation-date", type: "timestamp with time zone", nullable: false),
                    trashed = table.Column<bool>(type: "boolean", nullable: false),
                    trashdate = table.Column<DateTime>(name: "trash-date", type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_persons", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "projects",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    creationdate = table.Column<DateTime>(name: "creation-date", type: "timestamp with time zone", nullable: false),
                    deletiondate = table.Column<DateTime>(name: "deletion-date", type: "timestamp with time zone", nullable: false),
                    projecttype = table.Column<string>(name: "project-type", type: "character varying(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_projects", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "regions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_regions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "resource-types",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_resource-types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tags",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    isstandardized = table.Column<bool>(name: "is-standardized", type: "boolean", nullable: false),
                    isapproved = table.Column<bool>(name: "is-approved", type: "boolean", nullable: false),
                    approvedon = table.Column<DateTime>(name: "approved-on", type: "timestamp with time zone", nullable: true),
                    approvedby = table.Column<Guid>(name: "approved-by", type: "uuid", nullable: true),
                    createdby = table.Column<Guid>(name: "created-by", type: "uuid", nullable: false),
                    createdon = table.Column<DateTime>(name: "created-on", type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tags", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<string>(type: "text", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    ProviderKey = table.Column<string>(type: "text", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    RoleId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    chatid = table.Column<Guid>(name: "chat-id", type: "uuid", nullable: false),
                    messagerole = table.Column<string>(name: "message-role", type: "character varying(50)", maxLength: 50, nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    creationdate = table.Column<DateTime>(name: "creation-date", type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_messages", x => x.id);
                    table.ForeignKey(
                        name: "FK_messages_chats_chat-id",
                        column: x => x.chatid,
                        principalTable: "chats",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "organisation-relationships",
                columns: table => new
                {
                    sourceorganisationid = table.Column<Guid>(name: "source-organisation-id", type: "uuid", nullable: false),
                    targetorganisationid = table.Column<Guid>(name: "target-organisation-id", type: "uuid", nullable: false),
                    relation = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_organisation-relationships", x => new { x.sourceorganisationid, x.targetorganisationid });
                    table.ForeignKey(
                        name: "FK_organisation-relationships_organisations_source-organisatio~",
                        column: x => x.sourceorganisationid,
                        principalTable: "organisations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_organisation-relationships_organisations_target-organisatio~",
                        column: x => x.targetorganisationid,
                        principalTable: "organisations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "person-organisation",
                columns: table => new
                {
                    personid = table.Column<Guid>(name: "person-id", type: "uuid", nullable: false),
                    organisationid = table.Column<Guid>(name: "organisation-id", type: "uuid", nullable: false),
                    role = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_person-organisation", x => new { x.personid, x.organisationid });
                    table.ForeignKey(
                        name: "FK_person-organisation_organisations_organisation-id",
                        column: x => x.organisationid,
                        principalTable: "organisations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_person-organisation_persons_person-id",
                        column: x => x.personid,
                        principalTable: "persons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "person-person",
                columns: table => new
                {
                    sourcepersonid = table.Column<Guid>(name: "source-person-id", type: "uuid", nullable: false),
                    targetpersonid = table.Column<Guid>(name: "target-person-id", type: "uuid", nullable: false),
                    relation = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_person-person", x => new { x.sourcepersonid, x.targetpersonid });
                    table.ForeignKey(
                        name: "FK_person-person_persons_source-person-id",
                        column: x => x.sourcepersonid,
                        principalTable: "persons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_person-person_persons_target-person-id",
                        column: x => x.targetpersonid,
                        principalTable: "persons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "project-creator",
                columns: table => new
                {
                    projectid = table.Column<Guid>(name: "project-id", type: "uuid", nullable: false),
                    creatorid = table.Column<string>(name: "creator-id", type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project-creator", x => new { x.projectid, x.creatorid });
                    table.ForeignKey(
                        name: "FK_project-creator_AspNetUsers_creator-id",
                        column: x => x.creatorid,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_project-creator_projects_project-id",
                        column: x => x.projectid,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "project-folder",
                columns: table => new
                {
                    parentid = table.Column<Guid>(name: "parent-id", type: "uuid", nullable: false),
                    childid = table.Column<Guid>(name: "child-id", type: "uuid", nullable: false),
                    addedby = table.Column<string>(name: "added-by", type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project-folder", x => new { x.parentid, x.childid });
                    table.ForeignKey(
                        name: "FK_project-folder_projects_child-id",
                        column: x => x.childid,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_project-folder_projects_parent-id",
                        column: x => x.parentid,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "resources",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    typeid = table.Column<Guid>(name: "type-id", type: "uuid", nullable: false),
                    languagecode = table.Column<string>(name: "language-code", type: "character varying(2)", maxLength: 2, nullable: false),
                    publicationcode = table.Column<string>(name: "publication-code", type: "text", nullable: true),
                    publicationdate = table.Column<DateTime>(name: "publication-date", type: "timestamp with time zone", nullable: false),
                    creationdate = table.Column<DateTime>(name: "creation-date", type: "timestamp with time zone", nullable: false),
                    license = table.Column<string>(type: "text", nullable: true),
                    note = table.Column<string>(type: "text", nullable: true),
                    aigeneratedtags = table.Column<string>(name: "ai-generated-tags", type: "text", nullable: true),
                    filetype = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    fileext = table.Column<string>(name: "file-ext", type: "character varying(5)", maxLength: 5, nullable: true),
                    hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    trashed = table.Column<bool>(type: "boolean", nullable: false),
                    trashdate = table.Column<DateTime>(name: "trash-date", type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_resources", x => x.id);
                    table.ForeignKey(
                        name: "FK_resources_resource-types_type-id",
                        column: x => x.typeid,
                        principalTable: "resource-types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "project-tag",
                columns: table => new
                {
                    projectid = table.Column<Guid>(name: "project-id", type: "uuid", nullable: false),
                    tagid = table.Column<Guid>(name: "tag-id", type: "uuid", nullable: false),
                    addedby = table.Column<Guid>(name: "added-by", type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project-tag", x => new { x.projectid, x.tagid });
                    table.ForeignKey(
                        name: "FK_project-tag_projects_project-id",
                        column: x => x.projectid,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_project-tag_tags_tag-id",
                        column: x => x.tagid,
                        principalTable: "tags",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "audio-metadata",
                columns: table => new
                {
                    resourceid = table.Column<Guid>(name: "resource-id", type: "uuid", nullable: false),
                    length = table.Column<decimal>(type: "numeric(20,0)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audio-metadata", x => x.resourceid);
                    table.ForeignKey(
                        name: "FK_audio-metadata_resources_resource-id",
                        column: x => x.resourceid,
                        principalTable: "resources",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "document-metadata",
                columns: table => new
                {
                    resourceid = table.Column<Guid>(name: "resource-id", type: "uuid", nullable: false),
                    @abstract = table.Column<string>(name: "abstract", type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_document-metadata", x => x.resourceid);
                    table.ForeignKey(
                        name: "FK_document-metadata_resources_resource-id",
                        column: x => x.resourceid,
                        principalTable: "resources",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "project-resource",
                columns: table => new
                {
                    projectid = table.Column<Guid>(name: "project-id", type: "uuid", nullable: false),
                    resourceid = table.Column<Guid>(name: "resource-id", type: "uuid", nullable: false),
                    addedby = table.Column<string>(name: "added-by", type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project-resource", x => new { x.projectid, x.resourceid });
                    table.ForeignKey(
                        name: "FK_project-resource_projects_project-id",
                        column: x => x.projectid,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_project-resource_resources_resource-id",
                        column: x => x.resourceid,
                        principalTable: "resources",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "resource-author",
                columns: table => new
                {
                    resourceid = table.Column<Guid>(name: "resource-id", type: "uuid", nullable: false),
                    authorid = table.Column<Guid>(name: "author-id", type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_resource-author", x => new { x.resourceid, x.authorid });
                    table.ForeignKey(
                        name: "FK_resource-author_persons_author-id",
                        column: x => x.authorid,
                        principalTable: "persons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_resource-author_resources_resource-id",
                        column: x => x.resourceid,
                        principalTable: "resources",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "resource-organisation",
                columns: table => new
                {
                    resourceid = table.Column<Guid>(name: "resource-id", type: "uuid", nullable: false),
                    organisationid = table.Column<Guid>(name: "organisation-id", type: "uuid", nullable: false),
                    role = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_resource-organisation", x => new { x.resourceid, x.organisationid });
                    table.ForeignKey(
                        name: "FK_resource-organisation_organisations_organisation-id",
                        column: x => x.organisationid,
                        principalTable: "organisations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_resource-organisation_resources_resource-id",
                        column: x => x.resourceid,
                        principalTable: "resources",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "resource-region",
                columns: table => new
                {
                    resourceid = table.Column<Guid>(name: "resource-id", type: "uuid", nullable: false),
                    regionid = table.Column<Guid>(name: "region-id", type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_resource-region", x => new { x.resourceid, x.regionid });
                    table.ForeignKey(
                        name: "FK_resource-region_regions_region-id",
                        column: x => x.regionid,
                        principalTable: "regions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_resource-region_resources_resource-id",
                        column: x => x.resourceid,
                        principalTable: "resources",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "resource-related_organisation",
                columns: table => new
                {
                    resourceid = table.Column<Guid>(name: "resource-id", type: "uuid", nullable: false),
                    organisationid = table.Column<Guid>(name: "organisation-id", type: "uuid", nullable: false),
                    role = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_resource-related_organisation", x => new { x.resourceid, x.organisationid });
                    table.ForeignKey(
                        name: "FK_resource-related_organisation_organisations_organisation-id",
                        column: x => x.organisationid,
                        principalTable: "organisations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_resource-related_organisation_resources_resource-id",
                        column: x => x.resourceid,
                        principalTable: "resources",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "resource-related_person",
                columns: table => new
                {
                    resourceid = table.Column<Guid>(name: "resource-id", type: "uuid", nullable: false),
                    personid = table.Column<Guid>(name: "person-id", type: "uuid", nullable: false),
                    role = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_resource-related_person", x => new { x.resourceid, x.personid });
                    table.ForeignKey(
                        name: "FK_resource-related_person_persons_person-id",
                        column: x => x.personid,
                        principalTable: "persons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_resource-related_person_resources_resource-id",
                        column: x => x.resourceid,
                        principalTable: "resources",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "resource-related_source",
                columns: table => new
                {
                    resourceid = table.Column<Guid>(name: "resource-id", type: "uuid", nullable: false),
                    url = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_resource-related_source", x => new { x.resourceid, x.url });
                    table.ForeignKey(
                        name: "FK_resource-related_source_resources_resource-id",
                        column: x => x.resourceid,
                        principalTable: "resources",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "resource-source",
                columns: table => new
                {
                    resourceid = table.Column<Guid>(name: "resource-id", type: "uuid", nullable: false),
                    url = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_resource-source", x => new { x.resourceid, x.url });
                    table.ForeignKey(
                        name: "FK_resource-source_resources_resource-id",
                        column: x => x.resourceid,
                        principalTable: "resources",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "resource-tag",
                columns: table => new
                {
                    resourceid = table.Column<Guid>(name: "resource-id", type: "uuid", nullable: false),
                    tagid = table.Column<Guid>(name: "tag-id", type: "uuid", nullable: false),
                    addedby = table.Column<Guid>(name: "added-by", type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_resource-tag", x => new { x.resourceid, x.tagid });
                    table.ForeignKey(
                        name: "FK_resource-tag_resources_resource-id",
                        column: x => x.resourceid,
                        principalTable: "resources",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_resource-tag_tags_tag-id",
                        column: x => x.tagid,
                        principalTable: "tags",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "video-metadata",
                columns: table => new
                {
                    resourceid = table.Column<Guid>(name: "resource-id", type: "uuid", nullable: false),
                    length = table.Column<decimal>(type: "numeric(20,0)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_video-metadata", x => x.resourceid);
                    table.ForeignKey(
                        name: "FK_video-metadata_resources_resource-id",
                        column: x => x.resourceid,
                        principalTable: "resources",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "website-metadata",
                columns: table => new
                {
                    resourceid = table.Column<Guid>(name: "resource-id", type: "uuid", nullable: false),
                    url = table.Column<string>(type: "text", nullable: false),
                    accessedon = table.Column<DateTime>(name: "accessed-on", type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_website-metadata", x => x.resourceid);
                    table.ForeignKey(
                        name: "FK_website-metadata_resources_resource-id",
                        column: x => x.resourceid,
                        principalTable: "resources",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_messages_chat-id",
                table: "messages",
                column: "chat-id");

            migrationBuilder.CreateIndex(
                name: "IX_organisation-relationships_target-organisation-id",
                table: "organisation-relationships",
                column: "target-organisation-id");

            migrationBuilder.CreateIndex(
                name: "IX_organisations_name",
                table: "organisations",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_person-organisation_organisation-id",
                table: "person-organisation",
                column: "organisation-id");

            migrationBuilder.CreateIndex(
                name: "IX_person-person_target-person-id",
                table: "person-person",
                column: "target-person-id");

            migrationBuilder.CreateIndex(
                name: "IX_persons_name",
                table: "persons",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_project-creator_creator-id",
                table: "project-creator",
                column: "creator-id");

            migrationBuilder.CreateIndex(
                name: "IX_project-folder_child-id",
                table: "project-folder",
                column: "child-id");

            migrationBuilder.CreateIndex(
                name: "IX_project-resource_resource-id",
                table: "project-resource",
                column: "resource-id");

            migrationBuilder.CreateIndex(
                name: "IX_project-tag_tag-id",
                table: "project-tag",
                column: "tag-id");

            migrationBuilder.CreateIndex(
                name: "IX_regions_name",
                table: "regions",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_resource-author_author-id",
                table: "resource-author",
                column: "author-id");

            migrationBuilder.CreateIndex(
                name: "IX_resource-organisation_organisation-id",
                table: "resource-organisation",
                column: "organisation-id");

            migrationBuilder.CreateIndex(
                name: "idx_resource_region_region_id",
                table: "resource-region",
                column: "region-id");

            migrationBuilder.CreateIndex(
                name: "IX_resource-related_organisation_organisation-id",
                table: "resource-related_organisation",
                column: "organisation-id");

            migrationBuilder.CreateIndex(
                name: "IX_resource-related_person_person-id",
                table: "resource-related_person",
                column: "person-id");

            migrationBuilder.CreateIndex(
                name: "idx_resource_tag_tag_id",
                table: "resource-tag",
                column: "tag-id");

            migrationBuilder.CreateIndex(
                name: "IX_resources_type-id",
                table: "resources",
                column: "type-id");

            migrationBuilder.CreateIndex(
                name: "IX_tags_name",
                table: "tags",
                column: "name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "audio-metadata");

            migrationBuilder.DropTable(
                name: "document-metadata");

            migrationBuilder.DropTable(
                name: "invitations");

            migrationBuilder.DropTable(
                name: "messages");

            migrationBuilder.DropTable(
                name: "organisation-relationships");

            migrationBuilder.DropTable(
                name: "person-organisation");

            migrationBuilder.DropTable(
                name: "person-person");

            migrationBuilder.DropTable(
                name: "project-creator");

            migrationBuilder.DropTable(
                name: "project-folder");

            migrationBuilder.DropTable(
                name: "project-resource");

            migrationBuilder.DropTable(
                name: "project-tag");

            migrationBuilder.DropTable(
                name: "resource-author");

            migrationBuilder.DropTable(
                name: "resource-organisation");

            migrationBuilder.DropTable(
                name: "resource-region");

            migrationBuilder.DropTable(
                name: "resource-related_organisation");

            migrationBuilder.DropTable(
                name: "resource-related_person");

            migrationBuilder.DropTable(
                name: "resource-related_source");

            migrationBuilder.DropTable(
                name: "resource-source");

            migrationBuilder.DropTable(
                name: "resource-tag");

            migrationBuilder.DropTable(
                name: "video-metadata");

            migrationBuilder.DropTable(
                name: "website-metadata");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "chats");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "projects");

            migrationBuilder.DropTable(
                name: "regions");

            migrationBuilder.DropTable(
                name: "organisations");

            migrationBuilder.DropTable(
                name: "persons");

            migrationBuilder.DropTable(
                name: "tags");

            migrationBuilder.DropTable(
                name: "resources");

            migrationBuilder.DropTable(
                name: "resource-types");
        }
    }
}

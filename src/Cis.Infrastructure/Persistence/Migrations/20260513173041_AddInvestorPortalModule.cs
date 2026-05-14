using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cis.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInvestorPortalModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "portal");

            migrationBuilder.CreateTable(
                name: "digital_service_requests",
                schema: "portal",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    investor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    request_payload_json = table.Column<string>(type: "jsonb", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    workflow_id = table.Column<Guid>(type: "uuid", nullable: true),
                    submitted_by_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    submitted_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    last_modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_digital_service_requests", x => x.id);
                    table.ForeignKey(
                        name: "fk_digital_service_requests_investors_investor_id",
                        column: x => x.investor_id,
                        principalSchema: "investors",
                        principalTable: "investors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_digital_service_requests_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_digital_service_requests_workflow_instances_workflow_id",
                        column: x => x.workflow_id,
                        principalSchema: "workflow",
                        principalTable: "workflow_instances",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "investor_notices",
                schema: "portal",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    investor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    body = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    published_date = table.Column<DateOnly>(type: "date", nullable: false),
                    published_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    published_by_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    last_modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_investor_notices", x => x.id);
                    table.ForeignKey(
                        name: "fk_investor_notices_investors_investor_id",
                        column: x => x.investor_id,
                        principalSchema: "investors",
                        principalTable: "investors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "portal_document_downloads",
                schema: "portal",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    investor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    document_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    document_reference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    downloaded_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ip_address = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    last_modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_portal_document_downloads", x => x.id);
                    table.ForeignKey(
                        name: "fk_portal_document_downloads_investors_investor_id",
                        column: x => x.investor_id,
                        principalSchema: "investors",
                        principalTable: "investors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_portal_document_downloads_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "portal_mfa_settings",
                schema: "portal",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    mfa_required = table.Column<bool>(type: "boolean", nullable: false),
                    mfa_verified = table.Column<bool>(type: "boolean", nullable: false),
                    updated_by_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    last_modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_portal_mfa_settings", x => x.id);
                    table.ForeignKey(
                        name: "fk_portal_mfa_settings_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "portal_sessions",
                schema: "portal",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    investor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ip_address = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    user_agent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    mfa_satisfied = table.Column<bool>(type: "boolean", nullable: false),
                    started_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_seen_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    last_modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_portal_sessions", x => x.id);
                    table.ForeignKey(
                        name: "fk_portal_sessions_investors_investor_id",
                        column: x => x.investor_id,
                        principalSchema: "investors",
                        principalTable: "investors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_portal_sessions_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "portal_user_profiles",
                schema: "portal",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    investor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_by_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    portal_user_profile_created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    last_modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_portal_user_profiles", x => x.id);
                    table.ForeignKey(
                        name: "fk_portal_user_profiles_investors_investor_id",
                        column: x => x.investor_id,
                        principalSchema: "investors",
                        principalTable: "investors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_portal_user_profiles_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "portal_activity_logs",
                schema: "portal",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    investor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    portal_session_id = table.Column<Guid>(type: "uuid", nullable: true),
                    activity_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    summary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    entity_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    entity_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ip_address = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    correlation_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    occurred_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    last_modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_portal_activity_logs", x => x.id);
                    table.ForeignKey(
                        name: "fk_portal_activity_logs_investors_investor_id",
                        column: x => x.investor_id,
                        principalSchema: "investors",
                        principalTable: "investors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_portal_activity_logs_portal_sessions_portal_session_id",
                        column: x => x.portal_session_id,
                        principalSchema: "portal",
                        principalTable: "portal_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_portal_activity_logs_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_digital_service_requests_investor_id_request_type_status",
                schema: "portal",
                table: "digital_service_requests",
                columns: new[] { "investor_id", "request_type", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_digital_service_requests_user_id",
                schema: "portal",
                table: "digital_service_requests",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_digital_service_requests_workflow_id",
                schema: "portal",
                table: "digital_service_requests",
                column: "workflow_id");

            migrationBuilder.CreateIndex(
                name: "ix_investor_notices_investor_id_status_published_date",
                schema: "portal",
                table: "investor_notices",
                columns: new[] { "investor_id", "status", "published_date" });

            migrationBuilder.CreateIndex(
                name: "ix_portal_activity_logs_investor_id_occurred_at_utc",
                schema: "portal",
                table: "portal_activity_logs",
                columns: new[] { "investor_id", "occurred_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_portal_activity_logs_portal_session_id",
                schema: "portal",
                table: "portal_activity_logs",
                column: "portal_session_id");

            migrationBuilder.CreateIndex(
                name: "ix_portal_activity_logs_user_id_occurred_at_utc",
                schema: "portal",
                table: "portal_activity_logs",
                columns: new[] { "user_id", "occurred_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_portal_document_downloads_investor_id_document_type_downloa",
                schema: "portal",
                table: "portal_document_downloads",
                columns: new[] { "investor_id", "document_type", "downloaded_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_portal_document_downloads_user_id",
                schema: "portal",
                table: "portal_document_downloads",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_portal_mfa_settings_user_id",
                schema: "portal",
                table: "portal_mfa_settings",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_portal_sessions_investor_id",
                schema: "portal",
                table: "portal_sessions",
                column: "investor_id");

            migrationBuilder.CreateIndex(
                name: "ix_portal_sessions_user_id_status_last_seen_at_utc",
                schema: "portal",
                table: "portal_sessions",
                columns: new[] { "user_id", "status", "last_seen_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_portal_user_profiles_investor_id",
                schema: "portal",
                table: "portal_user_profiles",
                column: "investor_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_portal_user_profiles_user_id",
                schema: "portal",
                table: "portal_user_profiles",
                column: "user_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "digital_service_requests",
                schema: "portal");

            migrationBuilder.DropTable(
                name: "investor_notices",
                schema: "portal");

            migrationBuilder.DropTable(
                name: "portal_activity_logs",
                schema: "portal");

            migrationBuilder.DropTable(
                name: "portal_document_downloads",
                schema: "portal");

            migrationBuilder.DropTable(
                name: "portal_mfa_settings",
                schema: "portal");

            migrationBuilder.DropTable(
                name: "portal_user_profiles",
                schema: "portal");

            migrationBuilder.DropTable(
                name: "portal_sessions",
                schema: "portal");
        }
    }
}

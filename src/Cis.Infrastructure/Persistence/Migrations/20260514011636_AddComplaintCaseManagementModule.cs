using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cis.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddComplaintCaseManagementModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "cases");

            migrationBuilder.CreateTable(
                name: "case_sla_policies",
                schema: "cases",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    priority = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    target_hours = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    last_modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_case_sla_policies", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "service_cases",
                schema: "cases",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    case_number = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    investor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    case_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    subject = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    priority = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    owner_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    logged_by_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    logged_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    business_date = table.Column<DateOnly>(type: "date", nullable: false),
                    sla_target_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    resolved_by_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    resolved_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    resolution_summary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    resolution_evidence_reference = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    last_modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_service_cases", x => x.id);
                    table.ForeignKey(
                        name: "fk_service_cases_investors_investor_id",
                        column: x => x.investor_id,
                        principalSchema: "investors",
                        principalTable: "investors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "case_actions",
                schema: "cases",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_case_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    summary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    evidence_reference = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    actioned_by_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    actioned_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_case_actions", x => x.id);
                    table.ForeignKey(
                        name: "fk_case_actions_service_cases_service_case_id",
                        column: x => x.service_case_id,
                        principalSchema: "cases",
                        principalTable: "service_cases",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "case_escalations",
                schema: "cases",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_case_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    escalated_to_role = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    escalated_to_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    escalated_by_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    escalated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    resolved_by_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    resolved_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_case_escalations", x => x.id);
                    table.ForeignKey(
                        name: "fk_case_escalations_service_cases_service_case_id",
                        column: x => x.service_case_id,
                        principalSchema: "cases",
                        principalTable: "service_cases",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "case_status_history",
                schema: "cases",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_case_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    to_status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    changed_by_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    changed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_case_status_history", x => x.id);
                    table.ForeignKey(
                        name: "fk_case_status_history_service_cases_service_case_id",
                        column: x => x.service_case_id,
                        principalSchema: "cases",
                        principalTable: "service_cases",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "complaints",
                schema: "cases",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_case_id = table.Column<Guid>(type: "uuid", nullable: false),
                    complaint_reference = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    source = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    received_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_regulatory = table.Column<bool>(type: "boolean", nullable: false),
                    regulatory_category = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    turnaround_days = table.Column<int>(type: "integer", nullable: true),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    last_modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_complaints", x => x.id);
                    table.ForeignKey(
                        name: "fk_complaints_service_cases_service_case_id",
                        column: x => x.service_case_id,
                        principalSchema: "cases",
                        principalTable: "service_cases",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_case_actions_service_case_id_actioned_at_utc",
                schema: "cases",
                table: "case_actions",
                columns: new[] { "service_case_id", "actioned_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_case_escalations_escalated_at_utc",
                schema: "cases",
                table: "case_escalations",
                column: "escalated_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_case_escalations_service_case_id_status",
                schema: "cases",
                table: "case_escalations",
                columns: new[] { "service_case_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_case_sla_policies_category_priority_is_active",
                schema: "cases",
                table: "case_sla_policies",
                columns: new[] { "category", "priority", "is_active" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_case_status_history_service_case_id_changed_at_utc",
                schema: "cases",
                table: "case_status_history",
                columns: new[] { "service_case_id", "changed_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_complaints_complaint_reference",
                schema: "cases",
                table: "complaints",
                column: "complaint_reference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_complaints_received_at_utc",
                schema: "cases",
                table: "complaints",
                column: "received_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_complaints_service_case_id",
                schema: "cases",
                table: "complaints",
                column: "service_case_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_service_cases_case_number",
                schema: "cases",
                table: "service_cases",
                column: "case_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_service_cases_investor_id",
                schema: "cases",
                table: "service_cases",
                column: "investor_id");

            migrationBuilder.CreateIndex(
                name: "ix_service_cases_owner_user_id",
                schema: "cases",
                table: "service_cases",
                column: "owner_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_service_cases_sla_target_at_utc",
                schema: "cases",
                table: "service_cases",
                column: "sla_target_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_service_cases_status",
                schema: "cases",
                table: "service_cases",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "case_actions",
                schema: "cases");

            migrationBuilder.DropTable(
                name: "case_escalations",
                schema: "cases");

            migrationBuilder.DropTable(
                name: "case_sla_policies",
                schema: "cases");

            migrationBuilder.DropTable(
                name: "case_status_history",
                schema: "cases");

            migrationBuilder.DropTable(
                name: "complaints",
                schema: "cases");

            migrationBuilder.DropTable(
                name: "service_cases",
                schema: "cases");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cis.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDataQualityModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "data_quality");

            migrationBuilder.CreateTable(
                name: "data_quality_check_runs",
                schema: "data_quality",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    run_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    requested_by_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    started_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    completed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    rules_evaluated = table.Column<int>(type: "integer", nullable: false),
                    exceptions_generated = table.Column<int>(type: "integer", nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    last_modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_data_quality_check_runs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "data_quality_dashboard_snapshots",
                schema: "data_quality",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    open_exceptions = table.Column<int>(type: "integer", nullable: false),
                    assigned_exceptions = table.Column<int>(type: "integer", nullable: false),
                    overdue_exceptions = table.Column<int>(type: "integer", nullable: false),
                    resolved_exceptions = table.Column<int>(type: "integer", nullable: false),
                    generated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    last_modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_data_quality_dashboard_snapshots", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "data_quality_rules",
                schema: "data_quality",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    severity = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    threshold_days = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    last_modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_data_quality_rules", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "data_quality_exceptions",
                schema: "data_quality",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    check_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rule_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rule_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    severity = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    entity_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    entity_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    detected_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    due_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    owner_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    assigned_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    resolved_by_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    resolved_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    resolution_evidence_reference = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    resolution_comment = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    last_modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_data_quality_exceptions", x => x.id);
                    table.ForeignKey(
                        name: "fk_data_quality_exceptions_data_quality_check_runs_check_run_id",
                        column: x => x.check_run_id,
                        principalSchema: "data_quality",
                        principalTable: "data_quality_check_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_data_quality_exceptions_data_quality_rules_rule_id",
                        column: x => x.rule_id,
                        principalSchema: "data_quality",
                        principalTable: "data_quality_rules",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "exception_assignments",
                schema: "data_quality",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    data_quality_exception_id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    assigned_by_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    assigned_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_exception_assignments", x => x.id);
                    table.ForeignKey(
                        name: "fk_exception_assignments_data_quality_exceptions_data_quality_",
                        column: x => x.data_quality_exception_id,
                        principalSchema: "data_quality",
                        principalTable: "data_quality_exceptions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "exception_queue",
                schema: "data_quality",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    data_quality_exception_id = table.Column<Guid>(type: "uuid", nullable: false),
                    severity = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    queued_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    closed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    last_modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_exception_queue", x => x.id);
                    table.ForeignKey(
                        name: "fk_exception_queue_data_quality_exceptions_data_quality_except",
                        column: x => x.data_quality_exception_id,
                        principalSchema: "data_quality",
                        principalTable: "data_quality_exceptions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_data_quality_check_runs_run_number",
                schema: "data_quality",
                table: "data_quality_check_runs",
                column: "run_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_data_quality_check_runs_status_started_at_utc",
                schema: "data_quality",
                table: "data_quality_check_runs",
                columns: new[] { "status", "started_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_data_quality_dashboard_snapshots_generated_at_utc",
                schema: "data_quality",
                table: "data_quality_dashboard_snapshots",
                column: "generated_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_data_quality_exceptions_check_run_id",
                schema: "data_quality",
                table: "data_quality_exceptions",
                column: "check_run_id");

            migrationBuilder.CreateIndex(
                name: "ix_data_quality_exceptions_due_at_utc",
                schema: "data_quality",
                table: "data_quality_exceptions",
                column: "due_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_data_quality_exceptions_entity_type_entity_id",
                schema: "data_quality",
                table: "data_quality_exceptions",
                columns: new[] { "entity_type", "entity_id" });

            migrationBuilder.CreateIndex(
                name: "ix_data_quality_exceptions_rule_code_status",
                schema: "data_quality",
                table: "data_quality_exceptions",
                columns: new[] { "rule_code", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_data_quality_exceptions_rule_id",
                schema: "data_quality",
                table: "data_quality_exceptions",
                column: "rule_id");

            migrationBuilder.CreateIndex(
                name: "ix_data_quality_rules_code",
                schema: "data_quality",
                table: "data_quality_rules",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_exception_assignments_data_quality_exception_id_assigned_at",
                schema: "data_quality",
                table: "exception_assignments",
                columns: new[] { "data_quality_exception_id", "assigned_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_exception_queue_data_quality_exception_id",
                schema: "data_quality",
                table: "exception_queue",
                column: "data_quality_exception_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_exception_queue_status_severity_queued_at_utc",
                schema: "data_quality",
                table: "exception_queue",
                columns: new[] { "status", "severity", "queued_at_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "data_quality_dashboard_snapshots",
                schema: "data_quality");

            migrationBuilder.DropTable(
                name: "exception_assignments",
                schema: "data_quality");

            migrationBuilder.DropTable(
                name: "exception_queue",
                schema: "data_quality");

            migrationBuilder.DropTable(
                name: "data_quality_exceptions",
                schema: "data_quality");

            migrationBuilder.DropTable(
                name: "data_quality_check_runs",
                schema: "data_quality");

            migrationBuilder.DropTable(
                name: "data_quality_rules",
                schema: "data_quality");
        }
    }
}

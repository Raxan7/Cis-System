using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cis.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReportsModuleFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "reports");

            migrationBuilder.CreateTable(
                name: "report_bundles",
                schema: "reports",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    bundle_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    business_date = table.Column<DateOnly>(type: "date", nullable: false),
                    report_run_ids_json = table.Column<string>(type: "jsonb", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_by_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    report_bundle_created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    published_by_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    published_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    last_modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_report_bundles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "report_definitions",
                schema: "reports",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    frequency = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    primary_users = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    required_permission = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_by_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    report_definition_created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    last_modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_report_definitions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "report_owner_matrix",
                schema: "reports",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    report_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_role = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    responsibility = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    created_by_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    report_owner_matrix_created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    last_modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_report_owner_matrix", x => x.id);
                    table.ForeignKey(
                        name: "fk_report_owner_matrix_report_definitions_report_definition_id",
                        column: x => x.report_definition_id,
                        principalSchema: "reports",
                        principalTable: "report_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "report_runs",
                schema: "reports",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    report_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    report_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    business_date = table.Column<DateOnly>(type: "date", nullable: false),
                    source_data_timestamp_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    version_number = table.Column<int>(type: "integer", nullable: false),
                    generated_by_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    generated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    approved_by_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    approved_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    published_by_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    published_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    last_modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_report_runs", x => x.id);
                    table.ForeignKey(
                        name: "fk_report_runs_report_definitions_report_definition_id",
                        column: x => x.report_definition_id,
                        principalSchema: "reports",
                        principalTable: "report_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "report_schedules",
                schema: "reports",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    report_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    frequency = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    cron_expression = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    next_run_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_by_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    report_schedule_created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    last_modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_report_schedules", x => x.id);
                    table.ForeignKey(
                        name: "fk_report_schedules_report_definitions_report_definition_id",
                        column: x => x.report_definition_id,
                        principalSchema: "reports",
                        principalTable: "report_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "report_approvals",
                schema: "reports",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    report_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    decision = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    decided_by_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    decided_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    comment = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_report_approvals", x => x.id);
                    table.ForeignKey(
                        name: "fk_report_approvals_report_runs_report_run_id",
                        column: x => x.report_run_id,
                        principalSchema: "reports",
                        principalTable: "report_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "report_distributions",
                schema: "reports",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    report_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    channel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    recipient = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    distributed_by_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    distributed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    comment = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_report_distributions", x => x.id);
                    table.ForeignKey(
                        name: "fk_report_distributions_report_runs_report_run_id",
                        column: x => x.report_run_id,
                        principalSchema: "reports",
                        principalTable: "report_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "report_outputs",
                schema: "reports",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    report_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    format = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    storage_reference = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    content_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_report_outputs", x => x.id);
                    table.ForeignKey(
                        name: "fk_report_outputs_report_runs_report_run_id",
                        column: x => x.report_run_id,
                        principalSchema: "reports",
                        principalTable: "report_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "report_parameters",
                schema: "reports",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    report_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    value = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_report_parameters", x => x.id);
                    table.ForeignKey(
                        name: "fk_report_parameters_report_runs_report_run_id",
                        column: x => x.report_run_id,
                        principalSchema: "reports",
                        principalTable: "report_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "report_version_archives",
                schema: "reports",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    report_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version_number = table.Column<int>(type: "integer", nullable: false),
                    archive_payload_json = table.Column<string>(type: "jsonb", nullable: false),
                    payload_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    archived_by_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    archived_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    last_modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_report_version_archives", x => x.id);
                    table.ForeignKey(
                        name: "fk_report_version_archives_report_runs_report_run_id",
                        column: x => x.report_run_id,
                        principalSchema: "reports",
                        principalTable: "report_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_report_approvals_report_run_id_decision",
                schema: "reports",
                table: "report_approvals",
                columns: new[] { "report_run_id", "decision" });

            migrationBuilder.CreateIndex(
                name: "ix_report_bundles_bundle_code",
                schema: "reports",
                table: "report_bundles",
                column: "bundle_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_report_bundles_status_business_date",
                schema: "reports",
                table: "report_bundles",
                columns: new[] { "status", "business_date" });

            migrationBuilder.CreateIndex(
                name: "ix_report_definitions_category_frequency",
                schema: "reports",
                table: "report_definitions",
                columns: new[] { "category", "frequency" });

            migrationBuilder.CreateIndex(
                name: "ix_report_definitions_code",
                schema: "reports",
                table: "report_definitions",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_report_distributions_report_run_id_channel_distributed_at_u",
                schema: "reports",
                table: "report_distributions",
                columns: new[] { "report_run_id", "channel", "distributed_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_report_outputs_report_run_id_format",
                schema: "reports",
                table: "report_outputs",
                columns: new[] { "report_run_id", "format" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_report_owner_matrix_report_definition_id_owner_role",
                schema: "reports",
                table: "report_owner_matrix",
                columns: new[] { "report_definition_id", "owner_role" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_report_parameters_report_run_id_name",
                schema: "reports",
                table: "report_parameters",
                columns: new[] { "report_run_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_report_runs_report_code_business_date_version_number",
                schema: "reports",
                table: "report_runs",
                columns: new[] { "report_code", "business_date", "version_number" });

            migrationBuilder.CreateIndex(
                name: "ix_report_runs_report_definition_id",
                schema: "reports",
                table: "report_runs",
                column: "report_definition_id");

            migrationBuilder.CreateIndex(
                name: "ix_report_runs_status_generated_at_utc",
                schema: "reports",
                table: "report_runs",
                columns: new[] { "status", "generated_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_report_schedules_next_run_at_utc",
                schema: "reports",
                table: "report_schedules",
                column: "next_run_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_report_schedules_report_definition_id_status",
                schema: "reports",
                table: "report_schedules",
                columns: new[] { "report_definition_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_report_version_archives_report_run_id_version_number",
                schema: "reports",
                table: "report_version_archives",
                columns: new[] { "report_run_id", "version_number" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "report_approvals",
                schema: "reports");

            migrationBuilder.DropTable(
                name: "report_bundles",
                schema: "reports");

            migrationBuilder.DropTable(
                name: "report_distributions",
                schema: "reports");

            migrationBuilder.DropTable(
                name: "report_outputs",
                schema: "reports");

            migrationBuilder.DropTable(
                name: "report_owner_matrix",
                schema: "reports");

            migrationBuilder.DropTable(
                name: "report_parameters",
                schema: "reports");

            migrationBuilder.DropTable(
                name: "report_schedules",
                schema: "reports");

            migrationBuilder.DropTable(
                name: "report_version_archives",
                schema: "reports");

            migrationBuilder.DropTable(
                name: "report_runs",
                schema: "reports");

            migrationBuilder.DropTable(
                name: "report_definitions",
                schema: "reports");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cis.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOperationalReadinessModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "operations");

            migrationBuilder.CreateTable(
                name: "backup_run_records",
                schema: "operations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    environment = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    backup_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    database_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    storage_reference = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    started_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    completed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    size_bytes = table.Column<long>(type: "bigint", nullable: true),
                    sha256 = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    error_message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    initiated_by_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    last_modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_backup_run_records", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "dr_test_records",
                schema: "operations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    test_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    environment = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    scenario = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    planned_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    started_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    rto_achieved_minutes = table.Column<int>(type: "integer", nullable: true),
                    rpo_achieved_minutes = table.Column<int>(type: "integer", nullable: true),
                    requested_by_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    evidence_reference = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    findings = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    last_modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_dr_test_records", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "rto_rpo_configurations",
                schema: "operations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    environment = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    system_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    rto_minutes = table.Column<int>(type: "integer", nullable: false),
                    rpo_minutes = table.Column<int>(type: "integer", nullable: false),
                    backup_frequency_minutes = table.Column<int>(type: "integer", nullable: false),
                    effective_from_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    effective_to_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    last_modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rto_rpo_configurations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "restore_test_records",
                schema: "operations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    environment = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    backup_run_record_id = table.Column<Guid>(type: "uuid", nullable: true),
                    target_database_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    started_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    completed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    evidence_reference = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    validation_summary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    initiated_by_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    last_modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_restore_test_records", x => x.id);
                    table.ForeignKey(
                        name: "fk_restore_test_records_backup_run_records_backup_run_record_id",
                        column: x => x.backup_run_record_id,
                        principalSchema: "operations",
                        principalTable: "backup_run_records",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_backup_run_records_database_name_started_at_utc",
                schema: "operations",
                table: "backup_run_records",
                columns: new[] { "database_name", "started_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_backup_run_records_environment_status_started_at_utc",
                schema: "operations",
                table: "backup_run_records",
                columns: new[] { "environment", "status", "started_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_dr_test_records_environment_status_planned_at_utc",
                schema: "operations",
                table: "dr_test_records",
                columns: new[] { "environment", "status", "planned_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_restore_test_records_backup_run_record_id",
                schema: "operations",
                table: "restore_test_records",
                column: "backup_run_record_id");

            migrationBuilder.CreateIndex(
                name: "ix_restore_test_records_environment_status_started_at_utc",
                schema: "operations",
                table: "restore_test_records",
                columns: new[] { "environment", "status", "started_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_rto_rpo_configurations_environment_system_name_effective_fr",
                schema: "operations",
                table: "rto_rpo_configurations",
                columns: new[] { "environment", "system_name", "effective_from_utc" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_rto_rpo_configurations_environment_system_name_is_active",
                schema: "operations",
                table: "rto_rpo_configurations",
                columns: new[] { "environment", "system_name", "is_active" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "dr_test_records",
                schema: "operations");

            migrationBuilder.DropTable(
                name: "restore_test_records",
                schema: "operations");

            migrationBuilder.DropTable(
                name: "rto_rpo_configurations",
                schema: "operations");

            migrationBuilder.DropTable(
                name: "backup_run_records",
                schema: "operations");
        }
    }
}

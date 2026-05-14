using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cis.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PlatformAuditWorkflowArchive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "workflow");

            migrationBuilder.EnsureSchema(
                name: "archive");

            migrationBuilder.AddColumn<string>(
                name: "actor_role",
                schema: "audit",
                table: "audit_logs",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "after_json",
                schema: "audit",
                table: "audit_logs",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "before_json",
                schema: "audit",
                table: "audit_logs",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "event_type",
                schema: "audit",
                table: "audit_logs",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Updated");

            migrationBuilder.AddColumn<string>(
                name: "reason",
                schema: "audit",
                table: "audit_logs",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "workflow_id",
                schema: "audit",
                table: "audit_logs",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "approval_policies",
                schema: "workflow",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    workflow_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    module = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    requires_checker = table.Column<bool>(type: "boolean", nullable: false),
                    requires_approver = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    last_modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_approval_policies", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "retention_policies",
                schema: "archive",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    module = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    retention_days = table.Column<int>(type: "integer", nullable: false),
                    legal_hold_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    last_modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_retention_policies", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "workflow_instances",
                schema: "workflow",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    workflow_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    approval_policy_id = table.Column<Guid>(type: "uuid", nullable: false),
                    entity_type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    entity_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    initiated_by_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    submitted_by_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    submitted_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    checked_by_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    checked_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    approved_by_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    approved_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    rejected_by_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    rejected_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    decision_comment = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    last_modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_workflow_instances", x => x.id);
                    table.ForeignKey(
                        name: "fk_workflow_instances_approval_policies_approval_policy_id",
                        column: x => x.approval_policy_id,
                        principalSchema: "workflow",
                        principalTable: "approval_policies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "immutable_archive_records",
                schema: "archive",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    module = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    entity_type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    entity_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    payload_json = table.Column<string>(type: "jsonb", nullable: false),
                    payload_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    archived_by_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    archived_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    workflow_id = table.Column<Guid>(type: "uuid", nullable: true),
                    retention_policy_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_immutable_archive_records", x => x.id);
                    table.ForeignKey(
                        name: "fk_immutable_archive_records_retention_policies_retention_poli",
                        column: x => x.retention_policy_id,
                        principalSchema: "archive",
                        principalTable: "retention_policies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "workflow_actions",
                schema: "workflow",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    workflow_instance_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    actor_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    occurred_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    comment = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_workflow_actions", x => x.id);
                    table.ForeignKey(
                        name: "fk_workflow_actions_workflow_instances_workflow_instance_id",
                        column: x => x.workflow_instance_id,
                        principalSchema: "workflow",
                        principalTable: "workflow_instances",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "workflow_steps",
                schema: "workflow",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    workflow_instance_id = table.Column<Guid>(type: "uuid", nullable: false),
                    step_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    step_order = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    completed_by_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    completed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_workflow_steps", x => x.id);
                    table.ForeignKey(
                        name: "fk_workflow_steps_workflow_instances_workflow_instance_id",
                        column: x => x.workflow_instance_id,
                        principalSchema: "workflow",
                        principalTable: "workflow_instances",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_event_type",
                schema: "audit",
                table: "audit_logs",
                column: "event_type");

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_workflow_id",
                schema: "audit",
                table: "audit_logs",
                column: "workflow_id");

            migrationBuilder.CreateIndex(
                name: "ix_approval_policies_module",
                schema: "workflow",
                table: "approval_policies",
                column: "module");

            migrationBuilder.CreateIndex(
                name: "ix_approval_policies_workflow_type",
                schema: "workflow",
                table: "approval_policies",
                column: "workflow_type",
                unique: true,
                filter: "is_active = true");

            migrationBuilder.CreateIndex(
                name: "ix_immutable_archive_records_entity_type_entity_id",
                schema: "archive",
                table: "immutable_archive_records",
                columns: new[] { "entity_type", "entity_id" });

            migrationBuilder.CreateIndex(
                name: "ix_immutable_archive_records_module_archived_at_utc",
                schema: "archive",
                table: "immutable_archive_records",
                columns: new[] { "module", "archived_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_immutable_archive_records_payload_hash",
                schema: "archive",
                table: "immutable_archive_records",
                column: "payload_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_immutable_archive_records_retention_policy_id",
                schema: "archive",
                table: "immutable_archive_records",
                column: "retention_policy_id");

            migrationBuilder.CreateIndex(
                name: "ix_immutable_archive_records_workflow_id",
                schema: "archive",
                table: "immutable_archive_records",
                column: "workflow_id");

            migrationBuilder.CreateIndex(
                name: "ix_retention_policies_is_active",
                schema: "archive",
                table: "retention_policies",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_retention_policies_module_name",
                schema: "archive",
                table: "retention_policies",
                columns: new[] { "module", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_workflow_actions_action_type_actor_user_id",
                schema: "workflow",
                table: "workflow_actions",
                columns: new[] { "action_type", "actor_user_id" });

            migrationBuilder.CreateIndex(
                name: "ix_workflow_actions_workflow_instance_id_occurred_at_utc",
                schema: "workflow",
                table: "workflow_actions",
                columns: new[] { "workflow_instance_id", "occurred_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_workflow_instances_approval_policy_id",
                schema: "workflow",
                table: "workflow_instances",
                column: "approval_policy_id");

            migrationBuilder.CreateIndex(
                name: "ix_workflow_instances_entity_type_entity_id",
                schema: "workflow",
                table: "workflow_instances",
                columns: new[] { "entity_type", "entity_id" });

            migrationBuilder.CreateIndex(
                name: "ix_workflow_instances_workflow_type_status",
                schema: "workflow",
                table: "workflow_instances",
                columns: new[] { "workflow_type", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_workflow_steps_step_type_status",
                schema: "workflow",
                table: "workflow_steps",
                columns: new[] { "step_type", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_workflow_steps_workflow_instance_id_step_type",
                schema: "workflow",
                table: "workflow_steps",
                columns: new[] { "workflow_instance_id", "step_type" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "immutable_archive_records",
                schema: "archive");

            migrationBuilder.DropTable(
                name: "workflow_actions",
                schema: "workflow");

            migrationBuilder.DropTable(
                name: "workflow_steps",
                schema: "workflow");

            migrationBuilder.DropTable(
                name: "retention_policies",
                schema: "archive");

            migrationBuilder.DropTable(
                name: "workflow_instances",
                schema: "workflow");

            migrationBuilder.DropTable(
                name: "approval_policies",
                schema: "workflow");

            migrationBuilder.DropIndex(
                name: "ix_audit_logs_event_type",
                schema: "audit",
                table: "audit_logs");

            migrationBuilder.DropIndex(
                name: "ix_audit_logs_workflow_id",
                schema: "audit",
                table: "audit_logs");

            migrationBuilder.DropColumn(
                name: "actor_role",
                schema: "audit",
                table: "audit_logs");

            migrationBuilder.DropColumn(
                name: "after_json",
                schema: "audit",
                table: "audit_logs");

            migrationBuilder.DropColumn(
                name: "before_json",
                schema: "audit",
                table: "audit_logs");

            migrationBuilder.DropColumn(
                name: "event_type",
                schema: "audit",
                table: "audit_logs");

            migrationBuilder.DropColumn(
                name: "reason",
                schema: "audit",
                table: "audit_logs");

            migrationBuilder.DropColumn(
                name: "workflow_id",
                schema: "audit",
                table: "audit_logs");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cis.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIntegrationsModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "integrations");

            migrationBuilder.CreateTable(
                name: "integration_endpoints",
                schema: "integrations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    integration_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    base_address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    last_modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_integration_endpoints", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "integration_messages",
                schema: "integrations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    integration_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    direction = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    endpoint_code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    external_reference = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    payload_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    payload_storage_reference = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    received_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    processed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    last_modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_integration_messages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "integration_credential_references",
                schema: "integrations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    integration_endpoint_id = table.Column<Guid>(type: "uuid", nullable: false),
                    credential_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    secret_reference = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    last_modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_integration_credential_references", x => x.id);
                    table.ForeignKey(
                        name: "fk_integration_credential_references_integration_endpoints_int",
                        column: x => x.integration_endpoint_id,
                        principalSchema: "integrations",
                        principalTable: "integration_endpoints",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "integration_delivery_attempts",
                schema: "integrations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    integration_message_id = table.Column<Guid>(type: "uuid", nullable: false),
                    attempt_number = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    retryable = table.Column<bool>(type: "boolean", nullable: false),
                    response_code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    response_message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    attempted_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    last_modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_integration_delivery_attempts", x => x.id);
                    table.ForeignKey(
                        name: "fk_integration_delivery_attempts_integration_messages_integrat",
                        column: x => x.integration_message_id,
                        principalSchema: "integrations",
                        principalTable: "integration_messages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "integration_errors",
                schema: "integrations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    integration_message_id = table.Column<Guid>(type: "uuid", nullable: true),
                    integration_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    error_code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    error_message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    retryable = table.Column<bool>(type: "boolean", nullable: false),
                    source_reference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    occurred_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    last_modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_integration_errors", x => x.id);
                    table.ForeignKey(
                        name: "fk_integration_errors_integration_messages_integration_message",
                        column: x => x.integration_message_id,
                        principalSchema: "integrations",
                        principalTable: "integration_messages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "integration_idempotency_keys",
                schema: "integrations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    endpoint_code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    request_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    integration_message_id = table.Column<Guid>(type: "uuid", nullable: false),
                    response_json = table.Column<string>(type: "jsonb", nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    last_modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_integration_idempotency_keys", x => x.id);
                    table.ForeignKey(
                        name: "fk_integration_idempotency_keys_integration_messages_integrati",
                        column: x => x.integration_message_id,
                        principalSchema: "integrations",
                        principalTable: "integration_messages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "integration_ingestion_runs",
                schema: "integrations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    integration_message_id = table.Column<Guid>(type: "uuid", nullable: false),
                    integration_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    adapter_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    file_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    source_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    record_count = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    started_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    completed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    last_modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_integration_ingestion_runs", x => x.id);
                    table.ForeignKey(
                        name: "fk_integration_ingestion_runs_integration_messages_integration",
                        column: x => x.integration_message_id,
                        principalSchema: "integrations",
                        principalTable: "integration_messages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_integration_credential_references_integration_endpoint_id_c",
                schema: "integrations",
                table: "integration_credential_references",
                columns: new[] { "integration_endpoint_id", "credential_name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_integration_delivery_attempts_integration_message_id_attemp",
                schema: "integrations",
                table: "integration_delivery_attempts",
                columns: new[] { "integration_message_id", "attempt_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_integration_endpoints_code",
                schema: "integrations",
                table: "integration_endpoints",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_integration_errors_integration_message_id",
                schema: "integrations",
                table: "integration_errors",
                column: "integration_message_id");

            migrationBuilder.CreateIndex(
                name: "ix_integration_errors_integration_type_occurred_at_utc",
                schema: "integrations",
                table: "integration_errors",
                columns: new[] { "integration_type", "occurred_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_integration_errors_retryable",
                schema: "integrations",
                table: "integration_errors",
                column: "retryable");

            migrationBuilder.CreateIndex(
                name: "ix_integration_idempotency_keys_endpoint_code_key",
                schema: "integrations",
                table: "integration_idempotency_keys",
                columns: new[] { "endpoint_code", "key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_integration_idempotency_keys_integration_message_id",
                schema: "integrations",
                table: "integration_idempotency_keys",
                column: "integration_message_id");

            migrationBuilder.CreateIndex(
                name: "ix_integration_ingestion_runs_integration_message_id",
                schema: "integrations",
                table: "integration_ingestion_runs",
                column: "integration_message_id");

            migrationBuilder.CreateIndex(
                name: "ix_integration_ingestion_runs_integration_type_completed_at_utc",
                schema: "integrations",
                table: "integration_ingestion_runs",
                columns: new[] { "integration_type", "completed_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_integration_messages_endpoint_code_external_reference",
                schema: "integrations",
                table: "integration_messages",
                columns: new[] { "endpoint_code", "external_reference" });

            migrationBuilder.CreateIndex(
                name: "ix_integration_messages_integration_type_status",
                schema: "integrations",
                table: "integration_messages",
                columns: new[] { "integration_type", "status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "integration_credential_references",
                schema: "integrations");

            migrationBuilder.DropTable(
                name: "integration_delivery_attempts",
                schema: "integrations");

            migrationBuilder.DropTable(
                name: "integration_errors",
                schema: "integrations");

            migrationBuilder.DropTable(
                name: "integration_idempotency_keys",
                schema: "integrations");

            migrationBuilder.DropTable(
                name: "integration_ingestion_runs",
                schema: "integrations");

            migrationBuilder.DropTable(
                name: "integration_endpoints",
                schema: "integrations");

            migrationBuilder.DropTable(
                name: "integration_messages",
                schema: "integrations");
        }
    }
}

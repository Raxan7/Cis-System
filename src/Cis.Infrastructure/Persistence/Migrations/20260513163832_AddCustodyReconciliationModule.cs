using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cis.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCustodyReconciliationModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "custody_reconciliation");

            migrationBuilder.CreateTable(
                name: "custodians",
                schema: "custody_reconciliation",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    swift_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_by_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    custodian_created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    last_modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_custodians", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "custodian_accounts",
                schema: "custody_reconciliation",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    custodian_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheme_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheme_class_id = table.Column<Guid>(type: "uuid", nullable: true),
                    account_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    account_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_custodian_accounts", x => x.id);
                    table.ForeignKey(
                        name: "fk_custodian_accounts_custodians_custodian_id",
                        column: x => x.custodian_id,
                        principalSchema: "custody_reconciliation",
                        principalTable: "custodians",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_custodian_accounts_scheme_classes_scheme_class_id",
                        column: x => x.scheme_class_id,
                        principalSchema: "schemes",
                        principalTable: "scheme_classes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_custodian_accounts_schemes_scheme_id",
                        column: x => x.scheme_id,
                        principalSchema: "schemes",
                        principalTable: "schemes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "custodian_statement_imports",
                schema: "custody_reconciliation",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    custodian_id = table.Column<Guid>(type: "uuid", nullable: false),
                    custodian_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    statement_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    statement_date = table.Column<DateOnly>(type: "date", nullable: false),
                    source_file_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    source_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    imported_by_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    imported_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    line_count = table.Column<int>(type: "integer", nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    last_modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_custodian_statement_imports", x => x.id);
                    table.ForeignKey(
                        name: "fk_custodian_statement_imports_custodian_accounts_custodian_ac",
                        column: x => x.custodian_account_id,
                        principalSchema: "custody_reconciliation",
                        principalTable: "custodian_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_custodian_statement_imports_custodians_custodian_id",
                        column: x => x.custodian_id,
                        principalSchema: "custody_reconciliation",
                        principalTable: "custodians",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "custodian_cash_lines",
                schema: "custody_reconciliation",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    custodian_statement_import_id = table.Column<Guid>(type: "uuid", nullable: false),
                    custodian_id = table.Column<Guid>(type: "uuid", nullable: false),
                    custodian_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    scheme_id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    balance_date = table.Column<DateOnly>(type: "date", nullable: false),
                    cash_balance = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    settlement_reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    is_settled = table.Column<bool>(type: "boolean", nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_custodian_cash_lines", x => x.id);
                    table.ForeignKey(
                        name: "fk_custodian_cash_lines_custodian_accounts_custodian_account_id",
                        column: x => x.custodian_account_id,
                        principalSchema: "custody_reconciliation",
                        principalTable: "custodian_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_custodian_cash_lines_custodian_statement_imports_custodian_",
                        column: x => x.custodian_statement_import_id,
                        principalSchema: "custody_reconciliation",
                        principalTable: "custodian_statement_imports",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_custodian_cash_lines_custodians_custodian_id",
                        column: x => x.custodian_id,
                        principalSchema: "custody_reconciliation",
                        principalTable: "custodians",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_custodian_cash_lines_schemes_scheme_id",
                        column: x => x.scheme_id,
                        principalSchema: "schemes",
                        principalTable: "schemes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "custodian_holding_lines",
                schema: "custody_reconciliation",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    custodian_statement_import_id = table.Column<Guid>(type: "uuid", nullable: false),
                    custodian_id = table.Column<Guid>(type: "uuid", nullable: false),
                    custodian_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    scheme_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheme_class_id = table.Column<Guid>(type: "uuid", nullable: true),
                    instrument_id = table.Column<Guid>(type: "uuid", nullable: true),
                    instrument_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    instrument_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    market_value = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    settlement_reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    is_settled = table.Column<bool>(type: "boolean", nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_custodian_holding_lines", x => x.id);
                    table.ForeignKey(
                        name: "fk_custodian_holding_lines_custodian_accounts_custodian_accoun",
                        column: x => x.custodian_account_id,
                        principalSchema: "custody_reconciliation",
                        principalTable: "custodian_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_custodian_holding_lines_custodian_statement_imports_custodi",
                        column: x => x.custodian_statement_import_id,
                        principalSchema: "custody_reconciliation",
                        principalTable: "custodian_statement_imports",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_custodian_holding_lines_custodians_custodian_id",
                        column: x => x.custodian_id,
                        principalSchema: "custody_reconciliation",
                        principalTable: "custodians",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_custodian_holding_lines_scheme_classes_scheme_class_id",
                        column: x => x.scheme_class_id,
                        principalSchema: "schemes",
                        principalTable: "scheme_classes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_custodian_holding_lines_schemes_scheme_id",
                        column: x => x.scheme_id,
                        principalSchema: "schemes",
                        principalTable: "schemes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "custody_reconciliation_runs",
                schema: "custody_reconciliation",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    custodian_id = table.Column<Guid>(type: "uuid", nullable: false),
                    holdings_import_id = table.Column<Guid>(type: "uuid", nullable: true),
                    cash_import_id = table.Column<Guid>(type: "uuid", nullable: true),
                    business_date = table.Column<DateOnly>(type: "date", nullable: false),
                    source_data_json = table.Column<string>(type: "jsonb", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    run_by_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    run_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    holding_break_count = table.Column<int>(type: "integer", nullable: false),
                    cash_break_count = table.Column<int>(type: "integer", nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    last_modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_custody_reconciliation_runs", x => x.id);
                    table.ForeignKey(
                        name: "fk_custody_reconciliation_runs_custodian_statement_imports_cas",
                        column: x => x.cash_import_id,
                        principalSchema: "custody_reconciliation",
                        principalTable: "custodian_statement_imports",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_custody_reconciliation_runs_custodian_statement_imports_hol",
                        column: x => x.holdings_import_id,
                        principalSchema: "custody_reconciliation",
                        principalTable: "custodian_statement_imports",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_custody_reconciliation_runs_custodians_custodian_id",
                        column: x => x.custodian_id,
                        principalSchema: "custody_reconciliation",
                        principalTable: "custodians",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "cash_reconciliation_breaks",
                schema: "custody_reconciliation",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    reconciliation_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cash_line_id = table.Column<Guid>(type: "uuid", nullable: true),
                    scheme_id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    internal_cash_balance = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    custodian_cash_balance = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    difference = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    break_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    severity = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    owner_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    assigned_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    resolved_by_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    resolved_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    resolution_evidence_reference = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_by_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    cash_reconciliation_break_created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    last_modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cash_reconciliation_breaks", x => x.id);
                    table.ForeignKey(
                        name: "fk_cash_reconciliation_breaks_custodian_cash_lines_cash_line_id",
                        column: x => x.cash_line_id,
                        principalSchema: "custody_reconciliation",
                        principalTable: "custodian_cash_lines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_cash_reconciliation_breaks_custody_reconciliation_runs_reco",
                        column: x => x.reconciliation_run_id,
                        principalSchema: "custody_reconciliation",
                        principalTable: "custody_reconciliation_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_cash_reconciliation_breaks_schemes_scheme_id",
                        column: x => x.scheme_id,
                        principalSchema: "schemes",
                        principalTable: "schemes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "holdings_reconciliation_breaks",
                schema: "custody_reconciliation",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    reconciliation_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    holding_line_id = table.Column<Guid>(type: "uuid", nullable: true),
                    scheme_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheme_class_id = table.Column<Guid>(type: "uuid", nullable: true),
                    instrument_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    internal_quantity = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    custodian_quantity = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    quantity_difference = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    internal_market_value = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    custodian_market_value = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    market_value_difference = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    break_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    severity = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    owner_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    assigned_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    resolved_by_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    resolved_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    resolution_evidence_reference = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_by_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    holdings_reconciliation_break_created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    last_modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_holdings_reconciliation_breaks", x => x.id);
                    table.ForeignKey(
                        name: "fk_holdings_reconciliation_breaks_custodian_holding_lines_hold",
                        column: x => x.holding_line_id,
                        principalSchema: "custody_reconciliation",
                        principalTable: "custodian_holding_lines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_holdings_reconciliation_breaks_custody_reconciliation_runs_",
                        column: x => x.reconciliation_run_id,
                        principalSchema: "custody_reconciliation",
                        principalTable: "custody_reconciliation_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_holdings_reconciliation_breaks_scheme_classes_scheme_class_",
                        column: x => x.scheme_class_id,
                        principalSchema: "schemes",
                        principalTable: "scheme_classes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_holdings_reconciliation_breaks_schemes_scheme_id",
                        column: x => x.scheme_id,
                        principalSchema: "schemes",
                        principalTable: "schemes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "safekeeping_confirmations",
                schema: "custody_reconciliation",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    custodian_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheme_id = table.Column<Guid>(type: "uuid", nullable: true),
                    source_reconciliation_run_id = table.Column<Guid>(type: "uuid", nullable: true),
                    business_date = table.Column<DateOnly>(type: "date", nullable: false),
                    holdings_count = table.Column<int>(type: "integer", nullable: false),
                    cash_line_count = table.Column<int>(type: "integer", nullable: false),
                    total_market_value = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    total_cash_balance = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    settlement_reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    settlement_confirmed = table.Column<bool>(type: "boolean", nullable: false),
                    confirmation_payload_json = table.Column<string>(type: "jsonb", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    generated_by_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    generated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    last_modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_safekeeping_confirmations", x => x.id);
                    table.ForeignKey(
                        name: "fk_safekeeping_confirmations_custodians_custodian_id",
                        column: x => x.custodian_id,
                        principalSchema: "custody_reconciliation",
                        principalTable: "custodians",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_safekeeping_confirmations_custody_reconciliation_runs_sourc",
                        column: x => x.source_reconciliation_run_id,
                        principalSchema: "custody_reconciliation",
                        principalTable: "custody_reconciliation_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_safekeeping_confirmations_schemes_scheme_id",
                        column: x => x.scheme_id,
                        principalSchema: "schemes",
                        principalTable: "schemes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "break_action_notes",
                schema: "custody_reconciliation",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    holding_break_id = table.Column<Guid>(type: "uuid", nullable: true),
                    cash_break_id = table.Column<Guid>(type: "uuid", nullable: true),
                    note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    evidence_reference = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_by_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_break_action_notes", x => x.id);
                    table.ForeignKey(
                        name: "fk_break_action_notes_cash_reconciliation_breaks_cash_break_id",
                        column: x => x.cash_break_id,
                        principalSchema: "custody_reconciliation",
                        principalTable: "cash_reconciliation_breaks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_break_action_notes_holdings_reconciliation_breaks_holding_b",
                        column: x => x.holding_break_id,
                        principalSchema: "custody_reconciliation",
                        principalTable: "holdings_reconciliation_breaks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "break_aging",
                schema: "custody_reconciliation",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    holding_break_id = table.Column<Guid>(type: "uuid", nullable: true),
                    cash_break_id = table.Column<Guid>(type: "uuid", nullable: true),
                    business_date = table.Column<DateOnly>(type: "date", nullable: false),
                    age_days = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    calculated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_break_aging", x => x.id);
                    table.ForeignKey(
                        name: "fk_break_aging_cash_reconciliation_breaks_cash_break_id",
                        column: x => x.cash_break_id,
                        principalSchema: "custody_reconciliation",
                        principalTable: "cash_reconciliation_breaks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_break_aging_holdings_reconciliation_breaks_holding_break_id",
                        column: x => x.holding_break_id,
                        principalSchema: "custody_reconciliation",
                        principalTable: "holdings_reconciliation_breaks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_break_action_notes_cash_break_id_created_at_utc",
                schema: "custody_reconciliation",
                table: "break_action_notes",
                columns: new[] { "cash_break_id", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_break_action_notes_holding_break_id_created_at_utc",
                schema: "custody_reconciliation",
                table: "break_action_notes",
                columns: new[] { "holding_break_id", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_break_aging_cash_break_id_calculated_at_utc",
                schema: "custody_reconciliation",
                table: "break_aging",
                columns: new[] { "cash_break_id", "calculated_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_break_aging_holding_break_id_calculated_at_utc",
                schema: "custody_reconciliation",
                table: "break_aging",
                columns: new[] { "holding_break_id", "calculated_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_cash_reconciliation_breaks_cash_line_id",
                schema: "custody_reconciliation",
                table: "cash_reconciliation_breaks",
                column: "cash_line_id");

            migrationBuilder.CreateIndex(
                name: "ix_cash_reconciliation_breaks_reconciliation_run_id_break_type",
                schema: "custody_reconciliation",
                table: "cash_reconciliation_breaks",
                columns: new[] { "reconciliation_run_id", "break_type", "account_number", "cash_line_id" });

            migrationBuilder.CreateIndex(
                name: "ix_cash_reconciliation_breaks_scheme_id",
                schema: "custody_reconciliation",
                table: "cash_reconciliation_breaks",
                column: "scheme_id");

            migrationBuilder.CreateIndex(
                name: "ix_cash_reconciliation_breaks_status_severity",
                schema: "custody_reconciliation",
                table: "cash_reconciliation_breaks",
                columns: new[] { "status", "severity" });

            migrationBuilder.CreateIndex(
                name: "ix_custodian_accounts_custodian_id_account_number_currency",
                schema: "custody_reconciliation",
                table: "custodian_accounts",
                columns: new[] { "custodian_id", "account_number", "currency" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_custodian_accounts_scheme_class_id",
                schema: "custody_reconciliation",
                table: "custodian_accounts",
                column: "scheme_class_id");

            migrationBuilder.CreateIndex(
                name: "ix_custodian_accounts_scheme_id_scheme_class_id",
                schema: "custody_reconciliation",
                table: "custodian_accounts",
                columns: new[] { "scheme_id", "scheme_class_id" });

            migrationBuilder.CreateIndex(
                name: "ix_custodian_cash_lines_custodian_account_id",
                schema: "custody_reconciliation",
                table: "custodian_cash_lines",
                column: "custodian_account_id");

            migrationBuilder.CreateIndex(
                name: "ix_custodian_cash_lines_custodian_id",
                schema: "custody_reconciliation",
                table: "custodian_cash_lines",
                column: "custodian_id");

            migrationBuilder.CreateIndex(
                name: "ix_custodian_cash_lines_custodian_statement_import_id_scheme_i",
                schema: "custody_reconciliation",
                table: "custodian_cash_lines",
                columns: new[] { "custodian_statement_import_id", "scheme_id", "account_number", "currency" });

            migrationBuilder.CreateIndex(
                name: "ix_custodian_cash_lines_scheme_id",
                schema: "custody_reconciliation",
                table: "custodian_cash_lines",
                column: "scheme_id");

            migrationBuilder.CreateIndex(
                name: "ix_custodian_holding_lines_custodian_account_id",
                schema: "custody_reconciliation",
                table: "custodian_holding_lines",
                column: "custodian_account_id");

            migrationBuilder.CreateIndex(
                name: "ix_custodian_holding_lines_custodian_id",
                schema: "custody_reconciliation",
                table: "custodian_holding_lines",
                column: "custodian_id");

            migrationBuilder.CreateIndex(
                name: "ix_custodian_holding_lines_custodian_statement_import_id_schem",
                schema: "custody_reconciliation",
                table: "custodian_holding_lines",
                columns: new[] { "custodian_statement_import_id", "scheme_id", "scheme_class_id", "instrument_code", "currency" });

            migrationBuilder.CreateIndex(
                name: "ix_custodian_holding_lines_scheme_class_id",
                schema: "custody_reconciliation",
                table: "custodian_holding_lines",
                column: "scheme_class_id");

            migrationBuilder.CreateIndex(
                name: "ix_custodian_holding_lines_scheme_id",
                schema: "custody_reconciliation",
                table: "custodian_holding_lines",
                column: "scheme_id");

            migrationBuilder.CreateIndex(
                name: "ix_custodian_statement_imports_custodian_account_id",
                schema: "custody_reconciliation",
                table: "custodian_statement_imports",
                column: "custodian_account_id");

            migrationBuilder.CreateIndex(
                name: "ix_custodian_statement_imports_custodian_id_statement_type_sta",
                schema: "custody_reconciliation",
                table: "custodian_statement_imports",
                columns: new[] { "custodian_id", "statement_type", "statement_date" });

            migrationBuilder.CreateIndex(
                name: "ix_custodian_statement_imports_idempotency_key",
                schema: "custody_reconciliation",
                table: "custodian_statement_imports",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_custodians_code",
                schema: "custody_reconciliation",
                table: "custodians",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_custodians_swift_code",
                schema: "custody_reconciliation",
                table: "custodians",
                column: "swift_code");

            migrationBuilder.CreateIndex(
                name: "ix_custody_reconciliation_runs_cash_import_id",
                schema: "custody_reconciliation",
                table: "custody_reconciliation_runs",
                column: "cash_import_id");

            migrationBuilder.CreateIndex(
                name: "ix_custody_reconciliation_runs_custodian_id_business_date",
                schema: "custody_reconciliation",
                table: "custody_reconciliation_runs",
                columns: new[] { "custodian_id", "business_date" });

            migrationBuilder.CreateIndex(
                name: "ix_custody_reconciliation_runs_holdings_import_id",
                schema: "custody_reconciliation",
                table: "custody_reconciliation_runs",
                column: "holdings_import_id");

            migrationBuilder.CreateIndex(
                name: "ix_holdings_reconciliation_breaks_holding_line_id",
                schema: "custody_reconciliation",
                table: "holdings_reconciliation_breaks",
                column: "holding_line_id");

            migrationBuilder.CreateIndex(
                name: "ix_holdings_reconciliation_breaks_reconciliation_run_id_break_",
                schema: "custody_reconciliation",
                table: "holdings_reconciliation_breaks",
                columns: new[] { "reconciliation_run_id", "break_type", "instrument_code", "holding_line_id" });

            migrationBuilder.CreateIndex(
                name: "ix_holdings_reconciliation_breaks_scheme_class_id",
                schema: "custody_reconciliation",
                table: "holdings_reconciliation_breaks",
                column: "scheme_class_id");

            migrationBuilder.CreateIndex(
                name: "ix_holdings_reconciliation_breaks_scheme_id",
                schema: "custody_reconciliation",
                table: "holdings_reconciliation_breaks",
                column: "scheme_id");

            migrationBuilder.CreateIndex(
                name: "ix_holdings_reconciliation_breaks_status_severity",
                schema: "custody_reconciliation",
                table: "holdings_reconciliation_breaks",
                columns: new[] { "status", "severity" });

            migrationBuilder.CreateIndex(
                name: "ix_safekeeping_confirmations_custodian_id_scheme_id_business_d",
                schema: "custody_reconciliation",
                table: "safekeeping_confirmations",
                columns: new[] { "custodian_id", "scheme_id", "business_date" });

            migrationBuilder.CreateIndex(
                name: "ix_safekeeping_confirmations_scheme_id",
                schema: "custody_reconciliation",
                table: "safekeeping_confirmations",
                column: "scheme_id");

            migrationBuilder.CreateIndex(
                name: "ix_safekeeping_confirmations_source_reconciliation_run_id",
                schema: "custody_reconciliation",
                table: "safekeeping_confirmations",
                column: "source_reconciliation_run_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "break_action_notes",
                schema: "custody_reconciliation");

            migrationBuilder.DropTable(
                name: "break_aging",
                schema: "custody_reconciliation");

            migrationBuilder.DropTable(
                name: "safekeeping_confirmations",
                schema: "custody_reconciliation");

            migrationBuilder.DropTable(
                name: "cash_reconciliation_breaks",
                schema: "custody_reconciliation");

            migrationBuilder.DropTable(
                name: "holdings_reconciliation_breaks",
                schema: "custody_reconciliation");

            migrationBuilder.DropTable(
                name: "custodian_cash_lines",
                schema: "custody_reconciliation");

            migrationBuilder.DropTable(
                name: "custodian_holding_lines",
                schema: "custody_reconciliation");

            migrationBuilder.DropTable(
                name: "custody_reconciliation_runs",
                schema: "custody_reconciliation");

            migrationBuilder.DropTable(
                name: "custodian_statement_imports",
                schema: "custody_reconciliation");

            migrationBuilder.DropTable(
                name: "custodian_accounts",
                schema: "custody_reconciliation");

            migrationBuilder.DropTable(
                name: "custodians",
                schema: "custody_reconciliation");
        }
    }
}

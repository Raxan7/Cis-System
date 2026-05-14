using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cis.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPortfolioModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "cash");

            migrationBuilder.EnsureSchema(
                name: "portfolio");

            migrationBuilder.AlterColumn<string>(
                name: "idempotency_key",
                schema: "unit_register",
                table: "unit_adjustments",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "bank_statement_imports",
                schema: "cash",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheme_bank_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    statement_date = table.Column<DateOnly>(type: "date", nullable: false),
                    date_tolerance_days = table.Column<int>(type: "integer", nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    source_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    imported_by_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    imported_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    line_count = table.Column<int>(type: "integer", nullable: false),
                    matched_line_count = table.Column<int>(type: "integer", nullable: false),
                    suspense_line_count = table.Column<int>(type: "integer", nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    last_modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bank_statement_imports", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "cash_book_entries",
                schema: "cash",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheme_bank_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    investor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    scheme_id = table.Column<Guid>(type: "uuid", nullable: true),
                    scheme_class_id = table.Column<Guid>(type: "uuid", nullable: true),
                    entry_date = table.Column<DateOnly>(type: "date", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    direction = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    source_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    source_entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    narrative = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    last_modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cash_book_entries", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "cash_matches",
                schema: "cash",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    bank_statement_line_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cash_book_entry_id = table.Column<Guid>(type: "uuid", nullable: false),
                    match_rule = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    related_entity_id = table.Column<Guid>(type: "uuid", nullable: true),
                    related_entity_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    matched_by_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    matched_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cash_matches", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "counterparties",
                schema: "portfolio",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    contact = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    last_modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_counterparties", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "counterparty_exposures",
                schema: "portfolio",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheme_id = table.Column<Guid>(type: "uuid", nullable: false),
                    counterparty_id = table.Column<Guid>(type: "uuid", nullable: false),
                    total_exposure = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    exposure_percentage = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    limit_percentage = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    last_calculated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_counterparty_exposures", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "instruments",
                schema: "portfolio",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    isin = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    instrument_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    counterparty_id = table.Column<Guid>(type: "uuid", nullable: true),
                    issuer_id = table.Column<Guid>(type: "uuid", nullable: true),
                    yield_rate = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: true),
                    coupon_rate = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: true),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    last_modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_instruments", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "investment_transactions",
                schema: "portfolio",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    placement_id = table.Column<Guid>(type: "uuid", nullable: false),
                    transaction_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    transaction_date = table.Column<DateOnly>(type: "date", nullable: false),
                    settlement_status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    reference = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    last_modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_investment_transactions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "issuers",
                schema: "portfolio",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    contact = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    last_modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_issuers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "mandate_validation_results",
                schema: "portfolio",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    placement_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    validated_field = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    limit_value = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: true),
                    actual_value = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: true),
                    validation_message = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    validated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mandate_validation_results", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "payment_instructions",
                schema: "cash",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    investor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheme_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheme_class_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheme_bank_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    payment_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    related_dealing_instruction_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    requested_by_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    requested_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    external_reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    failed_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    last_modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payment_instructions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "placements",
                schema: "portfolio",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheme_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheme_class_id = table.Column<Guid>(type: "uuid", nullable: false),
                    instrument_id = table.Column<Guid>(type: "uuid", nullable: false),
                    counterparty_id = table.Column<Guid>(type: "uuid", nullable: true),
                    issuer_id = table.Column<Guid>(type: "uuid", nullable: true),
                    principal = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    acquisition_date = table.Column<DateOnly>(type: "date", nullable: false),
                    maturity_date = table.Column<DateOnly>(type: "date", nullable: false),
                    yield = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    accrued_income = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    settlement_status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    submitted_by_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    submitted_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    approved_by_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    approved_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    rejected_by_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    rejected_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    rejection_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    last_modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_placements", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "portfolio_holdings",
                schema: "portfolio",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheme_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheme_class_id = table.Column<Guid>(type: "uuid", nullable: false),
                    instrument_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    market_value = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    last_updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_portfolio_holdings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "reconciliation_runs",
                schema: "cash",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheme_bank_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    run_date = table.Column<DateOnly>(type: "date", nullable: false),
                    aging_threshold_days = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    requested_by_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    requested_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    completed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_by_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    matched_count = table.Column<int>(type: "integer", nullable: false),
                    suspense_count = table.Column<int>(type: "integer", nullable: false),
                    break_count = table.Column<int>(type: "integer", nullable: false),
                    aged_break_count = table.Column<int>(type: "integer", nullable: false),
                    summary = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    last_modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reconciliation_runs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "returned_funds",
                schema: "cash",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_instruction_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    returned_by_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    returned_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    resolution_comment = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    last_modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_returned_funds", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "reversal_requests",
                schema: "cash",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_instruction_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    requested_by_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    requested_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    approved_by_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    approved_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    approval_comment = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    last_modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reversal_requests", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "suspense_items",
                schema: "cash",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    bank_statement_line_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheme_bank_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    opened_by_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    opened_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    resolved_by_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    resolved_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    resolution_comment = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    payment_instruction_id = table.Column<Guid>(type: "uuid", nullable: true),
                    cash_book_entry_id = table.Column<Guid>(type: "uuid", nullable: true),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    last_modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_suspense_items", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "bank_statement_lines",
                schema: "cash",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    bank_statement_import_id = table.Column<Guid>(type: "uuid", nullable: false),
                    line_number = table.Column<int>(type: "integer", nullable: false),
                    scheme_bank_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    transaction_date = table.Column<DateOnly>(type: "date", nullable: false),
                    reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    amount = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    direction = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    investor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    scheme_id = table.Column<Guid>(type: "uuid", nullable: true),
                    scheme_class_id = table.Column<Guid>(type: "uuid", nullable: true),
                    match_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    cash_book_entry_id = table.Column<Guid>(type: "uuid", nullable: true),
                    suspense_item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    related_entity_id = table.Column<Guid>(type: "uuid", nullable: true),
                    related_entity_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    match_rule = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    matched_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    matched_by_user_id = table.Column<string>(type: "text", nullable: true),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bank_statement_lines", x => x.id);
                    table.ForeignKey(
                        name: "fk_bank_statement_lines_bank_statement_imports_bank_statement_",
                        column: x => x.bank_statement_import_id,
                        principalSchema: "cash",
                        principalTable: "bank_statement_imports",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "payment_status_events",
                schema: "cash",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_instruction_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    event_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    occurred_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    external_reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    changed_by_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payment_status_events", x => x.id);
                    table.ForeignKey(
                        name: "fk_payment_status_events_payment_instructions_payment_instruct",
                        column: x => x.payment_instruction_id,
                        principalSchema: "cash",
                        principalTable: "payment_instructions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "income_schedules",
                schema: "portfolio",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    placement_id = table.Column<Guid>(type: "uuid", nullable: false),
                    income_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    due_date = table.Column<DateOnly>(type: "date", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    received = table.Column<bool>(type: "boolean", nullable: false),
                    received_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    received_by_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_income_schedules", x => x.id);
                    table.ForeignKey(
                        name: "fk_income_schedules_placements_placement_id",
                        column: x => x.placement_id,
                        principalSchema: "portfolio",
                        principalTable: "placements",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_bank_statement_imports_idempotency_key",
                schema: "cash",
                table: "bank_statement_imports",
                column: "idempotency_key",
                unique: true,
                filter: "idempotency_key IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_bank_statement_imports_scheme_bank_account_id_statement_date",
                schema: "cash",
                table: "bank_statement_imports",
                columns: new[] { "scheme_bank_account_id", "statement_date" });

            migrationBuilder.CreateIndex(
                name: "ix_bank_statement_lines_bank_statement_import_id_line_number",
                schema: "cash",
                table: "bank_statement_lines",
                columns: new[] { "bank_statement_import_id", "line_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_bank_statement_lines_match_status",
                schema: "cash",
                table: "bank_statement_lines",
                column: "match_status");

            migrationBuilder.CreateIndex(
                name: "ix_bank_statement_lines_scheme_bank_account_id_investor_id_sch",
                schema: "cash",
                table: "bank_statement_lines",
                columns: new[] { "scheme_bank_account_id", "investor_id", "scheme_id", "scheme_class_id" });

            migrationBuilder.CreateIndex(
                name: "ix_bank_statement_lines_suspense_item_id",
                schema: "cash",
                table: "bank_statement_lines",
                column: "suspense_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_cash_book_entries_reference",
                schema: "cash",
                table: "cash_book_entries",
                column: "reference");

            migrationBuilder.CreateIndex(
                name: "ix_cash_book_entries_scheme_bank_account_id_entry_date",
                schema: "cash",
                table: "cash_book_entries",
                columns: new[] { "scheme_bank_account_id", "entry_date" });

            migrationBuilder.CreateIndex(
                name: "ix_cash_book_entries_source_type_source_entity_id",
                schema: "cash",
                table: "cash_book_entries",
                columns: new[] { "source_type", "source_entity_id" });

            migrationBuilder.CreateIndex(
                name: "ix_cash_matches_bank_statement_line_id",
                schema: "cash",
                table: "cash_matches",
                column: "bank_statement_line_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_cash_matches_cash_book_entry_id",
                schema: "cash",
                table: "cash_matches",
                column: "cash_book_entry_id");

            migrationBuilder.CreateIndex(
                name: "ix_counterparties_code",
                schema: "portfolio",
                table: "counterparties",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_counterparty_exposures_last_calculated_at_utc",
                schema: "portfolio",
                table: "counterparty_exposures",
                column: "last_calculated_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_counterparty_exposures_scheme_id_counterparty_id",
                schema: "portfolio",
                table: "counterparty_exposures",
                columns: new[] { "scheme_id", "counterparty_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_income_schedules_placement_id_due_date",
                schema: "portfolio",
                table: "income_schedules",
                columns: new[] { "placement_id", "due_date" });

            migrationBuilder.CreateIndex(
                name: "ix_income_schedules_received_due_date",
                schema: "portfolio",
                table: "income_schedules",
                columns: new[] { "received", "due_date" });

            migrationBuilder.CreateIndex(
                name: "ix_instruments_isin",
                schema: "portfolio",
                table: "instruments",
                column: "isin",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_investment_transactions_placement_id_transaction_date",
                schema: "portfolio",
                table: "investment_transactions",
                columns: new[] { "placement_id", "transaction_date" });

            migrationBuilder.CreateIndex(
                name: "ix_investment_transactions_reference",
                schema: "portfolio",
                table: "investment_transactions",
                column: "reference");

            migrationBuilder.CreateIndex(
                name: "ix_issuers_code",
                schema: "portfolio",
                table: "issuers",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_mandate_validation_results_placement_id_status",
                schema: "portfolio",
                table: "mandate_validation_results",
                columns: new[] { "placement_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_mandate_validation_results_validated_at_utc",
                schema: "portfolio",
                table: "mandate_validation_results",
                column: "validated_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_payment_instructions_idempotency_key",
                schema: "cash",
                table: "payment_instructions",
                column: "idempotency_key",
                unique: true,
                filter: "idempotency_key IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_payment_instructions_scheme_bank_account_id_reference",
                schema: "cash",
                table: "payment_instructions",
                columns: new[] { "scheme_bank_account_id", "reference" });

            migrationBuilder.CreateIndex(
                name: "ix_payment_instructions_status",
                schema: "cash",
                table: "payment_instructions",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_payment_status_events_payment_instruction_id_occurred_at_utc",
                schema: "cash",
                table: "payment_status_events",
                columns: new[] { "payment_instruction_id", "occurred_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_placements_maturity_date",
                schema: "portfolio",
                table: "placements",
                column: "maturity_date");

            migrationBuilder.CreateIndex(
                name: "ix_placements_scheme_id_scheme_class_id",
                schema: "portfolio",
                table: "placements",
                columns: new[] { "scheme_id", "scheme_class_id" });

            migrationBuilder.CreateIndex(
                name: "ix_placements_status",
                schema: "portfolio",
                table: "placements",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_portfolio_holdings_last_updated_at_utc",
                schema: "portfolio",
                table: "portfolio_holdings",
                column: "last_updated_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_portfolio_holdings_scheme_id_scheme_class_id_instrument_id",
                schema: "portfolio",
                table: "portfolio_holdings",
                columns: new[] { "scheme_id", "scheme_class_id", "instrument_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_reconciliation_runs_scheme_bank_account_id_run_date",
                schema: "cash",
                table: "reconciliation_runs",
                columns: new[] { "scheme_bank_account_id", "run_date" });

            migrationBuilder.CreateIndex(
                name: "ix_reconciliation_runs_status",
                schema: "cash",
                table: "reconciliation_runs",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_returned_funds_payment_instruction_id",
                schema: "cash",
                table: "returned_funds",
                column: "payment_instruction_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_reversal_requests_payment_instruction_id",
                schema: "cash",
                table: "reversal_requests",
                column: "payment_instruction_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_suspense_items_cash_book_entry_id",
                schema: "cash",
                table: "suspense_items",
                column: "cash_book_entry_id");

            migrationBuilder.CreateIndex(
                name: "ix_suspense_items_payment_instruction_id",
                schema: "cash",
                table: "suspense_items",
                column: "payment_instruction_id");

            migrationBuilder.CreateIndex(
                name: "ix_suspense_items_scheme_bank_account_id_status_opened_at_utc",
                schema: "cash",
                table: "suspense_items",
                columns: new[] { "scheme_bank_account_id", "status", "opened_at_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bank_statement_lines",
                schema: "cash");

            migrationBuilder.DropTable(
                name: "cash_book_entries",
                schema: "cash");

            migrationBuilder.DropTable(
                name: "cash_matches",
                schema: "cash");

            migrationBuilder.DropTable(
                name: "counterparties",
                schema: "portfolio");

            migrationBuilder.DropTable(
                name: "counterparty_exposures",
                schema: "portfolio");

            migrationBuilder.DropTable(
                name: "income_schedules",
                schema: "portfolio");

            migrationBuilder.DropTable(
                name: "instruments",
                schema: "portfolio");

            migrationBuilder.DropTable(
                name: "investment_transactions",
                schema: "portfolio");

            migrationBuilder.DropTable(
                name: "issuers",
                schema: "portfolio");

            migrationBuilder.DropTable(
                name: "mandate_validation_results",
                schema: "portfolio");

            migrationBuilder.DropTable(
                name: "payment_status_events",
                schema: "cash");

            migrationBuilder.DropTable(
                name: "portfolio_holdings",
                schema: "portfolio");

            migrationBuilder.DropTable(
                name: "reconciliation_runs",
                schema: "cash");

            migrationBuilder.DropTable(
                name: "returned_funds",
                schema: "cash");

            migrationBuilder.DropTable(
                name: "reversal_requests",
                schema: "cash");

            migrationBuilder.DropTable(
                name: "suspense_items",
                schema: "cash");

            migrationBuilder.DropTable(
                name: "bank_statement_imports",
                schema: "cash");

            migrationBuilder.DropTable(
                name: "placements",
                schema: "portfolio");

            migrationBuilder.DropTable(
                name: "payment_instructions",
                schema: "cash");

            migrationBuilder.AlterColumn<string>(
                name: "idempotency_key",
                schema: "unit_register",
                table: "unit_adjustments",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);
        }
    }
}

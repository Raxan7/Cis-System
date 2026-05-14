using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cis.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFundAccountingModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "accounting");

            migrationBuilder.CreateTable(
                name: "accounting_nav_reconciliations",
                schema: "accounting",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheme_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheme_class_id = table.Column<Guid>(type: "uuid", nullable: true),
                    valuation_date = table.Column<DateOnly>(type: "date", nullable: false),
                    accounting_nav = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    published_nav = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: true),
                    difference = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    generated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_accounting_nav_reconciliations", x => x.id);
                    table.ForeignKey(
                        name: "fk_accounting_nav_reconciliations_scheme_classes_scheme_class_",
                        column: x => x.scheme_class_id,
                        principalSchema: "schemes",
                        principalTable: "scheme_classes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_accounting_nav_reconciliations_schemes_scheme_id",
                        column: x => x.scheme_id,
                        principalSchema: "schemes",
                        principalTable: "schemes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "accounting_periods",
                schema: "accounting",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheme_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    closed_by_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    closed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    last_modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_accounting_periods", x => x.id);
                    table.ForeignKey(
                        name: "fk_accounting_periods_schemes_scheme_id",
                        column: x => x.scheme_id,
                        principalSchema: "schemes",
                        principalTable: "schemes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "chart_of_accounts",
                schema: "accounting",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheme_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    base_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    last_modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_chart_of_accounts", x => x.id);
                    table.ForeignKey(
                        name: "fk_chart_of_accounts_schemes_scheme_id",
                        column: x => x.scheme_id,
                        principalSchema: "schemes",
                        principalTable: "schemes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "trial_balances",
                schema: "accounting",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheme_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheme_class_id = table.Column<Guid>(type: "uuid", nullable: true),
                    as_of_date = table.Column<DateOnly>(type: "date", nullable: false),
                    total_debits = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    total_credits = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    payload_json = table.Column<string>(type: "jsonb", nullable: false),
                    generated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_trial_balances", x => x.id);
                    table.ForeignKey(
                        name: "fk_trial_balances_scheme_classes_scheme_class_id",
                        column: x => x.scheme_class_id,
                        principalSchema: "schemes",
                        principalTable: "scheme_classes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_trial_balances_schemes_scheme_id",
                        column: x => x.scheme_id,
                        principalSchema: "schemes",
                        principalTable: "schemes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "financial_statements",
                schema: "accounting",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheme_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheme_class_id = table.Column<Guid>(type: "uuid", nullable: true),
                    accounting_period_id = table.Column<Guid>(type: "uuid", nullable: false),
                    statement_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    payload_json = table.Column<string>(type: "jsonb", nullable: false),
                    generated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_financial_statements", x => x.id);
                    table.ForeignKey(
                        name: "fk_financial_statements_accounting_periods_accounting_period_id",
                        column: x => x.accounting_period_id,
                        principalSchema: "accounting",
                        principalTable: "accounting_periods",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_financial_statements_scheme_classes_scheme_class_id",
                        column: x => x.scheme_class_id,
                        principalSchema: "schemes",
                        principalTable: "scheme_classes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_financial_statements_schemes_scheme_id",
                        column: x => x.scheme_id,
                        principalSchema: "schemes",
                        principalTable: "schemes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "accounts",
                schema: "accounting",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    chart_of_accounts_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheme_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    account_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    normal_balance = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    is_control_account = table.Column<bool>(type: "boolean", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_accounts", x => x.id);
                    table.ForeignKey(
                        name: "fk_accounts_chart_of_accounts_chart_of_accounts_id",
                        column: x => x.chart_of_accounts_id,
                        principalSchema: "accounting",
                        principalTable: "chart_of_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_accounts_schemes_scheme_id",
                        column: x => x.scheme_id,
                        principalSchema: "schemes",
                        principalTable: "schemes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "journal_templates",
                schema: "accounting",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    chart_of_accounts_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheme_id = table.Column<Guid>(type: "uuid", nullable: false),
                    journal_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    debit_account_code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    credit_account_code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_journal_templates", x => x.id);
                    table.ForeignKey(
                        name: "fk_journal_templates_chart_of_accounts_chart_of_accounts_id",
                        column: x => x.chart_of_accounts_id,
                        principalSchema: "accounting",
                        principalTable: "chart_of_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_journal_templates_schemes_scheme_id",
                        column: x => x.scheme_id,
                        principalSchema: "schemes",
                        principalTable: "schemes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ledgers",
                schema: "accounting",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheme_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheme_class_id = table.Column<Guid>(type: "uuid", nullable: true),
                    chart_of_accounts_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    last_modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ledgers", x => x.id);
                    table.ForeignKey(
                        name: "fk_ledgers_chart_of_accounts_chart_of_accounts_id",
                        column: x => x.chart_of_accounts_id,
                        principalSchema: "accounting",
                        principalTable: "chart_of_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ledgers_scheme_classes_scheme_class_id",
                        column: x => x.scheme_class_id,
                        principalSchema: "schemes",
                        principalTable: "scheme_classes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ledgers_schemes_scheme_id",
                        column: x => x.scheme_id,
                        principalSchema: "schemes",
                        principalTable: "schemes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "journals",
                schema: "accounting",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheme_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheme_class_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ledger_id = table.Column<Guid>(type: "uuid", nullable: false),
                    accounting_period_id = table.Column<Guid>(type: "uuid", nullable: false),
                    journal_number = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    automated_journal_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    posting_date = table.Column<DateOnly>(type: "date", nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    originating_event_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    originating_event_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    is_backdated = table.Column<bool>(type: "boolean", nullable: false),
                    is_correction = table.Column<bool>(type: "boolean", nullable: false),
                    corrected_journal_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_by_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    journal_created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    submitted_by_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    submitted_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    approved_by_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    approved_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    posted_by_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    posted_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    approval_comment = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    last_modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_journals", x => x.id);
                    table.ForeignKey(
                        name: "fk_journals_accounting_periods_accounting_period_id",
                        column: x => x.accounting_period_id,
                        principalSchema: "accounting",
                        principalTable: "accounting_periods",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_journals_journals_corrected_journal_id",
                        column: x => x.corrected_journal_id,
                        principalSchema: "accounting",
                        principalTable: "journals",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_journals_ledgers_ledger_id",
                        column: x => x.ledger_id,
                        principalSchema: "accounting",
                        principalTable: "ledgers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_journals_scheme_classes_scheme_class_id",
                        column: x => x.scheme_class_id,
                        principalSchema: "schemes",
                        principalTable: "scheme_classes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_journals_schemes_scheme_id",
                        column: x => x.scheme_id,
                        principalSchema: "schemes",
                        principalTable: "schemes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "fair_value_adjustments",
                schema: "accounting",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    journal_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheme_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheme_class_id = table.Column<Guid>(type: "uuid", nullable: true),
                    valuation_date = table.Column<DateOnly>(type: "date", nullable: false),
                    adjustment_amount = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    last_modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fair_value_adjustments", x => x.id);
                    table.ForeignKey(
                        name: "fk_fair_value_adjustments_journals_journal_id",
                        column: x => x.journal_id,
                        principalSchema: "accounting",
                        principalTable: "journals",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_fair_value_adjustments_scheme_classes_scheme_class_id",
                        column: x => x.scheme_class_id,
                        principalSchema: "schemes",
                        principalTable: "scheme_classes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_fair_value_adjustments_schemes_scheme_id",
                        column: x => x.scheme_id,
                        principalSchema: "schemes",
                        principalTable: "schemes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "journal_lines",
                schema: "accounting",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    journal_id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    line_number = table.Column<int>(type: "integer", nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    debit = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    credit = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_journal_lines", x => x.id);
                    table.ForeignKey(
                        name: "fk_journal_lines_accounts_account_id",
                        column: x => x.account_id,
                        principalSchema: "accounting",
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_journal_lines_journals_journal_id",
                        column: x => x.journal_id,
                        principalSchema: "accounting",
                        principalTable: "journals",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ledger_entries",
                schema: "accounting",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    journal_id = table.Column<Guid>(type: "uuid", nullable: false),
                    journal_line_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ledger_id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheme_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheme_class_id = table.Column<Guid>(type: "uuid", nullable: true),
                    posting_date = table.Column<DateOnly>(type: "date", nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    debit = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    credit = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    originating_event_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    originating_event_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    posted_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ledger_entries", x => x.id);
                    table.ForeignKey(
                        name: "fk_ledger_entries_accounts_account_id",
                        column: x => x.account_id,
                        principalSchema: "accounting",
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ledger_entries_journal_lines_journal_line_id",
                        column: x => x.journal_line_id,
                        principalSchema: "accounting",
                        principalTable: "journal_lines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ledger_entries_journals_journal_id",
                        column: x => x.journal_id,
                        principalSchema: "accounting",
                        principalTable: "journals",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ledger_entries_ledgers_ledger_id",
                        column: x => x.ledger_id,
                        principalSchema: "accounting",
                        principalTable: "ledgers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ledger_entries_scheme_classes_scheme_class_id",
                        column: x => x.scheme_class_id,
                        principalSchema: "schemes",
                        principalTable: "scheme_classes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ledger_entries_schemes_scheme_id",
                        column: x => x.scheme_id,
                        principalSchema: "schemes",
                        principalTable: "schemes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "suspense_ledger_entries",
                schema: "accounting",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    journal_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ledger_entry_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_suspense_ledger_entries", x => x.id);
                    table.ForeignKey(
                        name: "fk_suspense_ledger_entries_journals_journal_id",
                        column: x => x.journal_id,
                        principalSchema: "accounting",
                        principalTable: "journals",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_suspense_ledger_entries_ledger_entries_ledger_entry_id",
                        column: x => x.ledger_entry_id,
                        principalSchema: "accounting",
                        principalTable: "ledger_entries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_accounting_nav_reconciliations_generated_at_utc",
                schema: "accounting",
                table: "accounting_nav_reconciliations",
                column: "generated_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_accounting_nav_reconciliations_scheme_class_id",
                schema: "accounting",
                table: "accounting_nav_reconciliations",
                column: "scheme_class_id");

            migrationBuilder.CreateIndex(
                name: "ix_accounting_nav_reconciliations_scheme_id_scheme_class_id_va",
                schema: "accounting",
                table: "accounting_nav_reconciliations",
                columns: new[] { "scheme_id", "scheme_class_id", "valuation_date" });

            migrationBuilder.CreateIndex(
                name: "ix_accounting_periods_scheme_id_start_date_end_date",
                schema: "accounting",
                table: "accounting_periods",
                columns: new[] { "scheme_id", "start_date", "end_date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_accounting_periods_scheme_id_status",
                schema: "accounting",
                table: "accounting_periods",
                columns: new[] { "scheme_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_accounts_chart_of_accounts_id_code",
                schema: "accounting",
                table: "accounts",
                columns: new[] { "chart_of_accounts_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_accounts_scheme_id_account_type",
                schema: "accounting",
                table: "accounts",
                columns: new[] { "scheme_id", "account_type" });

            migrationBuilder.CreateIndex(
                name: "ix_chart_of_accounts_scheme_id_code",
                schema: "accounting",
                table: "chart_of_accounts",
                columns: new[] { "scheme_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_fair_value_adjustments_journal_id",
                schema: "accounting",
                table: "fair_value_adjustments",
                column: "journal_id");

            migrationBuilder.CreateIndex(
                name: "ix_fair_value_adjustments_scheme_class_id",
                schema: "accounting",
                table: "fair_value_adjustments",
                column: "scheme_class_id");

            migrationBuilder.CreateIndex(
                name: "ix_fair_value_adjustments_scheme_id_scheme_class_id_valuation_",
                schema: "accounting",
                table: "fair_value_adjustments",
                columns: new[] { "scheme_id", "scheme_class_id", "valuation_date" });

            migrationBuilder.CreateIndex(
                name: "ix_financial_statements_accounting_period_id",
                schema: "accounting",
                table: "financial_statements",
                column: "accounting_period_id");

            migrationBuilder.CreateIndex(
                name: "ix_financial_statements_scheme_class_id",
                schema: "accounting",
                table: "financial_statements",
                column: "scheme_class_id");

            migrationBuilder.CreateIndex(
                name: "ix_financial_statements_scheme_id_accounting_period_id_stateme",
                schema: "accounting",
                table: "financial_statements",
                columns: new[] { "scheme_id", "accounting_period_id", "statement_type" });

            migrationBuilder.CreateIndex(
                name: "ix_journal_lines_account_id",
                schema: "accounting",
                table: "journal_lines",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "ix_journal_lines_journal_id_line_number",
                schema: "accounting",
                table: "journal_lines",
                columns: new[] { "journal_id", "line_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_journal_templates_chart_of_accounts_id_journal_type",
                schema: "accounting",
                table: "journal_templates",
                columns: new[] { "chart_of_accounts_id", "journal_type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_journal_templates_scheme_id",
                schema: "accounting",
                table: "journal_templates",
                column: "scheme_id");

            migrationBuilder.CreateIndex(
                name: "ix_journals_accounting_period_id",
                schema: "accounting",
                table: "journals",
                column: "accounting_period_id");

            migrationBuilder.CreateIndex(
                name: "ix_journals_corrected_journal_id",
                schema: "accounting",
                table: "journals",
                column: "corrected_journal_id");

            migrationBuilder.CreateIndex(
                name: "ix_journals_journal_number",
                schema: "accounting",
                table: "journals",
                column: "journal_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_journals_ledger_id",
                schema: "accounting",
                table: "journals",
                column: "ledger_id");

            migrationBuilder.CreateIndex(
                name: "ix_journals_originating_event_type_originating_event_id",
                schema: "accounting",
                table: "journals",
                columns: new[] { "originating_event_type", "originating_event_id" });

            migrationBuilder.CreateIndex(
                name: "ix_journals_scheme_class_id",
                schema: "accounting",
                table: "journals",
                column: "scheme_class_id");

            migrationBuilder.CreateIndex(
                name: "ix_journals_scheme_id_posting_date",
                schema: "accounting",
                table: "journals",
                columns: new[] { "scheme_id", "posting_date" });

            migrationBuilder.CreateIndex(
                name: "ix_ledger_entries_account_id_posting_date",
                schema: "accounting",
                table: "ledger_entries",
                columns: new[] { "account_id", "posting_date" });

            migrationBuilder.CreateIndex(
                name: "ix_ledger_entries_journal_id",
                schema: "accounting",
                table: "ledger_entries",
                column: "journal_id");

            migrationBuilder.CreateIndex(
                name: "ix_ledger_entries_journal_line_id",
                schema: "accounting",
                table: "ledger_entries",
                column: "journal_line_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ledger_entries_ledger_id",
                schema: "accounting",
                table: "ledger_entries",
                column: "ledger_id");

            migrationBuilder.CreateIndex(
                name: "ix_ledger_entries_originating_event_type_originating_event_id",
                schema: "accounting",
                table: "ledger_entries",
                columns: new[] { "originating_event_type", "originating_event_id" });

            migrationBuilder.CreateIndex(
                name: "ix_ledger_entries_scheme_class_id",
                schema: "accounting",
                table: "ledger_entries",
                column: "scheme_class_id");

            migrationBuilder.CreateIndex(
                name: "ix_ledger_entries_scheme_id_scheme_class_id_posting_date",
                schema: "accounting",
                table: "ledger_entries",
                columns: new[] { "scheme_id", "scheme_class_id", "posting_date" });

            migrationBuilder.CreateIndex(
                name: "ix_ledgers_chart_of_accounts_id",
                schema: "accounting",
                table: "ledgers",
                column: "chart_of_accounts_id");

            migrationBuilder.CreateIndex(
                name: "ix_ledgers_scheme_class_id",
                schema: "accounting",
                table: "ledgers",
                column: "scheme_class_id");

            migrationBuilder.CreateIndex(
                name: "ix_ledgers_scheme_id_code",
                schema: "accounting",
                table: "ledgers",
                columns: new[] { "scheme_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ledgers_scheme_id_scheme_class_id",
                schema: "accounting",
                table: "ledgers",
                columns: new[] { "scheme_id", "scheme_class_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_suspense_ledger_entries_journal_id",
                schema: "accounting",
                table: "suspense_ledger_entries",
                column: "journal_id");

            migrationBuilder.CreateIndex(
                name: "ix_suspense_ledger_entries_ledger_entry_id",
                schema: "accounting",
                table: "suspense_ledger_entries",
                column: "ledger_entry_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_suspense_ledger_entries_status",
                schema: "accounting",
                table: "suspense_ledger_entries",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_trial_balances_scheme_class_id",
                schema: "accounting",
                table: "trial_balances",
                column: "scheme_class_id");

            migrationBuilder.CreateIndex(
                name: "ix_trial_balances_scheme_id_scheme_class_id_as_of_date",
                schema: "accounting",
                table: "trial_balances",
                columns: new[] { "scheme_id", "scheme_class_id", "as_of_date" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "accounting_nav_reconciliations",
                schema: "accounting");

            migrationBuilder.DropTable(
                name: "fair_value_adjustments",
                schema: "accounting");

            migrationBuilder.DropTable(
                name: "financial_statements",
                schema: "accounting");

            migrationBuilder.DropTable(
                name: "journal_templates",
                schema: "accounting");

            migrationBuilder.DropTable(
                name: "suspense_ledger_entries",
                schema: "accounting");

            migrationBuilder.DropTable(
                name: "trial_balances",
                schema: "accounting");

            migrationBuilder.DropTable(
                name: "ledger_entries",
                schema: "accounting");

            migrationBuilder.DropTable(
                name: "journal_lines",
                schema: "accounting");

            migrationBuilder.DropTable(
                name: "accounts",
                schema: "accounting");

            migrationBuilder.DropTable(
                name: "journals",
                schema: "accounting");

            migrationBuilder.DropTable(
                name: "accounting_periods",
                schema: "accounting");

            migrationBuilder.DropTable(
                name: "ledgers",
                schema: "accounting");

            migrationBuilder.DropTable(
                name: "chart_of_accounts",
                schema: "accounting");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cis.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFeesTaxDistributionModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "fees_tax_distribution");

            migrationBuilder.CreateTable(
                name: "distribution_declarations",
                schema: "fees_tax_distribution",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheme_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheme_class_id = table.Column<Guid>(type: "uuid", nullable: false),
                    declaration_date = table.Column<DateOnly>(type: "date", nullable: false),
                    record_date = table.Column<DateOnly>(type: "date", nullable: false),
                    payment_date = table.Column<DateOnly>(type: "date", nullable: false),
                    formula_version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    investment_income = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    other_income = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    realized_gains_losses = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    prior_period_adjustments = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    fund_expenses = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    fees = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    taxes = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    reserve_transfers = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    gross_distributable_income = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    net_distributable_income = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    eligible_units = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    distribution_per_unit = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    available_cash = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    planned_distribution_amount = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    distribution_coverage = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    coverage_override_requested = table.Column<bool>(type: "boolean", nullable: false),
                    coverage_override_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    coverage_override_approved = table.Column<bool>(type: "boolean", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_by_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    distribution_declaration_created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
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
                    table.PrimaryKey("pk_distribution_declarations", x => x.id);
                    table.ForeignKey(
                        name: "fk_distribution_declarations_scheme_classes_scheme_class_id",
                        column: x => x.scheme_class_id,
                        principalSchema: "schemes",
                        principalTable: "scheme_classes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_distribution_declarations_schemes_scheme_id",
                        column: x => x.scheme_id,
                        principalSchema: "schemes",
                        principalTable: "schemes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "fee_accrual_runs",
                schema: "fees_tax_distribution",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheme_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheme_class_id = table.Column<Guid>(type: "uuid", nullable: false),
                    period_start = table.Column<DateOnly>(type: "date", nullable: false),
                    period_end = table.Column<DateOnly>(type: "date", nullable: false),
                    run_number = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    formula_version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    day_count_basis = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    internal_precision = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    owner_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    calculated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    approved_by_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    approved_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    total_fee_amount = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    total_waiver_amount = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    total_vat_amount = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    total_withholding_tax_amount = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    net_payable_amount = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    total_expense_ratio = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    last_modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fee_accrual_runs", x => x.id);
                    table.ForeignKey(
                        name: "fk_fee_accrual_runs_scheme_classes_scheme_class_id",
                        column: x => x.scheme_class_id,
                        principalSchema: "schemes",
                        principalTable: "scheme_classes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_fee_accrual_runs_schemes_scheme_id",
                        column: x => x.scheme_id,
                        principalSchema: "schemes",
                        principalTable: "schemes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "fee_waiver_requests",
                schema: "fees_tax_distribution",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheme_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheme_class_id = table.Column<Guid>(type: "uuid", nullable: false),
                    investor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    fee_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    effective_from = table.Column<DateOnly>(type: "date", nullable: false),
                    effective_to = table.Column<DateOnly>(type: "date", nullable: true),
                    waiver_rate = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: true),
                    waiver_amount = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: true),
                    reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    requested_by_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    requested_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    approved_by_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    approved_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    decision_comment = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    last_modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fee_waiver_requests", x => x.id);
                    table.ForeignKey(
                        name: "fk_fee_waiver_requests_investors_investor_id",
                        column: x => x.investor_id,
                        principalSchema: "investors",
                        principalTable: "investors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_fee_waiver_requests_scheme_classes_scheme_class_id",
                        column: x => x.scheme_class_id,
                        principalSchema: "schemes",
                        principalTable: "scheme_classes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_fee_waiver_requests_schemes_scheme_id",
                        column: x => x.scheme_id,
                        principalSchema: "schemes",
                        principalTable: "schemes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tax_rules",
                schema: "fees_tax_distribution",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    jurisdiction = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    tax_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    rate = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    effective_from = table.Column<DateOnly>(type: "date", nullable: false),
                    effective_to = table.Column<DateOnly>(type: "date", nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_by_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    tax_rule_created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    last_modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tax_rules", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "distribution_runs",
                schema: "fees_tax_distribution",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    declaration_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheme_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheme_class_id = table.Column<Guid>(type: "uuid", nullable: false),
                    run_date = table.Column<DateOnly>(type: "date", nullable: false),
                    run_number = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_by_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    distribution_run_created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    published_by_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    published_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    total_gross_distribution = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    total_tax_amount = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    total_net_distribution = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    total_reinvested_units = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    last_modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_distribution_runs", x => x.id);
                    table.ForeignKey(
                        name: "fk_distribution_runs_distribution_declarations_declaration_id",
                        column: x => x.declaration_id,
                        principalSchema: "fees_tax_distribution",
                        principalTable: "distribution_declarations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_distribution_runs_scheme_classes_scheme_class_id",
                        column: x => x.scheme_class_id,
                        principalSchema: "schemes",
                        principalTable: "scheme_classes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_distribution_runs_schemes_scheme_id",
                        column: x => x.scheme_id,
                        principalSchema: "schemes",
                        principalTable: "schemes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "fee_calculations",
                schema: "fees_tax_distribution",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    fee_accrual_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fee_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    formula_code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    formula_version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    applicable_fee_base = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    annual_rate = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: true),
                    period_days = table.Column<int>(type: "integer", nullable: true),
                    fixed_amount = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: true),
                    gross_amount = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    waiver_amount = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    net_amount = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    inputs_json = table.Column<string>(type: "jsonb", nullable: false),
                    output_json = table.Column<string>(type: "jsonb", nullable: false),
                    owner_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    approved_by_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fee_calculations", x => x.id);
                    table.ForeignKey(
                        name: "fk_fee_calculations_fee_accrual_runs_fee_accrual_run_id",
                        column: x => x.fee_accrual_run_id,
                        principalSchema: "fees_tax_distribution",
                        principalTable: "fee_accrual_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "investor_distributions",
                schema: "fees_tax_distribution",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    distribution_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    investor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    eligible_units = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    distribution_per_unit = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    gross_distribution = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    investor_tax_amount = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    net_distribution = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    method = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    opening_value = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    closing_value = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    cash_distributions = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    net_contributions = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    investor_return_for_period = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    tax_rule_id = table.Column<Guid>(type: "uuid", nullable: true),
                    formula_version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    owner_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    approver_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_investor_distributions", x => x.id);
                    table.ForeignKey(
                        name: "fk_investor_distributions_distribution_runs_distribution_run_id",
                        column: x => x.distribution_run_id,
                        principalSchema: "fees_tax_distribution",
                        principalTable: "distribution_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_investor_distributions_investors_investor_id",
                        column: x => x.investor_id,
                        principalSchema: "investors",
                        principalTable: "investors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_investor_distributions_tax_rules_tax_rule_id",
                        column: x => x.tax_rule_id,
                        principalSchema: "fees_tax_distribution",
                        principalTable: "tax_rules",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "reinvestment_instructions",
                schema: "fees_tax_distribution",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    distribution_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    investor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reinvestment_price = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    owner_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    approved_by_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    approved_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reinvestment_instructions", x => x.id);
                    table.ForeignKey(
                        name: "fk_reinvestment_instructions_distribution_runs_distribution_ru",
                        column: x => x.distribution_run_id,
                        principalSchema: "fees_tax_distribution",
                        principalTable: "distribution_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_reinvestment_instructions_investors_investor_id",
                        column: x => x.investor_id,
                        principalSchema: "investors",
                        principalTable: "investors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "vat_calculations",
                schema: "fees_tax_distribution",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    fee_accrual_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fee_calculation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tax_rule_id = table.Column<Guid>(type: "uuid", nullable: true),
                    taxable_fee_amount = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    vat_rate = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    vat_amount = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    formula_version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    owner_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    approver_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vat_calculations", x => x.id);
                    table.ForeignKey(
                        name: "fk_vat_calculations_fee_accrual_runs_fee_accrual_run_id",
                        column: x => x.fee_accrual_run_id,
                        principalSchema: "fees_tax_distribution",
                        principalTable: "fee_accrual_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_vat_calculations_fee_calculations_fee_calculation_id",
                        column: x => x.fee_calculation_id,
                        principalSchema: "fees_tax_distribution",
                        principalTable: "fee_calculations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_vat_calculations_tax_rules_tax_rule_id",
                        column: x => x.tax_rule_id,
                        principalSchema: "fees_tax_distribution",
                        principalTable: "tax_rules",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "withholding_tax_calculations",
                schema: "fees_tax_distribution",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    fee_accrual_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fee_calculation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tax_rule_id = table.Column<Guid>(type: "uuid", nullable: true),
                    taxable_payment_base = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    withholding_tax_rate = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    withholding_tax_amount = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    formula_version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    owner_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    approver_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_withholding_tax_calculations", x => x.id);
                    table.ForeignKey(
                        name: "fk_withholding_tax_calculations_fee_accrual_runs_fee_accrual_r",
                        column: x => x.fee_accrual_run_id,
                        principalSchema: "fees_tax_distribution",
                        principalTable: "fee_accrual_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_withholding_tax_calculations_fee_calculations_fee_calculati",
                        column: x => x.fee_calculation_id,
                        principalSchema: "fees_tax_distribution",
                        principalTable: "fee_calculations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_withholding_tax_calculations_tax_rules_tax_rule_id",
                        column: x => x.tax_rule_id,
                        principalSchema: "fees_tax_distribution",
                        principalTable: "tax_rules",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tax_calculations",
                schema: "fees_tax_distribution",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    fee_accrual_run_id = table.Column<Guid>(type: "uuid", nullable: true),
                    investor_distribution_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tax_rule_id = table.Column<Guid>(type: "uuid", nullable: true),
                    calculation_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    formula_code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    formula_version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    taxable_base = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    rate = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    tax_amount = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    inputs_json = table.Column<string>(type: "jsonb", nullable: false),
                    output_json = table.Column<string>(type: "jsonb", nullable: false),
                    owner_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    approver_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tax_calculations", x => x.id);
                    table.ForeignKey(
                        name: "fk_tax_calculations_fee_accrual_runs_fee_accrual_run_id",
                        column: x => x.fee_accrual_run_id,
                        principalSchema: "fees_tax_distribution",
                        principalTable: "fee_accrual_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_tax_calculations_investor_distributions_investor_distributi",
                        column: x => x.investor_distribution_id,
                        principalSchema: "fees_tax_distribution",
                        principalTable: "investor_distributions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_tax_calculations_tax_rules_tax_rule_id",
                        column: x => x.tax_rule_id,
                        principalSchema: "fees_tax_distribution",
                        principalTable: "tax_rules",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "reinvestment_unit_allocations",
                schema: "fees_tax_distribution",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    distribution_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    investor_distribution_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reinvestment_instruction_id = table.Column<Guid>(type: "uuid", nullable: false),
                    investor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    unit_ledger_entry_id = table.Column<Guid>(type: "uuid", nullable: true),
                    net_distribution = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    reinvestment_price = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    units_allocated = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    source_reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    formula_version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    allocated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reinvestment_unit_allocations", x => x.id);
                    table.ForeignKey(
                        name: "fk_reinvestment_unit_allocations_distribution_runs_distributio",
                        column: x => x.distribution_run_id,
                        principalSchema: "fees_tax_distribution",
                        principalTable: "distribution_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_reinvestment_unit_allocations_investor_distributions_invest",
                        column: x => x.investor_distribution_id,
                        principalSchema: "fees_tax_distribution",
                        principalTable: "investor_distributions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_reinvestment_unit_allocations_investors_investor_id",
                        column: x => x.investor_id,
                        principalSchema: "investors",
                        principalTable: "investors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_reinvestment_unit_allocations_reinvestment_instructions_rei",
                        column: x => x.reinvestment_instruction_id,
                        principalSchema: "fees_tax_distribution",
                        principalTable: "reinvestment_instructions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_reinvestment_unit_allocations_unit_ledger_entries_unit_ledg",
                        column: x => x.unit_ledger_entry_id,
                        principalSchema: "unit_register",
                        principalTable: "unit_ledger_entries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_distribution_declarations_scheme_class_id",
                schema: "fees_tax_distribution",
                table: "distribution_declarations",
                column: "scheme_class_id");

            migrationBuilder.CreateIndex(
                name: "ix_distribution_declarations_scheme_id_scheme_class_id_record_",
                schema: "fees_tax_distribution",
                table: "distribution_declarations",
                columns: new[] { "scheme_id", "scheme_class_id", "record_date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_distribution_declarations_status",
                schema: "fees_tax_distribution",
                table: "distribution_declarations",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_distribution_runs_declaration_id",
                schema: "fees_tax_distribution",
                table: "distribution_runs",
                column: "declaration_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_distribution_runs_run_number",
                schema: "fees_tax_distribution",
                table: "distribution_runs",
                column: "run_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_distribution_runs_scheme_class_id",
                schema: "fees_tax_distribution",
                table: "distribution_runs",
                column: "scheme_class_id");

            migrationBuilder.CreateIndex(
                name: "ix_distribution_runs_scheme_id",
                schema: "fees_tax_distribution",
                table: "distribution_runs",
                column: "scheme_id");

            migrationBuilder.CreateIndex(
                name: "ix_distribution_runs_status",
                schema: "fees_tax_distribution",
                table: "distribution_runs",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_fee_accrual_runs_run_number",
                schema: "fees_tax_distribution",
                table: "fee_accrual_runs",
                column: "run_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_fee_accrual_runs_scheme_class_id",
                schema: "fees_tax_distribution",
                table: "fee_accrual_runs",
                column: "scheme_class_id");

            migrationBuilder.CreateIndex(
                name: "ix_fee_accrual_runs_scheme_id_scheme_class_id_period_start_per",
                schema: "fees_tax_distribution",
                table: "fee_accrual_runs",
                columns: new[] { "scheme_id", "scheme_class_id", "period_start", "period_end" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_fee_accrual_runs_status",
                schema: "fees_tax_distribution",
                table: "fee_accrual_runs",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_fee_calculations_fee_accrual_run_id_fee_type_formula_code",
                schema: "fees_tax_distribution",
                table: "fee_calculations",
                columns: new[] { "fee_accrual_run_id", "fee_type", "formula_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_fee_waiver_requests_investor_id",
                schema: "fees_tax_distribution",
                table: "fee_waiver_requests",
                column: "investor_id");

            migrationBuilder.CreateIndex(
                name: "ix_fee_waiver_requests_scheme_class_id",
                schema: "fees_tax_distribution",
                table: "fee_waiver_requests",
                column: "scheme_class_id");

            migrationBuilder.CreateIndex(
                name: "ix_fee_waiver_requests_scheme_id_scheme_class_id_fee_type_effe",
                schema: "fees_tax_distribution",
                table: "fee_waiver_requests",
                columns: new[] { "scheme_id", "scheme_class_id", "fee_type", "effective_from", "investor_id" });

            migrationBuilder.CreateIndex(
                name: "ix_fee_waiver_requests_status",
                schema: "fees_tax_distribution",
                table: "fee_waiver_requests",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_investor_distributions_distribution_run_id_investor_id",
                schema: "fees_tax_distribution",
                table: "investor_distributions",
                columns: new[] { "distribution_run_id", "investor_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_investor_distributions_investor_id",
                schema: "fees_tax_distribution",
                table: "investor_distributions",
                column: "investor_id");

            migrationBuilder.CreateIndex(
                name: "ix_investor_distributions_tax_rule_id",
                schema: "fees_tax_distribution",
                table: "investor_distributions",
                column: "tax_rule_id");

            migrationBuilder.CreateIndex(
                name: "ix_reinvestment_instructions_distribution_run_id_investor_id",
                schema: "fees_tax_distribution",
                table: "reinvestment_instructions",
                columns: new[] { "distribution_run_id", "investor_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_reinvestment_instructions_investor_id",
                schema: "fees_tax_distribution",
                table: "reinvestment_instructions",
                column: "investor_id");

            migrationBuilder.CreateIndex(
                name: "ix_reinvestment_unit_allocations_distribution_run_id",
                schema: "fees_tax_distribution",
                table: "reinvestment_unit_allocations",
                column: "distribution_run_id");

            migrationBuilder.CreateIndex(
                name: "ix_reinvestment_unit_allocations_investor_distribution_id",
                schema: "fees_tax_distribution",
                table: "reinvestment_unit_allocations",
                column: "investor_distribution_id");

            migrationBuilder.CreateIndex(
                name: "ix_reinvestment_unit_allocations_investor_id",
                schema: "fees_tax_distribution",
                table: "reinvestment_unit_allocations",
                column: "investor_id");

            migrationBuilder.CreateIndex(
                name: "ix_reinvestment_unit_allocations_reinvestment_instruction_id",
                schema: "fees_tax_distribution",
                table: "reinvestment_unit_allocations",
                column: "reinvestment_instruction_id");

            migrationBuilder.CreateIndex(
                name: "ix_reinvestment_unit_allocations_source_reference",
                schema: "fees_tax_distribution",
                table: "reinvestment_unit_allocations",
                column: "source_reference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_reinvestment_unit_allocations_unit_ledger_entry_id",
                schema: "fees_tax_distribution",
                table: "reinvestment_unit_allocations",
                column: "unit_ledger_entry_id",
                unique: true,
                filter: "unit_ledger_entry_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_tax_calculations_fee_accrual_run_id",
                schema: "fees_tax_distribution",
                table: "tax_calculations",
                column: "fee_accrual_run_id");

            migrationBuilder.CreateIndex(
                name: "ix_tax_calculations_investor_distribution_id",
                schema: "fees_tax_distribution",
                table: "tax_calculations",
                column: "investor_distribution_id");

            migrationBuilder.CreateIndex(
                name: "ix_tax_calculations_tax_rule_id",
                schema: "fees_tax_distribution",
                table: "tax_calculations",
                column: "tax_rule_id");

            migrationBuilder.CreateIndex(
                name: "ix_tax_rules_jurisdiction_category_tax_type_effective_from",
                schema: "fees_tax_distribution",
                table: "tax_rules",
                columns: new[] { "jurisdiction", "category", "tax_type", "effective_from" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tax_rules_jurisdiction_category_tax_type_status",
                schema: "fees_tax_distribution",
                table: "tax_rules",
                columns: new[] { "jurisdiction", "category", "tax_type", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_vat_calculations_fee_accrual_run_id",
                schema: "fees_tax_distribution",
                table: "vat_calculations",
                column: "fee_accrual_run_id");

            migrationBuilder.CreateIndex(
                name: "ix_vat_calculations_fee_calculation_id",
                schema: "fees_tax_distribution",
                table: "vat_calculations",
                column: "fee_calculation_id");

            migrationBuilder.CreateIndex(
                name: "ix_vat_calculations_tax_rule_id",
                schema: "fees_tax_distribution",
                table: "vat_calculations",
                column: "tax_rule_id");

            migrationBuilder.CreateIndex(
                name: "ix_withholding_tax_calculations_fee_accrual_run_id",
                schema: "fees_tax_distribution",
                table: "withholding_tax_calculations",
                column: "fee_accrual_run_id");

            migrationBuilder.CreateIndex(
                name: "ix_withholding_tax_calculations_fee_calculation_id",
                schema: "fees_tax_distribution",
                table: "withholding_tax_calculations",
                column: "fee_calculation_id");

            migrationBuilder.CreateIndex(
                name: "ix_withholding_tax_calculations_tax_rule_id",
                schema: "fees_tax_distribution",
                table: "withholding_tax_calculations",
                column: "tax_rule_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "fee_waiver_requests",
                schema: "fees_tax_distribution");

            migrationBuilder.DropTable(
                name: "reinvestment_unit_allocations",
                schema: "fees_tax_distribution");

            migrationBuilder.DropTable(
                name: "tax_calculations",
                schema: "fees_tax_distribution");

            migrationBuilder.DropTable(
                name: "vat_calculations",
                schema: "fees_tax_distribution");

            migrationBuilder.DropTable(
                name: "withholding_tax_calculations",
                schema: "fees_tax_distribution");

            migrationBuilder.DropTable(
                name: "reinvestment_instructions",
                schema: "fees_tax_distribution");

            migrationBuilder.DropTable(
                name: "investor_distributions",
                schema: "fees_tax_distribution");

            migrationBuilder.DropTable(
                name: "fee_calculations",
                schema: "fees_tax_distribution");

            migrationBuilder.DropTable(
                name: "distribution_runs",
                schema: "fees_tax_distribution");

            migrationBuilder.DropTable(
                name: "tax_rules",
                schema: "fees_tax_distribution");

            migrationBuilder.DropTable(
                name: "fee_accrual_runs",
                schema: "fees_tax_distribution");

            migrationBuilder.DropTable(
                name: "distribution_declarations",
                schema: "fees_tax_distribution");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cis.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNavValuationModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "nav");

            migrationBuilder.CreateTable(
                name: "manual_valuation_overrides",
                schema: "nav",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheme_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheme_class_id = table.Column<Guid>(type: "uuid", nullable: false),
                    valuation_date = table.Column<DateOnly>(type: "date", nullable: false),
                    instrument_id = table.Column<Guid>(type: "uuid", nullable: false),
                    override_price = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    override_value = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
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
                    table.PrimaryKey("pk_manual_valuation_overrides", x => x.id);
                    table.ForeignKey(
                        name: "fk_manual_valuation_overrides_instruments_instrument_id",
                        column: x => x.instrument_id,
                        principalSchema: "portfolio",
                        principalTable: "instruments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_manual_valuation_overrides_scheme_classes_scheme_class_id",
                        column: x => x.scheme_class_id,
                        principalSchema: "schemes",
                        principalTable: "scheme_classes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_manual_valuation_overrides_schemes_scheme_id",
                        column: x => x.scheme_id,
                        principalSchema: "schemes",
                        principalTable: "schemes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "price_source_hierarchies",
                schema: "nav",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheme_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheme_class_id = table.Column<Guid>(type: "uuid", nullable: false),
                    instrument_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    primary_source = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    secondary_source = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    manual_fallback_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    max_price_age_days = table.Column<int>(type: "integer", nullable: false),
                    variance_tolerance_percent = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    last_modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_price_source_hierarchies", x => x.id);
                    table.ForeignKey(
                        name: "fk_price_source_hierarchies_scheme_classes_scheme_class_id",
                        column: x => x.scheme_class_id,
                        principalSchema: "schemes",
                        principalTable: "scheme_classes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_price_source_hierarchies_schemes_scheme_id",
                        column: x => x.scheme_id,
                        principalSchema: "schemes",
                        principalTable: "schemes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "valuation_runs",
                schema: "nav",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheme_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheme_class_id = table.Column<Guid>(type: "uuid", nullable: false),
                    valuation_date = table.Column<DateOnly>(type: "date", nullable: false),
                    run_number = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    formula_version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    day_count_basis = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    internal_precision = table.Column<int>(type: "integer", nullable: false),
                    report_precision = table.Column<int>(type: "integer", nullable: false),
                    price_stale_after_days = table.Column<int>(type: "integer", nullable: false),
                    price_variance_tolerance_percent = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    allows_amortized_cost = table.Column<bool>(type: "boolean", nullable: false),
                    prepared_by_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    prepared_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    submitted_by_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    submitted_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    checked_by_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    checked_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    approved_by_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    approved_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    published_by_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    published_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    published_version = table.Column<int>(type: "integer", nullable: true),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    last_modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_valuation_runs", x => x.id);
                    table.ForeignKey(
                        name: "fk_valuation_runs_scheme_classes_scheme_class_id",
                        column: x => x.scheme_class_id,
                        principalSchema: "schemes",
                        principalTable: "scheme_classes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_valuation_runs_schemes_scheme_id",
                        column: x => x.scheme_id,
                        principalSchema: "schemes",
                        principalTable: "schemes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "instrument_valuations",
                schema: "nav",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    valuation_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    instrument_id = table.Column<Guid>(type: "uuid", nullable: false),
                    instrument_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    market_price = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: true),
                    accrued_income = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    daily_accretion = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    amortized_cost = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: true),
                    use_amortized_cost = table.Column<bool>(type: "boolean", nullable: false),
                    investment_value = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    price_date = table.Column<DateOnly>(type: "date", nullable: true),
                    price_source = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    is_price_missing = table.Column<bool>(type: "boolean", nullable: false),
                    is_price_stale = table.Column<bool>(type: "boolean", nullable: false),
                    override_applied = table.Column<bool>(type: "boolean", nullable: false),
                    manual_valuation_override_id = table.Column<Guid>(type: "uuid", nullable: true),
                    formula_code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_instrument_valuations", x => x.id);
                    table.ForeignKey(
                        name: "fk_instrument_valuations_instruments_instrument_id",
                        column: x => x.instrument_id,
                        principalSchema: "portfolio",
                        principalTable: "instruments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_instrument_valuations_manual_valuation_overrides_manual_val",
                        column: x => x.manual_valuation_override_id,
                        principalSchema: "nav",
                        principalTable: "manual_valuation_overrides",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_instrument_valuations_valuation_runs_valuation_run_id",
                        column: x => x.valuation_run_id,
                        principalSchema: "nav",
                        principalTable: "valuation_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "nav_approvals",
                schema: "nav",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    valuation_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    step = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    actor_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    occurred_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    comment = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_nav_approvals", x => x.id);
                    table.ForeignKey(
                        name: "fk_nav_approvals_valuation_runs_valuation_run_id",
                        column: x => x.valuation_run_id,
                        principalSchema: "nav",
                        principalTable: "valuation_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "nav_calculations",
                schema: "nav",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    valuation_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    formula_version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    investment_value = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    instrument_accrued_income = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    daily_accretion = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    cash_and_bank = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    other_receivables = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    prepayments = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    gross_asset_value = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    total_liabilities = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    accrued_expenses = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    net_asset_value = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    net_subscription_amount = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    units_allocated = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    gross_redemption_value = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    net_redemption_payable = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    switch_out_value = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    switch_in_units = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    redeemable_amount = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    calculated_by_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    calculated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_nav_calculations", x => x.id);
                    table.ForeignKey(
                        name: "fk_nav_calculations_valuation_runs_valuation_run_id",
                        column: x => x.valuation_run_id,
                        principalSchema: "nav",
                        principalTable: "valuation_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "nav_per_units",
                schema: "nav",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    valuation_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheme_class_id = table.Column<Guid>(type: "uuid", nullable: false),
                    opening_units = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    units_issued = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    units_redeemed = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    approved_unit_adjustments = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    closing_units = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    nav_amount = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    reported_unit_price = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_nav_per_units", x => x.id);
                    table.ForeignKey(
                        name: "fk_nav_per_units_scheme_classes_scheme_class_id",
                        column: x => x.scheme_class_id,
                        principalSchema: "schemes",
                        principalTable: "scheme_classes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_nav_per_units_valuation_runs_valuation_run_id",
                        column: x => x.valuation_run_id,
                        principalSchema: "nav",
                        principalTable: "valuation_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "nav_publications",
                schema: "nav",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    valuation_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheme_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheme_class_id = table.Column<Guid>(type: "uuid", nullable: false),
                    valuation_date = table.Column<DateOnly>(type: "date", nullable: false),
                    version_number = table.Column<int>(type: "integer", nullable: false),
                    published_nav = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    published_unit_price = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    reported_unit_price = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    formula_version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    published_by_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    published_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    last_modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_nav_publications", x => x.id);
                    table.ForeignKey(
                        name: "fk_nav_publications_scheme_classes_scheme_class_id",
                        column: x => x.scheme_class_id,
                        principalSchema: "schemes",
                        principalTable: "scheme_classes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_nav_publications_schemes_scheme_id",
                        column: x => x.scheme_id,
                        principalSchema: "schemes",
                        principalTable: "schemes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_nav_publications_valuation_runs_valuation_run_id",
                        column: x => x.valuation_run_id,
                        principalSchema: "nav",
                        principalTable: "valuation_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "pricing_variance_exceptions",
                schema: "nav",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    valuation_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    instrument_id = table.Column<Guid>(type: "uuid", nullable: false),
                    current_price = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    prior_price = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    variance_percent = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    tolerance_percent = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    message = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pricing_variance_exceptions", x => x.id);
                    table.ForeignKey(
                        name: "fk_pricing_variance_exceptions_instruments_instrument_id",
                        column: x => x.instrument_id,
                        principalSchema: "portfolio",
                        principalTable: "instruments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_pricing_variance_exceptions_valuation_runs_valuation_run_id",
                        column: x => x.valuation_run_id,
                        principalSchema: "nav",
                        principalTable: "valuation_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "stale_price_exceptions",
                schema: "nav",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    valuation_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    instrument_id = table.Column<Guid>(type: "uuid", nullable: false),
                    price_date = table.Column<DateOnly>(type: "date", nullable: true),
                    valuation_date = table.Column<DateOnly>(type: "date", nullable: false),
                    max_age_days = table.Column<int>(type: "integer", nullable: false),
                    missing_price = table.Column<bool>(type: "boolean", nullable: false),
                    message = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stale_price_exceptions", x => x.id);
                    table.ForeignKey(
                        name: "fk_stale_price_exceptions_instruments_instrument_id",
                        column: x => x.instrument_id,
                        principalSchema: "portfolio",
                        principalTable: "instruments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_stale_price_exceptions_valuation_runs_valuation_run_id",
                        column: x => x.valuation_run_id,
                        principalSchema: "nav",
                        principalTable: "valuation_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "valuation_inputs",
                schema: "nav",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    valuation_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    input_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: true),
                    price = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: true),
                    formula_code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    source_reference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_valuation_inputs", x => x.id);
                    table.ForeignKey(
                        name: "fk_valuation_inputs_valuation_runs_valuation_run_id",
                        column: x => x.valuation_run_id,
                        principalSchema: "nav",
                        principalTable: "valuation_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "valuation_sources",
                schema: "nav",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    valuation_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    provider_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    source_reference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    received_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_approved = table.Column<bool>(type: "boolean", nullable: false),
                    is_override = table.Column<bool>(type: "boolean", nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_valuation_sources", x => x.id);
                    table.ForeignKey(
                        name: "fk_valuation_sources_valuation_runs_valuation_run_id",
                        column: x => x.valuation_run_id,
                        principalSchema: "nav",
                        principalTable: "valuation_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "nav_restatements",
                schema: "nav",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    original_publication_id = table.Column<Guid>(type: "uuid", nullable: false),
                    corrected_valuation_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    corrected_publication_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheme_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheme_class_id = table.Column<Guid>(type: "uuid", nullable: false),
                    valuation_date = table.Column<DateOnly>(type: "date", nullable: false),
                    corrected_version_number = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("pk_nav_restatements", x => x.id);
                    table.ForeignKey(
                        name: "fk_nav_restatements_nav_publications_corrected_publication_id",
                        column: x => x.corrected_publication_id,
                        principalSchema: "nav",
                        principalTable: "nav_publications",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_nav_restatements_nav_publications_original_publication_id",
                        column: x => x.original_publication_id,
                        principalSchema: "nav",
                        principalTable: "nav_publications",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_nav_restatements_scheme_classes_scheme_class_id",
                        column: x => x.scheme_class_id,
                        principalSchema: "schemes",
                        principalTable: "scheme_classes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_nav_restatements_schemes_scheme_id",
                        column: x => x.scheme_id,
                        principalSchema: "schemes",
                        principalTable: "schemes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_nav_restatements_valuation_runs_corrected_valuation_run_id",
                        column: x => x.corrected_valuation_run_id,
                        principalSchema: "nav",
                        principalTable: "valuation_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "nav_version_archives",
                schema: "nav",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    valuation_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nav_publication_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version_number = table.Column<int>(type: "integer", nullable: false),
                    payload_json = table.Column<string>(type: "jsonb", nullable: false),
                    payload_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    archived_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    last_modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_nav_version_archives", x => x.id);
                    table.ForeignKey(
                        name: "fk_nav_version_archives_nav_publications_nav_publication_id",
                        column: x => x.nav_publication_id,
                        principalSchema: "nav",
                        principalTable: "nav_publications",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_nav_version_archives_valuation_runs_valuation_run_id",
                        column: x => x.valuation_run_id,
                        principalSchema: "nav",
                        principalTable: "valuation_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_instrument_valuations_instrument_id",
                schema: "nav",
                table: "instrument_valuations",
                column: "instrument_id");

            migrationBuilder.CreateIndex(
                name: "ix_instrument_valuations_is_price_missing_is_price_stale",
                schema: "nav",
                table: "instrument_valuations",
                columns: new[] { "is_price_missing", "is_price_stale" });

            migrationBuilder.CreateIndex(
                name: "ix_instrument_valuations_manual_valuation_override_id",
                schema: "nav",
                table: "instrument_valuations",
                column: "manual_valuation_override_id");

            migrationBuilder.CreateIndex(
                name: "ix_instrument_valuations_valuation_run_id_instrument_id",
                schema: "nav",
                table: "instrument_valuations",
                columns: new[] { "valuation_run_id", "instrument_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_manual_valuation_overrides_instrument_id",
                schema: "nav",
                table: "manual_valuation_overrides",
                column: "instrument_id");

            migrationBuilder.CreateIndex(
                name: "ix_manual_valuation_overrides_requested_by_user_id",
                schema: "nav",
                table: "manual_valuation_overrides",
                column: "requested_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_manual_valuation_overrides_scheme_class_id",
                schema: "nav",
                table: "manual_valuation_overrides",
                column: "scheme_class_id");

            migrationBuilder.CreateIndex(
                name: "ix_manual_valuation_overrides_scheme_id_scheme_class_id_valuat",
                schema: "nav",
                table: "manual_valuation_overrides",
                columns: new[] { "scheme_id", "scheme_class_id", "valuation_date", "instrument_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_nav_approvals_actor_user_id",
                schema: "nav",
                table: "nav_approvals",
                column: "actor_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_nav_approvals_valuation_run_id_step",
                schema: "nav",
                table: "nav_approvals",
                columns: new[] { "valuation_run_id", "step" });

            migrationBuilder.CreateIndex(
                name: "ix_nav_calculations_valuation_run_id_calculated_at_utc",
                schema: "nav",
                table: "nav_calculations",
                columns: new[] { "valuation_run_id", "calculated_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_nav_per_units_scheme_class_id",
                schema: "nav",
                table: "nav_per_units",
                column: "scheme_class_id");

            migrationBuilder.CreateIndex(
                name: "ix_nav_per_units_valuation_run_id_scheme_class_id",
                schema: "nav",
                table: "nav_per_units",
                columns: new[] { "valuation_run_id", "scheme_class_id" });

            migrationBuilder.CreateIndex(
                name: "ix_nav_publications_published_at_utc",
                schema: "nav",
                table: "nav_publications",
                column: "published_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_nav_publications_scheme_class_id",
                schema: "nav",
                table: "nav_publications",
                column: "scheme_class_id");

            migrationBuilder.CreateIndex(
                name: "ix_nav_publications_scheme_id_scheme_class_id_valuation_date_v",
                schema: "nav",
                table: "nav_publications",
                columns: new[] { "scheme_id", "scheme_class_id", "valuation_date", "version_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_nav_publications_valuation_run_id",
                schema: "nav",
                table: "nav_publications",
                column: "valuation_run_id");

            migrationBuilder.CreateIndex(
                name: "ix_nav_restatements_corrected_publication_id",
                schema: "nav",
                table: "nav_restatements",
                column: "corrected_publication_id");

            migrationBuilder.CreateIndex(
                name: "ix_nav_restatements_corrected_valuation_run_id",
                schema: "nav",
                table: "nav_restatements",
                column: "corrected_valuation_run_id");

            migrationBuilder.CreateIndex(
                name: "ix_nav_restatements_original_publication_id",
                schema: "nav",
                table: "nav_restatements",
                column: "original_publication_id");

            migrationBuilder.CreateIndex(
                name: "ix_nav_restatements_scheme_class_id",
                schema: "nav",
                table: "nav_restatements",
                column: "scheme_class_id");

            migrationBuilder.CreateIndex(
                name: "ix_nav_restatements_scheme_id_scheme_class_id_valuation_date_c",
                schema: "nav",
                table: "nav_restatements",
                columns: new[] { "scheme_id", "scheme_class_id", "valuation_date", "corrected_version_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_nav_version_archives_nav_publication_id_version_number",
                schema: "nav",
                table: "nav_version_archives",
                columns: new[] { "nav_publication_id", "version_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_nav_version_archives_payload_hash",
                schema: "nav",
                table: "nav_version_archives",
                column: "payload_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_nav_version_archives_valuation_run_id",
                schema: "nav",
                table: "nav_version_archives",
                column: "valuation_run_id");

            migrationBuilder.CreateIndex(
                name: "ix_price_source_hierarchies_scheme_class_id",
                schema: "nav",
                table: "price_source_hierarchies",
                column: "scheme_class_id");

            migrationBuilder.CreateIndex(
                name: "ix_price_source_hierarchies_scheme_id_scheme_class_id_instrume",
                schema: "nav",
                table: "price_source_hierarchies",
                columns: new[] { "scheme_id", "scheme_class_id", "instrument_type", "is_active" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_pricing_variance_exceptions_instrument_id",
                schema: "nav",
                table: "pricing_variance_exceptions",
                column: "instrument_id");

            migrationBuilder.CreateIndex(
                name: "ix_pricing_variance_exceptions_valuation_run_id_status",
                schema: "nav",
                table: "pricing_variance_exceptions",
                columns: new[] { "valuation_run_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_stale_price_exceptions_instrument_id",
                schema: "nav",
                table: "stale_price_exceptions",
                column: "instrument_id");

            migrationBuilder.CreateIndex(
                name: "ix_stale_price_exceptions_valuation_run_id_status",
                schema: "nav",
                table: "stale_price_exceptions",
                columns: new[] { "valuation_run_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_valuation_inputs_formula_code",
                schema: "nav",
                table: "valuation_inputs",
                column: "formula_code");

            migrationBuilder.CreateIndex(
                name: "ix_valuation_inputs_valuation_run_id_input_type",
                schema: "nav",
                table: "valuation_inputs",
                columns: new[] { "valuation_run_id", "input_type" });

            migrationBuilder.CreateIndex(
                name: "ix_valuation_runs_run_number",
                schema: "nav",
                table: "valuation_runs",
                column: "run_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_valuation_runs_scheme_class_id",
                schema: "nav",
                table: "valuation_runs",
                column: "scheme_class_id");

            migrationBuilder.CreateIndex(
                name: "ix_valuation_runs_scheme_id_scheme_class_id_valuation_date",
                schema: "nav",
                table: "valuation_runs",
                columns: new[] { "scheme_id", "scheme_class_id", "valuation_date" });

            migrationBuilder.CreateIndex(
                name: "ix_valuation_runs_status",
                schema: "nav",
                table: "valuation_runs",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_valuation_sources_received_at_utc",
                schema: "nav",
                table: "valuation_sources",
                column: "received_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_valuation_sources_valuation_run_id_source_type",
                schema: "nav",
                table: "valuation_sources",
                columns: new[] { "valuation_run_id", "source_type" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "instrument_valuations",
                schema: "nav");

            migrationBuilder.DropTable(
                name: "nav_approvals",
                schema: "nav");

            migrationBuilder.DropTable(
                name: "nav_calculations",
                schema: "nav");

            migrationBuilder.DropTable(
                name: "nav_per_units",
                schema: "nav");

            migrationBuilder.DropTable(
                name: "nav_restatements",
                schema: "nav");

            migrationBuilder.DropTable(
                name: "nav_version_archives",
                schema: "nav");

            migrationBuilder.DropTable(
                name: "price_source_hierarchies",
                schema: "nav");

            migrationBuilder.DropTable(
                name: "pricing_variance_exceptions",
                schema: "nav");

            migrationBuilder.DropTable(
                name: "stale_price_exceptions",
                schema: "nav");

            migrationBuilder.DropTable(
                name: "valuation_inputs",
                schema: "nav");

            migrationBuilder.DropTable(
                name: "valuation_sources",
                schema: "nav");

            migrationBuilder.DropTable(
                name: "manual_valuation_overrides",
                schema: "nav");

            migrationBuilder.DropTable(
                name: "nav_publications",
                schema: "nav");

            migrationBuilder.DropTable(
                name: "valuation_runs",
                schema: "nav");
        }
    }
}

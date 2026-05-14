using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cis.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddComplianceRiskLiquidityModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "compliance_risk");

            migrationBuilder.CreateTable(
                name: "internal_policy_limits",
                schema: "compliance_risk",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheme_id = table.Column<Guid>(type: "uuid", nullable: true),
                    scheme_class_id = table.Column<Guid>(type: "uuid", nullable: true),
                    limit_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    reference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    limit_value = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    effective_date = table.Column<DateOnly>(type: "date", nullable: false),
                    created_by_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    internal_policy_limit_created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    last_modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_internal_policy_limits", x => x.id);
                    table.ForeignKey(
                        name: "fk_internal_policy_limits_scheme_classes_scheme_class_id",
                        column: x => x.scheme_class_id,
                        principalSchema: "schemes",
                        principalTable: "scheme_classes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_internal_policy_limits_schemes_scheme_id",
                        column: x => x.scheme_id,
                        principalSchema: "schemes",
                        principalTable: "schemes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "limit_check_runs",
                schema: "compliance_risk",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheme_id = table.Column<Guid>(type: "uuid", nullable: true),
                    scheme_class_id = table.Column<Guid>(type: "uuid", nullable: true),
                    business_date = table.Column<DateOnly>(type: "date", nullable: false),
                    run_number = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    formula_version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    assets_under_management = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    net_flow = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    average_nav = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    expense_to_aum_ratio = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    annualized_yield = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    source_data_json = table.Column<string>(type: "jsonb", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    run_by_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    run_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    last_modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_limit_check_runs", x => x.id);
                    table.ForeignKey(
                        name: "fk_limit_check_runs_scheme_classes_scheme_class_id",
                        column: x => x.scheme_class_id,
                        principalSchema: "schemes",
                        principalTable: "scheme_classes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_limit_check_runs_schemes_scheme_id",
                        column: x => x.scheme_id,
                        principalSchema: "schemes",
                        principalTable: "schemes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "liquidation_time_analysis_runs",
                schema: "compliance_risk",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheme_id = table.Column<Guid>(type: "uuid", nullable: true),
                    scheme_class_id = table.Column<Guid>(type: "uuid", nullable: true),
                    business_date = table.Column<DateOnly>(type: "date", nullable: false),
                    formula_version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    weighted_average_maturity_days = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    assumptions_json = table.Column<string>(type: "jsonb", nullable: false),
                    source_data_json = table.Column<string>(type: "jsonb", nullable: false),
                    run_by_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    run_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    last_modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_liquidation_time_analysis_runs", x => x.id);
                    table.ForeignKey(
                        name: "fk_liquidation_time_analysis_runs_scheme_classes_scheme_class_",
                        column: x => x.scheme_class_id,
                        principalSchema: "schemes",
                        principalTable: "scheme_classes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_liquidation_time_analysis_runs_schemes_scheme_id",
                        column: x => x.scheme_id,
                        principalSchema: "schemes",
                        principalTable: "schemes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "liquidity_coverage_runs",
                schema: "compliance_risk",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheme_id = table.Column<Guid>(type: "uuid", nullable: true),
                    scheme_class_id = table.Column<Guid>(type: "uuid", nullable: true),
                    business_date = table.Column<DateOnly>(type: "date", nullable: false),
                    formula_version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    available_liquid_assets = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    projected_short_term_redemptions = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    liquidity_coverage_ratio = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    source_data_json = table.Column<string>(type: "jsonb", nullable: false),
                    run_by_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    run_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    last_modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_liquidity_coverage_runs", x => x.id);
                    table.ForeignKey(
                        name: "fk_liquidity_coverage_runs_scheme_classes_scheme_class_id",
                        column: x => x.scheme_class_id,
                        principalSchema: "schemes",
                        principalTable: "scheme_classes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_liquidity_coverage_runs_schemes_scheme_id",
                        column: x => x.scheme_id,
                        principalSchema: "schemes",
                        principalTable: "schemes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "redemption_stress_scenarios",
                schema: "compliance_risk",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheme_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    stress_redemption_amount = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    assumptions_json = table.Column<string>(type: "jsonb", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_by_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    redemption_stress_scenario_created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    last_modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_redemption_stress_scenarios", x => x.id);
                    table.ForeignKey(
                        name: "fk_redemption_stress_scenarios_schemes_scheme_id",
                        column: x => x.scheme_id,
                        principalSchema: "schemes",
                        principalTable: "schemes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "risk_dashboard_snapshots",
                schema: "compliance_risk",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_date = table.Column<DateOnly>(type: "date", nullable: false),
                    assets_under_management = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    net_flow = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    average_nav = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    expense_to_aum_ratio = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    latest_liquidity_coverage_ratio = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    latest_stress_coverage = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    open_breaches = table.Column<int>(type: "integer", nullable: false),
                    critical_breaches = table.Column<int>(type: "integer", nullable: false),
                    related_party_exposure_percent = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
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
                    table.PrimaryKey("pk_risk_dashboard_snapshots", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "statutory_limits",
                schema: "compliance_risk",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheme_id = table.Column<Guid>(type: "uuid", nullable: true),
                    scheme_class_id = table.Column<Guid>(type: "uuid", nullable: true),
                    limit_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    reference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    limit_value = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    effective_date = table.Column<DateOnly>(type: "date", nullable: false),
                    created_by_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    statutory_limit_created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    last_modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_statutory_limits", x => x.id);
                    table.ForeignKey(
                        name: "fk_statutory_limits_scheme_classes_scheme_class_id",
                        column: x => x.scheme_class_id,
                        principalSchema: "schemes",
                        principalTable: "scheme_classes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_statutory_limits_schemes_scheme_id",
                        column: x => x.scheme_id,
                        principalSchema: "schemes",
                        principalTable: "schemes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "counterparty_limit_usages",
                schema: "compliance_risk",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    limit_check_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    counterparty_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    exposure_amount = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    limit_amount = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    usage_percent = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    source_reference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_counterparty_limit_usages", x => x.id);
                    table.ForeignKey(
                        name: "fk_counterparty_limit_usages_limit_check_runs_limit_check_run_",
                        column: x => x.limit_check_run_id,
                        principalSchema: "compliance_risk",
                        principalTable: "limit_check_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "related_party_exposures",
                schema: "compliance_risk",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    limit_check_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    related_party_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    exposure_amount = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    exposure_percent_of_aum = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    source_reference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_related_party_exposures", x => x.id);
                    table.ForeignKey(
                        name: "fk_related_party_exposures_limit_check_runs_limit_check_run_id",
                        column: x => x.limit_check_run_id,
                        principalSchema: "compliance_risk",
                        principalTable: "limit_check_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "redemption_stress_test_runs",
                schema: "compliance_risk",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    scenario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheme_id = table.Column<Guid>(type: "uuid", nullable: true),
                    business_date = table.Column<DateOnly>(type: "date", nullable: false),
                    formula_version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    available_liquid_assets = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    stress_redemption_amount = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    stress_coverage = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    source_data_json = table.Column<string>(type: "jsonb", nullable: false),
                    run_by_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    run_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    last_modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_redemption_stress_test_runs", x => x.id);
                    table.ForeignKey(
                        name: "fk_redemption_stress_test_runs_redemption_stress_scenarios_sce",
                        column: x => x.scenario_id,
                        principalSchema: "compliance_risk",
                        principalTable: "redemption_stress_scenarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_redemption_stress_test_runs_schemes_scheme_id",
                        column: x => x.scheme_id,
                        principalSchema: "schemes",
                        principalTable: "schemes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "limit_breaches",
                schema: "compliance_risk",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    limit_check_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    statutory_limit_id = table.Column<Guid>(type: "uuid", nullable: true),
                    internal_policy_limit_id = table.Column<Guid>(type: "uuid", nullable: true),
                    rule_scope = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    limit_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    reference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    limit_value = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    actual_value = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    severity = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    owner_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    assignment_comment = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    remediation_plan = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    closure_evidence_reference = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_by_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    limit_breach_created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    remediated_by_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    remediated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("pk_limit_breaches", x => x.id);
                    table.ForeignKey(
                        name: "fk_limit_breaches_internal_policy_limits_internal_policy_limit",
                        column: x => x.internal_policy_limit_id,
                        principalSchema: "compliance_risk",
                        principalTable: "internal_policy_limits",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_limit_breaches_limit_check_runs_limit_check_run_id",
                        column: x => x.limit_check_run_id,
                        principalSchema: "compliance_risk",
                        principalTable: "limit_check_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_limit_breaches_statutory_limits_statutory_limit_id",
                        column: x => x.statutory_limit_id,
                        principalSchema: "compliance_risk",
                        principalTable: "statutory_limits",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "breach_exception_register",
                schema: "compliance_risk",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    limit_breach_id = table.Column<Guid>(type: "uuid", nullable: false),
                    exception_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    evidence_reference = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    registered_by_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    registered_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    last_modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_breach_exception_register", x => x.id);
                    table.ForeignKey(
                        name: "fk_breach_exception_register_limit_breaches_limit_breach_id",
                        column: x => x.limit_breach_id,
                        principalSchema: "compliance_risk",
                        principalTable: "limit_breaches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "remediation_actions",
                schema: "compliance_risk",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    limit_breach_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action_description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    evidence_reference = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    owner_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    submitted_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    last_modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_remediation_actions", x => x.id);
                    table.ForeignKey(
                        name: "fk_remediation_actions_limit_breaches_limit_breach_id",
                        column: x => x.limit_breach_id,
                        principalSchema: "compliance_risk",
                        principalTable: "limit_breaches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_breach_exception_register_limit_breach_id",
                schema: "compliance_risk",
                table: "breach_exception_register",
                column: "limit_breach_id");

            migrationBuilder.CreateIndex(
                name: "ix_counterparty_limit_usages_limit_check_run_id_counterparty_n",
                schema: "compliance_risk",
                table: "counterparty_limit_usages",
                columns: new[] { "limit_check_run_id", "counterparty_name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_internal_policy_limits_scheme_class_id",
                schema: "compliance_risk",
                table: "internal_policy_limits",
                column: "scheme_class_id");

            migrationBuilder.CreateIndex(
                name: "ix_internal_policy_limits_scheme_id_scheme_class_id_limit_type",
                schema: "compliance_risk",
                table: "internal_policy_limits",
                columns: new[] { "scheme_id", "scheme_class_id", "limit_type", "reference", "effective_date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_limit_breaches_internal_policy_limit_id",
                schema: "compliance_risk",
                table: "limit_breaches",
                column: "internal_policy_limit_id");

            migrationBuilder.CreateIndex(
                name: "ix_limit_breaches_limit_check_run_id_rule_scope_limit_type_ref",
                schema: "compliance_risk",
                table: "limit_breaches",
                columns: new[] { "limit_check_run_id", "rule_scope", "limit_type", "reference" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_limit_breaches_status_severity",
                schema: "compliance_risk",
                table: "limit_breaches",
                columns: new[] { "status", "severity" });

            migrationBuilder.CreateIndex(
                name: "ix_limit_breaches_statutory_limit_id",
                schema: "compliance_risk",
                table: "limit_breaches",
                column: "statutory_limit_id");

            migrationBuilder.CreateIndex(
                name: "ix_limit_check_runs_run_number",
                schema: "compliance_risk",
                table: "limit_check_runs",
                column: "run_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_limit_check_runs_scheme_class_id",
                schema: "compliance_risk",
                table: "limit_check_runs",
                column: "scheme_class_id");

            migrationBuilder.CreateIndex(
                name: "ix_limit_check_runs_scheme_id_scheme_class_id_business_date",
                schema: "compliance_risk",
                table: "limit_check_runs",
                columns: new[] { "scheme_id", "scheme_class_id", "business_date" });

            migrationBuilder.CreateIndex(
                name: "ix_liquidation_time_analysis_runs_scheme_class_id",
                schema: "compliance_risk",
                table: "liquidation_time_analysis_runs",
                column: "scheme_class_id");

            migrationBuilder.CreateIndex(
                name: "ix_liquidation_time_analysis_runs_scheme_id_scheme_class_id_bu",
                schema: "compliance_risk",
                table: "liquidation_time_analysis_runs",
                columns: new[] { "scheme_id", "scheme_class_id", "business_date" });

            migrationBuilder.CreateIndex(
                name: "ix_liquidity_coverage_runs_scheme_class_id",
                schema: "compliance_risk",
                table: "liquidity_coverage_runs",
                column: "scheme_class_id");

            migrationBuilder.CreateIndex(
                name: "ix_liquidity_coverage_runs_scheme_id_scheme_class_id_business_",
                schema: "compliance_risk",
                table: "liquidity_coverage_runs",
                columns: new[] { "scheme_id", "scheme_class_id", "business_date" });

            migrationBuilder.CreateIndex(
                name: "ix_redemption_stress_scenarios_scheme_id_name",
                schema: "compliance_risk",
                table: "redemption_stress_scenarios",
                columns: new[] { "scheme_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_redemption_stress_test_runs_scenario_id_business_date",
                schema: "compliance_risk",
                table: "redemption_stress_test_runs",
                columns: new[] { "scenario_id", "business_date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_redemption_stress_test_runs_scheme_id",
                schema: "compliance_risk",
                table: "redemption_stress_test_runs",
                column: "scheme_id");

            migrationBuilder.CreateIndex(
                name: "ix_related_party_exposures_limit_check_run_id_related_party_na",
                schema: "compliance_risk",
                table: "related_party_exposures",
                columns: new[] { "limit_check_run_id", "related_party_name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_remediation_actions_limit_breach_id",
                schema: "compliance_risk",
                table: "remediation_actions",
                column: "limit_breach_id");

            migrationBuilder.CreateIndex(
                name: "ix_risk_dashboard_snapshots_generated_at_utc",
                schema: "compliance_risk",
                table: "risk_dashboard_snapshots",
                column: "generated_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_statutory_limits_scheme_class_id",
                schema: "compliance_risk",
                table: "statutory_limits",
                column: "scheme_class_id");

            migrationBuilder.CreateIndex(
                name: "ix_statutory_limits_scheme_id_scheme_class_id_limit_type_refer",
                schema: "compliance_risk",
                table: "statutory_limits",
                columns: new[] { "scheme_id", "scheme_class_id", "limit_type", "reference", "effective_date" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "breach_exception_register",
                schema: "compliance_risk");

            migrationBuilder.DropTable(
                name: "counterparty_limit_usages",
                schema: "compliance_risk");

            migrationBuilder.DropTable(
                name: "liquidation_time_analysis_runs",
                schema: "compliance_risk");

            migrationBuilder.DropTable(
                name: "liquidity_coverage_runs",
                schema: "compliance_risk");

            migrationBuilder.DropTable(
                name: "redemption_stress_test_runs",
                schema: "compliance_risk");

            migrationBuilder.DropTable(
                name: "related_party_exposures",
                schema: "compliance_risk");

            migrationBuilder.DropTable(
                name: "remediation_actions",
                schema: "compliance_risk");

            migrationBuilder.DropTable(
                name: "risk_dashboard_snapshots",
                schema: "compliance_risk");

            migrationBuilder.DropTable(
                name: "redemption_stress_scenarios",
                schema: "compliance_risk");

            migrationBuilder.DropTable(
                name: "limit_breaches",
                schema: "compliance_risk");

            migrationBuilder.DropTable(
                name: "internal_policy_limits",
                schema: "compliance_risk");

            migrationBuilder.DropTable(
                name: "limit_check_runs",
                schema: "compliance_risk");

            migrationBuilder.DropTable(
                name: "statutory_limits",
                schema: "compliance_risk");
        }
    }
}

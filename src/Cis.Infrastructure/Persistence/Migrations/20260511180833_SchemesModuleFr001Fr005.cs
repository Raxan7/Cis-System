using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cis.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SchemesModuleFr001Fr005 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "schemes");

            migrationBuilder.CreateTable(
                name: "schemes",
                schema: "schemes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    legal_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    base_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    version_number = table.Column<int>(type: "integer", nullable: false),
                    pending_effective_date = table.Column<DateOnly>(type: "date", nullable: true),
                    submitted_by_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    submitted_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    checked_by_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    checked_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    approved_by_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    approved_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    last_modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_schemes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "approved_instrument_rules",
                schema: "schemes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheme_id = table.Column<Guid>(type: "uuid", nullable: false),
                    instrument_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    tenor_limit_days = table.Column<int>(type: "integer", nullable: false),
                    issuer_limit = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    counterparty_limit = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    asset_class_limit = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_approved_instrument_rules", x => x.id);
                    table.ForeignKey(
                        name: "fk_approved_instrument_rules_schemes_scheme_id",
                        column: x => x.scheme_id,
                        principalSchema: "schemes",
                        principalTable: "schemes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "distribution_rules",
                schema: "schemes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheme_id = table.Column<Guid>(type: "uuid", nullable: false),
                    distribution_frequency = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    reinvestment_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    payment_day = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_distribution_rules", x => x.id);
                    table.ForeignKey(
                        name: "fk_distribution_rules_schemes_scheme_id",
                        column: x => x.scheme_id,
                        principalSchema: "schemes",
                        principalTable: "schemes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "liquidity_thresholds",
                schema: "schemes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheme_id = table.Column<Guid>(type: "uuid", nullable: false),
                    minimum_liquid_asset_ratio = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    warning_threshold = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    breach_threshold = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_liquidity_thresholds", x => x.id);
                    table.ForeignKey(
                        name: "fk_liquidity_thresholds_schemes_scheme_id",
                        column: x => x.scheme_id,
                        principalSchema: "schemes",
                        principalTable: "schemes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "scheme_bank_accounts",
                schema: "schemes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheme_id = table.Column<Guid>(type: "uuid", nullable: false),
                    bank_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    account_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    account_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    swift_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_scheme_bank_accounts", x => x.id);
                    table.ForeignKey(
                        name: "fk_scheme_bank_accounts_schemes_scheme_id",
                        column: x => x.scheme_id,
                        principalSchema: "schemes",
                        principalTable: "schemes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "scheme_classes",
                schema: "schemes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheme_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    valuation_frequency = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    dealing_frequency = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    cut_off_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    minimum_contribution = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    minimum_balance = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    lock_in_days = table.Column<int>(type: "integer", nullable: false),
                    notice_period_days = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_scheme_classes", x => x.id);
                    table.ForeignKey(
                        name: "fk_scheme_classes_schemes_scheme_id",
                        column: x => x.scheme_id,
                        principalSchema: "schemes",
                        principalTable: "schemes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "scheme_configurations",
                schema: "schemes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheme_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nav_pricing_basis = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    income_recognition_basis = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_scheme_configurations", x => x.id);
                    table.ForeignKey(
                        name: "fk_scheme_configurations_schemes_scheme_id",
                        column: x => x.scheme_id,
                        principalSchema: "schemes",
                        principalTable: "schemes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "scheme_custodian_mappings",
                schema: "schemes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheme_id = table.Column<Guid>(type: "uuid", nullable: false),
                    custodian_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    custody_account_reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    settlement_account_reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_scheme_custodian_mappings", x => x.id);
                    table.ForeignKey(
                        name: "fk_scheme_custodian_mappings_schemes_scheme_id",
                        column: x => x.scheme_id,
                        principalSchema: "schemes",
                        principalTable: "schemes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "scheme_eligibility_rules",
                schema: "schemes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheme_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rule_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    rule_expression_json = table.Column<string>(type: "jsonb", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_scheme_eligibility_rules", x => x.id);
                    table.ForeignKey(
                        name: "fk_scheme_eligibility_rules_schemes_scheme_id",
                        column: x => x.scheme_id,
                        principalSchema: "schemes",
                        principalTable: "schemes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "scheme_risk_profiles",
                schema: "schemes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheme_id = table.Column<Guid>(type: "uuid", nullable: false),
                    risk_rating = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    max_single_issuer_exposure = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_scheme_risk_profiles", x => x.id);
                    table.ForeignKey(
                        name: "fk_scheme_risk_profiles_schemes_scheme_id",
                        column: x => x.scheme_id,
                        principalSchema: "schemes",
                        principalTable: "schemes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "scheme_version_history",
                schema: "schemes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheme_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version_number = table.Column<int>(type: "integer", nullable: false),
                    change_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    changed_by_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    changed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    effective_date = table.Column<DateOnly>(type: "date", nullable: true),
                    before_json = table.Column<string>(type: "jsonb", nullable: true),
                    after_json = table.Column<string>(type: "jsonb", nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_scheme_version_history", x => x.id);
                    table.ForeignKey(
                        name: "fk_scheme_version_history_schemes_scheme_id",
                        column: x => x.scheme_id,
                        principalSchema: "schemes",
                        principalTable: "schemes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "template_mappings",
                schema: "schemes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheme_id = table.Column<Guid>(type: "uuid", nullable: false),
                    template_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    template_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_template_mappings", x => x.id);
                    table.ForeignKey(
                        name: "fk_template_mappings_schemes_scheme_id",
                        column: x => x.scheme_id,
                        principalSchema: "schemes",
                        principalTable: "schemes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "fee_schedules",
                schema: "schemes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheme_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheme_class_id = table.Column<Guid>(type: "uuid", nullable: true),
                    fee_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    calculation_basis = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    rate = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: true),
                    fixed_amount = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: true),
                    effective_from = table.Column<DateOnly>(type: "date", nullable: false),
                    effective_to = table.Column<DateOnly>(type: "date", nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fee_schedules", x => x.id);
                    table.ForeignKey(
                        name: "fk_fee_schedules_scheme_classes_scheme_class_id",
                        column: x => x.scheme_class_id,
                        principalSchema: "schemes",
                        principalTable: "scheme_classes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_fee_schedules_schemes_scheme_id",
                        column: x => x.scheme_id,
                        principalSchema: "schemes",
                        principalTable: "schemes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "fee_rules",
                schema: "schemes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    fee_schedule_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_amount = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: true),
                    to_amount = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: true),
                    rate = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: true),
                    fixed_amount = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: true),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fee_rules", x => x.id);
                    table.ForeignKey(
                        name: "fk_fee_rules_fee_schedules_fee_schedule_id",
                        column: x => x.fee_schedule_id,
                        principalSchema: "schemes",
                        principalTable: "fee_schedules",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_approved_instrument_rules_scheme_id_instrument_type_status",
                schema: "schemes",
                table: "approved_instrument_rules",
                columns: new[] { "scheme_id", "instrument_type", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_distribution_rules_scheme_id_is_active",
                schema: "schemes",
                table: "distribution_rules",
                columns: new[] { "scheme_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_fee_rules_fee_schedule_id",
                schema: "schemes",
                table: "fee_rules",
                column: "fee_schedule_id");

            migrationBuilder.CreateIndex(
                name: "ix_fee_schedules_scheme_class_id",
                schema: "schemes",
                table: "fee_schedules",
                column: "scheme_class_id");

            migrationBuilder.CreateIndex(
                name: "ix_fee_schedules_scheme_id_scheme_class_id_fee_type_effective_",
                schema: "schemes",
                table: "fee_schedules",
                columns: new[] { "scheme_id", "scheme_class_id", "fee_type", "effective_from" });

            migrationBuilder.CreateIndex(
                name: "ix_fee_schedules_status",
                schema: "schemes",
                table: "fee_schedules",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_liquidity_thresholds_scheme_id",
                schema: "schemes",
                table: "liquidity_thresholds",
                column: "scheme_id");

            migrationBuilder.CreateIndex(
                name: "ix_scheme_bank_accounts_currency_is_active",
                schema: "schemes",
                table: "scheme_bank_accounts",
                columns: new[] { "currency", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_scheme_bank_accounts_scheme_id_account_number_currency",
                schema: "schemes",
                table: "scheme_bank_accounts",
                columns: new[] { "scheme_id", "account_number", "currency" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_scheme_classes_currency_is_active",
                schema: "schemes",
                table: "scheme_classes",
                columns: new[] { "currency", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_scheme_classes_scheme_id_code",
                schema: "schemes",
                table: "scheme_classes",
                columns: new[] { "scheme_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_scheme_configurations_scheme_id",
                schema: "schemes",
                table: "scheme_configurations",
                column: "scheme_id");

            migrationBuilder.CreateIndex(
                name: "ix_scheme_custodian_mappings_scheme_id_custodian_name_custody_",
                schema: "schemes",
                table: "scheme_custodian_mappings",
                columns: new[] { "scheme_id", "custodian_name", "custody_account_reference" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_scheme_eligibility_rules_scheme_id_rule_type_status",
                schema: "schemes",
                table: "scheme_eligibility_rules",
                columns: new[] { "scheme_id", "rule_type", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_scheme_risk_profiles_scheme_id",
                schema: "schemes",
                table: "scheme_risk_profiles",
                column: "scheme_id");

            migrationBuilder.CreateIndex(
                name: "ix_scheme_version_history_changed_at_utc",
                schema: "schemes",
                table: "scheme_version_history",
                column: "changed_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_scheme_version_history_scheme_id_version_number",
                schema: "schemes",
                table: "scheme_version_history",
                columns: new[] { "scheme_id", "version_number" });

            migrationBuilder.CreateIndex(
                name: "ix_schemes_base_currency",
                schema: "schemes",
                table: "schemes",
                column: "base_currency");

            migrationBuilder.CreateIndex(
                name: "ix_schemes_code",
                schema: "schemes",
                table: "schemes",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_schemes_status",
                schema: "schemes",
                table: "schemes",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_template_mappings_scheme_id_template_type",
                schema: "schemes",
                table: "template_mappings",
                columns: new[] { "scheme_id", "template_type" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "approved_instrument_rules",
                schema: "schemes");

            migrationBuilder.DropTable(
                name: "distribution_rules",
                schema: "schemes");

            migrationBuilder.DropTable(
                name: "fee_rules",
                schema: "schemes");

            migrationBuilder.DropTable(
                name: "liquidity_thresholds",
                schema: "schemes");

            migrationBuilder.DropTable(
                name: "scheme_bank_accounts",
                schema: "schemes");

            migrationBuilder.DropTable(
                name: "scheme_configurations",
                schema: "schemes");

            migrationBuilder.DropTable(
                name: "scheme_custodian_mappings",
                schema: "schemes");

            migrationBuilder.DropTable(
                name: "scheme_eligibility_rules",
                schema: "schemes");

            migrationBuilder.DropTable(
                name: "scheme_risk_profiles",
                schema: "schemes");

            migrationBuilder.DropTable(
                name: "scheme_version_history",
                schema: "schemes");

            migrationBuilder.DropTable(
                name: "template_mappings",
                schema: "schemes");

            migrationBuilder.DropTable(
                name: "fee_schedules",
                schema: "schemes");

            migrationBuilder.DropTable(
                name: "scheme_classes",
                schema: "schemes");

            migrationBuilder.DropTable(
                name: "schemes",
                schema: "schemes");
        }
    }
}

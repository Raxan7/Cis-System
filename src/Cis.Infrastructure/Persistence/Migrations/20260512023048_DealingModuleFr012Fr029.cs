using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cis.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DealingModuleFr012Fr029 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "dealing");

            migrationBuilder.CreateTable(
                name: "approval_thresholds",
                schema: "dealing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    instruction_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    threshold_amount = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_approval_thresholds", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "dealing_batches",
                schema: "dealing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    batch_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    channel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    business_date = table.Column<DateOnly>(type: "date", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    instruction_count = table.Column<int>(type: "integer", nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    last_modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_dealing_batches", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "dealing_instructions",
                schema: "dealing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    instruction_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    instruction_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    investor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheme_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheme_class_id = table.Column<Guid>(type: "uuid", nullable: false),
                    channel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    business_date = table.Column<DateOnly>(type: "date", nullable: false),
                    received_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    submitted_by_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    submitted_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    approved_by_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    approved_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    rejected_by_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    rejected_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cancelled_by_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    cancelled_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    decision_comment = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    last_modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_dealing_instructions", x => x.id);
                    table.ForeignKey(
                        name: "fk_dealing_instructions_investors_investor_id",
                        column: x => x.investor_id,
                        principalSchema: "investors",
                        principalTable: "investors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_dealing_instructions_scheme_classes_scheme_class_id",
                        column: x => x.scheme_class_id,
                        principalSchema: "schemes",
                        principalTable: "scheme_classes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_dealing_instructions_schemes_scheme_id",
                        column: x => x.scheme_id,
                        principalSchema: "schemes",
                        principalTable: "schemes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "liens",
                schema: "dealing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    investor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheme_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheme_class_id = table.Column<Guid>(type: "uuid", nullable: false),
                    units = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: true),
                    amount = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: true),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    documentation_reference = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    approver_evidence_reference = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    placed_by_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    placed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    released_by_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    released_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    release_evidence_reference = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    last_modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_liens", x => x.id);
                    table.ForeignKey(
                        name: "fk_liens_investors_investor_id",
                        column: x => x.investor_id,
                        principalSchema: "investors",
                        principalTable: "investors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_liens_scheme_classes_scheme_class_id",
                        column: x => x.scheme_class_id,
                        principalSchema: "schemes",
                        principalTable: "scheme_classes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_liens_schemes_scheme_id",
                        column: x => x.scheme_id,
                        principalSchema: "schemes",
                        principalTable: "schemes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "recurring_contribution_plans",
                schema: "dealing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    investor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheme_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheme_class_id = table.Column<Guid>(type: "uuid", nullable: false),
                    frequency = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    channel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    effective_from = table.Column<DateOnly>(type: "date", nullable: false),
                    effective_to = table.Column<DateOnly>(type: "date", nullable: true),
                    collection_method = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    missed_collections = table.Column<int>(type: "integer", nullable: false),
                    failed_debits = table.Column<int>(type: "integer", nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    last_modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_recurring_contribution_plans", x => x.id);
                    table.ForeignKey(
                        name: "fk_recurring_contribution_plans_investors_investor_id",
                        column: x => x.investor_id,
                        principalSchema: "investors",
                        principalTable: "investors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_recurring_contribution_plans_scheme_classes_scheme_class_id",
                        column: x => x.scheme_class_id,
                        principalSchema: "schemes",
                        principalTable: "scheme_classes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_recurring_contribution_plans_schemes_scheme_id",
                        column: x => x.scheme_id,
                        principalSchema: "schemes",
                        principalTable: "schemes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "cut_off_breaches",
                schema: "dealing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    dealing_instruction_id = table.Column<Guid>(type: "uuid", nullable: false),
                    received_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    cut_off_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    requires_approval = table.Column<bool>(type: "boolean", nullable: false),
                    approved_by_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    approved_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cut_off_breaches", x => x.id);
                    table.ForeignKey(
                        name: "fk_cut_off_breaches_dealing_instructions_dealing_instruction_id",
                        column: x => x.dealing_instruction_id,
                        principalSchema: "dealing",
                        principalTable: "dealing_instructions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "dealing_validation_results",
                schema: "dealing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    dealing_instruction_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rule_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    severity = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    passed = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_dealing_validation_results", x => x.id);
                    table.ForeignKey(
                        name: "fk_dealing_validation_results_dealing_instructions_dealing_ins",
                        column: x => x.dealing_instruction_id,
                        principalSchema: "dealing",
                        principalTable: "dealing_instructions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "instruction_status_history",
                schema: "dealing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    dealing_instruction_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    changed_by_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    changed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    comment = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_instruction_status_history", x => x.id);
                    table.ForeignKey(
                        name: "fk_instruction_status_history_dealing_instructions_dealing_ins",
                        column: x => x.dealing_instruction_id,
                        principalSchema: "dealing",
                        principalTable: "dealing_instructions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "redemption_instructions",
                schema: "dealing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    dealing_instruction_id = table.Column<Guid>(type: "uuid", nullable: false),
                    mode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: true),
                    units = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: true),
                    full_redemption = table.Column<bool>(type: "boolean", nullable: false),
                    available_units = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    lien_units = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    locked_units = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    minimum_balance_units = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    lock_in_days = table.Column<int>(type: "integer", nullable: false),
                    notice_period_days = table.Column<int>(type: "integer", nullable: false),
                    approved_nav_available = table.Column<bool>(type: "boolean", nullable: false),
                    approved_nav_price = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    approved_nav_date = table.Column<DateOnly>(type: "date", nullable: false),
                    exit_fee_rate = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    tax_rate = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    requested_units = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    gross_amount = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    exit_fee_amount = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    tax_amount = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    net_payout_amount = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    requires_approval_threshold = table.Column<bool>(type: "boolean", nullable: false),
                    payout_authorized = table.Column<bool>(type: "boolean", nullable: false),
                    redemption_advice_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_redemption_instructions", x => x.id);
                    table.ForeignKey(
                        name: "fk_redemption_instructions_dealing_instructions_dealing_instru",
                        column: x => x.dealing_instruction_id,
                        principalSchema: "dealing",
                        principalTable: "dealing_instructions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "subscription_instructions",
                schema: "dealing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    dealing_instruction_id = table.Column<Guid>(type: "uuid", nullable: false),
                    mode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: true),
                    units = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: true),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    funds_cleared = table.Column<bool>(type: "boolean", nullable: false),
                    approved_nav_available = table.Column<bool>(type: "boolean", nullable: false),
                    approved_nav_price = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: true),
                    approved_nav_date = table.Column<DateOnly>(type: "date", nullable: true),
                    allocated_units = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: true),
                    confirmation_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_subscription_instructions", x => x.id);
                    table.ForeignKey(
                        name: "fk_subscription_instructions_dealing_instructions_dealing_inst",
                        column: x => x.dealing_instruction_id,
                        principalSchema: "dealing",
                        principalTable: "dealing_instructions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "switch_instructions",
                schema: "dealing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    dealing_instruction_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_scheme_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_scheme_class_id = table.Column<Guid>(type: "uuid", nullable: false),
                    mode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: true),
                    units = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: true),
                    fee_amount = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    ownership_history_json = table.Column<string>(type: "jsonb", nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_switch_instructions", x => x.id);
                    table.ForeignKey(
                        name: "fk_switch_instructions_dealing_instructions_dealing_instructio",
                        column: x => x.dealing_instruction_id,
                        principalSchema: "dealing",
                        principalTable: "dealing_instructions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_switch_instructions_scheme_classes_target_scheme_class_id",
                        column: x => x.target_scheme_class_id,
                        principalSchema: "schemes",
                        principalTable: "scheme_classes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_switch_instructions_schemes_target_scheme_id",
                        column: x => x.target_scheme_id,
                        principalSchema: "schemes",
                        principalTable: "schemes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "transfer_instructions",
                schema: "dealing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    dealing_instruction_id = table.Column<Guid>(type: "uuid", nullable: false),
                    to_investor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    units = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    ownership_history_json = table.Column<string>(type: "jsonb", nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_transfer_instructions", x => x.id);
                    table.ForeignKey(
                        name: "fk_transfer_instructions_dealing_instructions_dealing_instruct",
                        column: x => x.dealing_instruction_id,
                        principalSchema: "dealing",
                        principalTable: "dealing_instructions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_transfer_instructions_investors_to_investor_id",
                        column: x => x.to_investor_id,
                        principalSchema: "investors",
                        principalTable: "investors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_approval_thresholds_instruction_type_currency_is_active",
                schema: "dealing",
                table: "approval_thresholds",
                columns: new[] { "instruction_type", "currency", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_cut_off_breaches_dealing_instruction_id",
                schema: "dealing",
                table: "cut_off_breaches",
                column: "dealing_instruction_id");

            migrationBuilder.CreateIndex(
                name: "ix_cut_off_breaches_received_at_utc",
                schema: "dealing",
                table: "cut_off_breaches",
                column: "received_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_cut_off_breaches_requires_approval",
                schema: "dealing",
                table: "cut_off_breaches",
                column: "requires_approval");

            migrationBuilder.CreateIndex(
                name: "ix_dealing_batches_batch_number",
                schema: "dealing",
                table: "dealing_batches",
                column: "batch_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_dealing_batches_business_date_channel",
                schema: "dealing",
                table: "dealing_batches",
                columns: new[] { "business_date", "channel" });

            migrationBuilder.CreateIndex(
                name: "ix_dealing_instructions_business_date",
                schema: "dealing",
                table: "dealing_instructions",
                column: "business_date");

            migrationBuilder.CreateIndex(
                name: "ix_dealing_instructions_instruction_number",
                schema: "dealing",
                table: "dealing_instructions",
                column: "instruction_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_dealing_instructions_investor_id_scheme_id_scheme_class_id",
                schema: "dealing",
                table: "dealing_instructions",
                columns: new[] { "investor_id", "scheme_id", "scheme_class_id" });

            migrationBuilder.CreateIndex(
                name: "ix_dealing_instructions_scheme_class_id",
                schema: "dealing",
                table: "dealing_instructions",
                column: "scheme_class_id");

            migrationBuilder.CreateIndex(
                name: "ix_dealing_instructions_scheme_id",
                schema: "dealing",
                table: "dealing_instructions",
                column: "scheme_id");

            migrationBuilder.CreateIndex(
                name: "ix_dealing_instructions_status",
                schema: "dealing",
                table: "dealing_instructions",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_dealing_validation_results_dealing_instruction_id_rule_code",
                schema: "dealing",
                table: "dealing_validation_results",
                columns: new[] { "dealing_instruction_id", "rule_code" });

            migrationBuilder.CreateIndex(
                name: "ix_dealing_validation_results_passed_severity",
                schema: "dealing",
                table: "dealing_validation_results",
                columns: new[] { "passed", "severity" });

            migrationBuilder.CreateIndex(
                name: "ix_instruction_status_history_dealing_instruction_id_changed_a",
                schema: "dealing",
                table: "instruction_status_history",
                columns: new[] { "dealing_instruction_id", "changed_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_liens_investor_id_scheme_id_scheme_class_id_status",
                schema: "dealing",
                table: "liens",
                columns: new[] { "investor_id", "scheme_id", "scheme_class_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_liens_scheme_class_id",
                schema: "dealing",
                table: "liens",
                column: "scheme_class_id");

            migrationBuilder.CreateIndex(
                name: "ix_liens_scheme_id",
                schema: "dealing",
                table: "liens",
                column: "scheme_id");

            migrationBuilder.CreateIndex(
                name: "ix_recurring_contribution_plans_effective_from",
                schema: "dealing",
                table: "recurring_contribution_plans",
                column: "effective_from");

            migrationBuilder.CreateIndex(
                name: "ix_recurring_contribution_plans_investor_id_status",
                schema: "dealing",
                table: "recurring_contribution_plans",
                columns: new[] { "investor_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_recurring_contribution_plans_scheme_class_id",
                schema: "dealing",
                table: "recurring_contribution_plans",
                column: "scheme_class_id");

            migrationBuilder.CreateIndex(
                name: "ix_recurring_contribution_plans_scheme_id",
                schema: "dealing",
                table: "recurring_contribution_plans",
                column: "scheme_id");

            migrationBuilder.CreateIndex(
                name: "ix_redemption_instructions_dealing_instruction_id",
                schema: "dealing",
                table: "redemption_instructions",
                column: "dealing_instruction_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_redemption_instructions_payout_authorized",
                schema: "dealing",
                table: "redemption_instructions",
                column: "payout_authorized");

            migrationBuilder.CreateIndex(
                name: "ix_redemption_instructions_requires_approval_threshold",
                schema: "dealing",
                table: "redemption_instructions",
                column: "requires_approval_threshold");

            migrationBuilder.CreateIndex(
                name: "ix_subscription_instructions_dealing_instruction_id",
                schema: "dealing",
                table: "subscription_instructions",
                column: "dealing_instruction_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_subscription_instructions_funds_cleared_approved_nav_availa",
                schema: "dealing",
                table: "subscription_instructions",
                columns: new[] { "funds_cleared", "approved_nav_available" });

            migrationBuilder.CreateIndex(
                name: "ix_switch_instructions_dealing_instruction_id",
                schema: "dealing",
                table: "switch_instructions",
                column: "dealing_instruction_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_switch_instructions_target_scheme_class_id",
                schema: "dealing",
                table: "switch_instructions",
                column: "target_scheme_class_id");

            migrationBuilder.CreateIndex(
                name: "ix_switch_instructions_target_scheme_id",
                schema: "dealing",
                table: "switch_instructions",
                column: "target_scheme_id");

            migrationBuilder.CreateIndex(
                name: "ix_transfer_instructions_dealing_instruction_id",
                schema: "dealing",
                table: "transfer_instructions",
                column: "dealing_instruction_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_transfer_instructions_to_investor_id",
                schema: "dealing",
                table: "transfer_instructions",
                column: "to_investor_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "approval_thresholds",
                schema: "dealing");

            migrationBuilder.DropTable(
                name: "cut_off_breaches",
                schema: "dealing");

            migrationBuilder.DropTable(
                name: "dealing_batches",
                schema: "dealing");

            migrationBuilder.DropTable(
                name: "dealing_validation_results",
                schema: "dealing");

            migrationBuilder.DropTable(
                name: "instruction_status_history",
                schema: "dealing");

            migrationBuilder.DropTable(
                name: "liens",
                schema: "dealing");

            migrationBuilder.DropTable(
                name: "recurring_contribution_plans",
                schema: "dealing");

            migrationBuilder.DropTable(
                name: "redemption_instructions",
                schema: "dealing");

            migrationBuilder.DropTable(
                name: "subscription_instructions",
                schema: "dealing");

            migrationBuilder.DropTable(
                name: "switch_instructions",
                schema: "dealing");

            migrationBuilder.DropTable(
                name: "transfer_instructions",
                schema: "dealing");

            migrationBuilder.DropTable(
                name: "dealing_instructions",
                schema: "dealing");
        }
    }
}

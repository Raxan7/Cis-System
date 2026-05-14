using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cis.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UnitRegisterModuleFr030Fr033 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "unit_register");

            migrationBuilder.CreateTable(
                name: "investor_positions",
                schema: "unit_register",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    investor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheme_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheme_class_id = table.Column<Guid>(type: "uuid", nullable: false),
                    units = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    liened_units = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    redeemable_units = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    unit_precision = table.Column<int>(type: "integer", nullable: false),
                    as_of_date = table.Column<DateOnly>(type: "date", nullable: true),
                    last_transaction_reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    last_modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_investor_positions", x => x.id);
                    table.ForeignKey(
                        name: "fk_investor_positions_investors_investor_id",
                        column: x => x.investor_id,
                        principalSchema: "investors",
                        principalTable: "investors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_investor_positions_scheme_classes_scheme_class_id",
                        column: x => x.scheme_class_id,
                        principalSchema: "schemes",
                        principalTable: "scheme_classes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_investor_positions_schemes_scheme_id",
                        column: x => x.scheme_id,
                        principalSchema: "schemes",
                        principalTable: "schemes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "unit_adjustments",
                schema: "unit_register",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    investor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheme_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheme_class_id = table.Column<Guid>(type: "uuid", nullable: false),
                    units = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    valuation_date = table.Column<DateOnly>(type: "date", nullable: false),
                    transaction_reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    unit_precision = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("pk_unit_adjustments", x => x.id);
                    table.ForeignKey(
                        name: "fk_unit_adjustments_investors_investor_id",
                        column: x => x.investor_id,
                        principalSchema: "investors",
                        principalTable: "investors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_unit_adjustments_scheme_classes_scheme_class_id",
                        column: x => x.scheme_class_id,
                        principalSchema: "schemes",
                        principalTable: "scheme_classes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_unit_adjustments_schemes_scheme_id",
                        column: x => x.scheme_id,
                        principalSchema: "schemes",
                        principalTable: "schemes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "unit_holdings",
                schema: "unit_register",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    investor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheme_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheme_class_id = table.Column<Guid>(type: "uuid", nullable: false),
                    units = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    liened_units = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    unit_precision = table.Column<int>(type: "integer", nullable: false),
                    last_movement_date = table.Column<DateOnly>(type: "date", nullable: true),
                    last_transaction_reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    last_modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_unit_holdings", x => x.id);
                    table.ForeignKey(
                        name: "fk_unit_holdings_investors_investor_id",
                        column: x => x.investor_id,
                        principalSchema: "investors",
                        principalTable: "investors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_unit_holdings_scheme_classes_scheme_class_id",
                        column: x => x.scheme_class_id,
                        principalSchema: "schemes",
                        principalTable: "scheme_classes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_unit_holdings_schemes_scheme_id",
                        column: x => x.scheme_id,
                        principalSchema: "schemes",
                        principalTable: "schemes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "unit_movement_sources",
                schema: "unit_register",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    source_id = table.Column<Guid>(type: "uuid", nullable: false),
                    investor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheme_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheme_class_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    is_approved = table.Column<bool>(type: "boolean", nullable: false),
                    verified_by_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    verified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    last_modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_unit_movement_sources", x => x.id);
                    table.ForeignKey(
                        name: "fk_unit_movement_sources_investors_investor_id",
                        column: x => x.investor_id,
                        principalSchema: "investors",
                        principalTable: "investors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_unit_movement_sources_scheme_classes_scheme_class_id",
                        column: x => x.scheme_class_id,
                        principalSchema: "schemes",
                        principalTable: "scheme_classes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_unit_movement_sources_schemes_scheme_id",
                        column: x => x.scheme_id,
                        principalSchema: "schemes",
                        principalTable: "schemes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "unit_register_snapshots",
                schema: "unit_register",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheme_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheme_class_id = table.Column<Guid>(type: "uuid", nullable: false),
                    snapshot_date = table.Column<DateOnly>(type: "date", nullable: false),
                    total_units = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    liened_units = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    redeemable_units = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    holding_count = table.Column<int>(type: "integer", nullable: false),
                    last_transaction_reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    last_modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_unit_register_snapshots", x => x.id);
                    table.ForeignKey(
                        name: "fk_unit_register_snapshots_scheme_classes_scheme_class_id",
                        column: x => x.scheme_class_id,
                        principalSchema: "schemes",
                        principalTable: "scheme_classes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_unit_register_snapshots_schemes_scheme_id",
                        column: x => x.scheme_id,
                        principalSchema: "schemes",
                        principalTable: "schemes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "unit_ledger_entries",
                schema: "unit_register",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    movement_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    investor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheme_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheme_class_id = table.Column<Guid>(type: "uuid", nullable: false),
                    valuation_date = table.Column<DateOnly>(type: "date", nullable: false),
                    units = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    source_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    source_entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    adjustment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    transaction_reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    unit_precision = table.Column<int>(type: "integer", nullable: false),
                    posted_by_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    posted_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    narrative = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    last_modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_unit_ledger_entries", x => x.id);
                    table.ForeignKey(
                        name: "fk_unit_ledger_entries_investors_investor_id",
                        column: x => x.investor_id,
                        principalSchema: "investors",
                        principalTable: "investors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_unit_ledger_entries_scheme_classes_scheme_class_id",
                        column: x => x.scheme_class_id,
                        principalSchema: "schemes",
                        principalTable: "scheme_classes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_unit_ledger_entries_schemes_scheme_id",
                        column: x => x.scheme_id,
                        principalSchema: "schemes",
                        principalTable: "schemes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_unit_ledger_entries_unit_adjustments_adjustment_id",
                        column: x => x.adjustment_id,
                        principalSchema: "unit_register",
                        principalTable: "unit_adjustments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_unit_ledger_entries_unit_movement_sources_source_id",
                        column: x => x.source_id,
                        principalSchema: "unit_register",
                        principalTable: "unit_movement_sources",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "historical_holding_views",
                schema: "unit_register",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    investor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheme_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheme_class_id = table.Column<Guid>(type: "uuid", nullable: false),
                    valuation_date = table.Column<DateOnly>(type: "date", nullable: false),
                    transaction_reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    units = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    liened_units = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    redeemable_units = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    unit_precision = table.Column<int>(type: "integer", nullable: false),
                    unit_ledger_entry_id = table.Column<Guid>(type: "uuid", nullable: false),
                    recorded_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    last_modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_historical_holding_views", x => x.id);
                    table.ForeignKey(
                        name: "fk_historical_holding_views_investors_investor_id",
                        column: x => x.investor_id,
                        principalSchema: "investors",
                        principalTable: "investors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_historical_holding_views_scheme_classes_scheme_class_id",
                        column: x => x.scheme_class_id,
                        principalSchema: "schemes",
                        principalTable: "scheme_classes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_historical_holding_views_schemes_scheme_id",
                        column: x => x.scheme_id,
                        principalSchema: "schemes",
                        principalTable: "schemes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_historical_holding_views_unit_ledger_entries_unit_ledger_en",
                        column: x => x.unit_ledger_entry_id,
                        principalSchema: "unit_register",
                        principalTable: "unit_ledger_entries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_historical_holding_views_investor_id_valuation_date",
                schema: "unit_register",
                table: "historical_holding_views",
                columns: new[] { "investor_id", "valuation_date" });

            migrationBuilder.CreateIndex(
                name: "ix_historical_holding_views_scheme_class_id",
                schema: "unit_register",
                table: "historical_holding_views",
                column: "scheme_class_id");

            migrationBuilder.CreateIndex(
                name: "ix_historical_holding_views_scheme_id",
                schema: "unit_register",
                table: "historical_holding_views",
                column: "scheme_id");

            migrationBuilder.CreateIndex(
                name: "ix_historical_holding_views_transaction_reference",
                schema: "unit_register",
                table: "historical_holding_views",
                column: "transaction_reference");

            migrationBuilder.CreateIndex(
                name: "ix_historical_holding_views_unit_ledger_entry_id",
                schema: "unit_register",
                table: "historical_holding_views",
                column: "unit_ledger_entry_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_investor_positions_as_of_date",
                schema: "unit_register",
                table: "investor_positions",
                column: "as_of_date");

            migrationBuilder.CreateIndex(
                name: "ix_investor_positions_investor_id_scheme_id_scheme_class_id",
                schema: "unit_register",
                table: "investor_positions",
                columns: new[] { "investor_id", "scheme_id", "scheme_class_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_investor_positions_scheme_class_id",
                schema: "unit_register",
                table: "investor_positions",
                column: "scheme_class_id");

            migrationBuilder.CreateIndex(
                name: "ix_investor_positions_scheme_id",
                schema: "unit_register",
                table: "investor_positions",
                column: "scheme_id");

            migrationBuilder.CreateIndex(
                name: "ix_unit_adjustments_investor_id_scheme_id_scheme_class_id",
                schema: "unit_register",
                table: "unit_adjustments",
                columns: new[] { "investor_id", "scheme_id", "scheme_class_id" });

            migrationBuilder.CreateIndex(
                name: "ix_unit_adjustments_scheme_class_id",
                schema: "unit_register",
                table: "unit_adjustments",
                column: "scheme_class_id");

            migrationBuilder.CreateIndex(
                name: "ix_unit_adjustments_scheme_id",
                schema: "unit_register",
                table: "unit_adjustments",
                column: "scheme_id");

            migrationBuilder.CreateIndex(
                name: "ix_unit_adjustments_status",
                schema: "unit_register",
                table: "unit_adjustments",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_unit_adjustments_transaction_reference",
                schema: "unit_register",
                table: "unit_adjustments",
                column: "transaction_reference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_unit_holdings_investor_id_scheme_id_scheme_class_id",
                schema: "unit_register",
                table: "unit_holdings",
                columns: new[] { "investor_id", "scheme_id", "scheme_class_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_unit_holdings_scheme_class_id",
                schema: "unit_register",
                table: "unit_holdings",
                column: "scheme_class_id");

            migrationBuilder.CreateIndex(
                name: "ix_unit_holdings_scheme_id_scheme_class_id",
                schema: "unit_register",
                table: "unit_holdings",
                columns: new[] { "scheme_id", "scheme_class_id" });

            migrationBuilder.CreateIndex(
                name: "ix_unit_ledger_entries_adjustment_id",
                schema: "unit_register",
                table: "unit_ledger_entries",
                column: "adjustment_id");

            migrationBuilder.CreateIndex(
                name: "ix_unit_ledger_entries_investor_id_scheme_id_scheme_class_id_v",
                schema: "unit_register",
                table: "unit_ledger_entries",
                columns: new[] { "investor_id", "scheme_id", "scheme_class_id", "valuation_date" });

            migrationBuilder.CreateIndex(
                name: "ix_unit_ledger_entries_posted_at_utc",
                schema: "unit_register",
                table: "unit_ledger_entries",
                column: "posted_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_unit_ledger_entries_scheme_class_id",
                schema: "unit_register",
                table: "unit_ledger_entries",
                column: "scheme_class_id");

            migrationBuilder.CreateIndex(
                name: "ix_unit_ledger_entries_scheme_id",
                schema: "unit_register",
                table: "unit_ledger_entries",
                column: "scheme_id");

            migrationBuilder.CreateIndex(
                name: "ix_unit_ledger_entries_source_id",
                schema: "unit_register",
                table: "unit_ledger_entries",
                column: "source_id");

            migrationBuilder.CreateIndex(
                name: "ix_unit_ledger_entries_source_type_source_entity_id_movement_t",
                schema: "unit_register",
                table: "unit_ledger_entries",
                columns: new[] { "source_type", "source_entity_id", "movement_type" });

            migrationBuilder.CreateIndex(
                name: "ix_unit_ledger_entries_transaction_reference",
                schema: "unit_register",
                table: "unit_ledger_entries",
                column: "transaction_reference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_unit_movement_sources_investor_id_scheme_id_scheme_class_id",
                schema: "unit_register",
                table: "unit_movement_sources",
                columns: new[] { "investor_id", "scheme_id", "scheme_class_id" });

            migrationBuilder.CreateIndex(
                name: "ix_unit_movement_sources_is_approved",
                schema: "unit_register",
                table: "unit_movement_sources",
                column: "is_approved");

            migrationBuilder.CreateIndex(
                name: "ix_unit_movement_sources_scheme_class_id",
                schema: "unit_register",
                table: "unit_movement_sources",
                column: "scheme_class_id");

            migrationBuilder.CreateIndex(
                name: "ix_unit_movement_sources_scheme_id",
                schema: "unit_register",
                table: "unit_movement_sources",
                column: "scheme_id");

            migrationBuilder.CreateIndex(
                name: "ix_unit_movement_sources_source_type_source_id",
                schema: "unit_register",
                table: "unit_movement_sources",
                columns: new[] { "source_type", "source_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_unit_register_snapshots_scheme_class_id",
                schema: "unit_register",
                table: "unit_register_snapshots",
                column: "scheme_class_id");

            migrationBuilder.CreateIndex(
                name: "ix_unit_register_snapshots_scheme_id_scheme_class_id_snapshot_",
                schema: "unit_register",
                table: "unit_register_snapshots",
                columns: new[] { "scheme_id", "scheme_class_id", "snapshot_date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_unit_register_snapshots_snapshot_date",
                schema: "unit_register",
                table: "unit_register_snapshots",
                column: "snapshot_date");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "historical_holding_views",
                schema: "unit_register");

            migrationBuilder.DropTable(
                name: "investor_positions",
                schema: "unit_register");

            migrationBuilder.DropTable(
                name: "unit_holdings",
                schema: "unit_register");

            migrationBuilder.DropTable(
                name: "unit_register_snapshots",
                schema: "unit_register");

            migrationBuilder.DropTable(
                name: "unit_ledger_entries",
                schema: "unit_register");

            migrationBuilder.DropTable(
                name: "unit_adjustments",
                schema: "unit_register");

            migrationBuilder.DropTable(
                name: "unit_movement_sources",
                schema: "unit_register");
        }
    }
}

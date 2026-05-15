using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cis.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PerformanceReadinessImprovements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_report_runs_report_definition_id",
                schema: "reports",
                table: "report_runs");

            migrationBuilder.CreateIndex(
                name: "ix_schemes_name",
                schema: "schemes",
                table: "schemes",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "ix_schemes_status_code",
                schema: "schemes",
                table: "schemes",
                columns: new[] { "status", "code" });

            migrationBuilder.CreateIndex(
                name: "ix_report_runs_report_definition_id_status_generated_at_utc",
                schema: "reports",
                table: "report_runs",
                columns: new[] { "report_definition_id", "status", "generated_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_investors_display_name",
                schema: "investors",
                table: "investors",
                column: "display_name");

            migrationBuilder.CreateIndex(
                name: "ix_investors_status_investor_number",
                schema: "investors",
                table: "investors",
                columns: new[] { "status", "investor_number" });

            migrationBuilder.CreateIndex(
                name: "ix_dealing_instructions_channel_business_date",
                schema: "dealing",
                table: "dealing_instructions",
                columns: new[] { "channel", "business_date" });

            migrationBuilder.CreateIndex(
                name: "ix_dealing_instructions_status_business_date",
                schema: "dealing",
                table: "dealing_instructions",
                columns: new[] { "status", "business_date" });

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_actor_id_occurred_at_utc",
                schema: "audit",
                table: "audit_logs",
                columns: new[] { "actor_id", "occurred_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_entity_name_occurred_at_utc",
                schema: "audit",
                table: "audit_logs",
                columns: new[] { "entity_name", "occurred_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_module_event_type_occurred_at_utc",
                schema: "audit",
                table: "audit_logs",
                columns: new[] { "module", "event_type", "occurred_at_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_schemes_name",
                schema: "schemes",
                table: "schemes");

            migrationBuilder.DropIndex(
                name: "ix_schemes_status_code",
                schema: "schemes",
                table: "schemes");

            migrationBuilder.DropIndex(
                name: "ix_report_runs_report_definition_id_status_generated_at_utc",
                schema: "reports",
                table: "report_runs");

            migrationBuilder.DropIndex(
                name: "ix_investors_display_name",
                schema: "investors",
                table: "investors");

            migrationBuilder.DropIndex(
                name: "ix_investors_status_investor_number",
                schema: "investors",
                table: "investors");

            migrationBuilder.DropIndex(
                name: "ix_dealing_instructions_channel_business_date",
                schema: "dealing",
                table: "dealing_instructions");

            migrationBuilder.DropIndex(
                name: "ix_dealing_instructions_status_business_date",
                schema: "dealing",
                table: "dealing_instructions");

            migrationBuilder.DropIndex(
                name: "ix_audit_logs_actor_id_occurred_at_utc",
                schema: "audit",
                table: "audit_logs");

            migrationBuilder.DropIndex(
                name: "ix_audit_logs_entity_name_occurred_at_utc",
                schema: "audit",
                table: "audit_logs");

            migrationBuilder.DropIndex(
                name: "ix_audit_logs_module_event_type_occurred_at_utc",
                schema: "audit",
                table: "audit_logs");

            migrationBuilder.CreateIndex(
                name: "ix_report_runs_report_definition_id",
                schema: "reports",
                table: "report_runs",
                column: "report_definition_id");
        }
    }
}

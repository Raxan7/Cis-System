using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cis.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(CisDbContext))]
    [Migration("20260512101500_UnitRegisterAdjustmentIdempotency")]
    public partial class UnitRegisterAdjustmentIdempotency : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "idempotency_key",
                schema: "unit_register",
                table: "unit_adjustments",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_unit_adjustments_idempotency_key",
                schema: "unit_register",
                table: "unit_adjustments",
                column: "idempotency_key",
                unique: true,
                filter: "idempotency_key IS NOT NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_unit_adjustments_idempotency_key",
                schema: "unit_register",
                table: "unit_adjustments");

            migrationBuilder.DropColumn(
                name: "idempotency_key",
                schema: "unit_register",
                table: "unit_adjustments");
        }
    }
}

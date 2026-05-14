using Cis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cis.Infrastructure.Persistence.Migrations;

[DbContext(typeof(CisDbContext))]
[Migration("20260514110000_SecurityHardeningPass")]
public sealed class SecurityHardeningPass : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "ix_users_lockout_end_utc",
            schema: "identity",
            table: "users",
            column: "lockout_end_utc");

        migrationBuilder.CreateIndex(
            name: "ix_users_mfa_enabled",
            schema: "identity",
            table: "users",
            column: "mfa_enabled");

        migrationBuilder.CreateIndex(
            name: "ix_users_normalized_email_status",
            schema: "identity",
            table: "users",
            columns: new[] { "normalized_email", "status" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ix_users_lockout_end_utc",
            schema: "identity",
            table: "users");

        migrationBuilder.DropIndex(
            name: "ix_users_mfa_enabled",
            schema: "identity",
            table: "users");

        migrationBuilder.DropIndex(
            name: "ix_users_normalized_email_status",
            schema: "identity",
            table: "users");
    }
}

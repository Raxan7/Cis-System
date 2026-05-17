using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cis.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPortalSelfRegistrationFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "portal_self_registrations",
                schema: "portal",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    normalized_email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    phone_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    normalized_phone_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    otp_code_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    otp_sent_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    otp_expires_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    failed_otp_attempt_count = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    activated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    investor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    last_modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_portal_self_registrations", x => x.id);
                    table.ForeignKey(
                        name: "fk_portal_self_registrations_investors_investor_id",
                        column: x => x.investor_id,
                        principalSchema: "investors",
                        principalTable: "investors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_portal_self_registrations_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_portal_self_registrations_investor_id",
                schema: "portal",
                table: "portal_self_registrations",
                column: "investor_id");

            migrationBuilder.CreateIndex(
                name: "ix_portal_self_registrations_normalized_email",
                schema: "portal",
                table: "portal_self_registrations",
                column: "normalized_email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_portal_self_registrations_normalized_phone_number",
                schema: "portal",
                table: "portal_self_registrations",
                column: "normalized_phone_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_portal_self_registrations_status_otp_expires_at_utc",
                schema: "portal",
                table: "portal_self_registrations",
                columns: new[] { "status", "otp_expires_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_portal_self_registrations_user_id",
                schema: "portal",
                table: "portal_self_registrations",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "portal_self_registrations",
                schema: "portal");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cis.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InvestorsKycAmlDocumentsFr006Fr011 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "aml");

            migrationBuilder.EnsureSchema(
                name: "investors");

            migrationBuilder.EnsureSchema(
                name: "documents");

            migrationBuilder.EnsureSchema(
                name: "kyc");

            migrationBuilder.CreateTable(
                name: "investors",
                schema: "investors",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    investor_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    investor_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    phone_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    risk_category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    submitted_by_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    submitted_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    approved_by_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    approved_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    rejected_by_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    rejected_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    decision_comment = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    last_modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_investors", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "aml_screening_cases",
                schema: "aml",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    investor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    screening_reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    screened_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_aml_screening_cases", x => x.id);
                    table.ForeignKey(
                        name: "fk_aml_screening_cases_investors_investor_id",
                        column: x => x.investor_id,
                        principalSchema: "investors",
                        principalTable: "investors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "beneficial_owners",
                schema: "investors",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    investor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    identity_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ownership_percentage = table.Column<decimal>(type: "numeric(38,12)", precision: 38, scale: 12, nullable: false),
                    is_politically_exposed = table.Column<bool>(type: "boolean", nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_beneficial_owners", x => x.id);
                    table.ForeignKey(
                        name: "fk_beneficial_owners_investors_investor_id",
                        column: x => x.investor_id,
                        principalSchema: "investors",
                        principalTable: "investors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "duplicate_detection_results",
                schema: "investors",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    investor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    match_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    matched_value = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    matched_investor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    matched_investor_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    detected_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_duplicate_detection_results", x => x.id);
                    table.ForeignKey(
                        name: "fk_duplicate_detection_results_investors_investor_id",
                        column: x => x.investor_id,
                        principalSchema: "investors",
                        principalTable: "investors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "investor_bank_accounts",
                schema: "investors",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    investor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    bank_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    account_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    account_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    swift_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    high_risk_flag = table.Column<bool>(type: "boolean", nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_investor_bank_accounts", x => x.id);
                    table.ForeignKey(
                        name: "fk_investor_bank_accounts_investors_investor_id",
                        column: x => x.investor_id,
                        principalSchema: "investors",
                        principalTable: "investors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "investor_change_logs",
                schema: "investors",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    investor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    change_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    before_json = table.Column<string>(type: "jsonb", nullable: false),
                    after_json = table.Column<string>(type: "jsonb", nullable: false),
                    high_risk_flag = table.Column<bool>(type: "boolean", nullable: false),
                    changed_by_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    changed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_investor_change_logs", x => x.id);
                    table.ForeignKey(
                        name: "fk_investor_change_logs_investors_investor_id",
                        column: x => x.investor_id,
                        principalSchema: "investors",
                        principalTable: "investors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "investor_contacts",
                schema: "investors",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    investor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contact_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    value = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_investor_contacts", x => x.id);
                    table.ForeignKey(
                        name: "fk_investor_contacts_investors_investor_id",
                        column: x => x.investor_id,
                        principalSchema: "investors",
                        principalTable: "investors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "investor_mandates",
                schema: "investors",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    investor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    mandate_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    signing_authority = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    effective_from = table.Column<DateOnly>(type: "date", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_investor_mandates", x => x.id);
                    table.ForeignKey(
                        name: "fk_investor_mandates_investors_investor_id",
                        column: x => x.investor_id,
                        principalSchema: "investors",
                        principalTable: "investors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "investor_profile_corporates",
                schema: "investors",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    investor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    registered_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    registration_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    incorporation_date = table.Column<DateOnly>(type: "date", nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_investor_profile_corporates", x => x.id);
                    table.ForeignKey(
                        name: "fk_investor_profile_corporates_investors_investor_id",
                        column: x => x.investor_id,
                        principalSchema: "investors",
                        principalTable: "investors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "investor_profile_groups",
                schema: "investors",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    investor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    group_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    registration_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    contact_person_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_investor_profile_groups", x => x.id);
                    table.ForeignKey(
                        name: "fk_investor_profile_groups_investors_investor_id",
                        column: x => x.investor_id,
                        principalSchema: "investors",
                        principalTable: "investors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "investor_profile_individuals",
                schema: "investors",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    investor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    identity_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    date_of_birth = table.Column<DateOnly>(type: "date", nullable: false),
                    nationality = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_investor_profile_individuals", x => x.id);
                    table.ForeignKey(
                        name: "fk_investor_profile_individuals_investors_investor_id",
                        column: x => x.investor_id,
                        principalSchema: "investors",
                        principalTable: "investors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "investor_profile_joints",
                schema: "investors",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    investor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    joint_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    primary_identity_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    secondary_identity_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_investor_profile_joints", x => x.id);
                    table.ForeignKey(
                        name: "fk_investor_profile_joints_investors_investor_id",
                        column: x => x.investor_id,
                        principalSchema: "investors",
                        principalTable: "investors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "investor_risk_classifications",
                schema: "investors",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    investor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    risk_category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    assigned_by_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    assigned_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    approved_by_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    approved_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_investor_risk_classifications", x => x.id);
                    table.ForeignKey(
                        name: "fk_investor_risk_classifications_investors_investor_id",
                        column: x => x.investor_id,
                        principalSchema: "investors",
                        principalTable: "investors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "investor_tax_profiles",
                schema: "investors",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    investor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tax_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    country_of_tax_residence = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_investor_tax_profiles", x => x.id);
                    table.ForeignKey(
                        name: "fk_investor_tax_profiles_investors_investor_id",
                        column: x => x.investor_id,
                        principalSchema: "investors",
                        principalTable: "investors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "kyc_documents",
                schema: "documents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    investor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    document_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    content_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    storage_reference = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    issue_date = table.Column<DateOnly>(type: "date", nullable: true),
                    expiry_date = table.Column<DateOnly>(type: "date", nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    uploaded_by_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    uploaded_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_kyc_documents", x => x.id);
                    table.ForeignKey(
                        name: "fk_kyc_documents_investors_investor_id",
                        column: x => x.investor_id,
                        principalSchema: "investors",
                        principalTable: "investors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "kyc_requirements",
                schema: "kyc",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    investor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    document_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    is_mandatory = table.Column<bool>(type: "boolean", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    satisfied_by_document_id = table.Column<Guid>(type: "uuid", nullable: true),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_kyc_requirements", x => x.id);
                    table.ForeignKey(
                        name: "fk_kyc_requirements_investors_investor_id",
                        column: x => x.investor_id,
                        principalSchema: "investors",
                        principalTable: "investors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "kyc_reviews",
                schema: "kyc",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    investor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    performed_by_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    performed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    comment = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_kyc_reviews", x => x.id);
                    table.ForeignKey(
                        name: "fk_kyc_reviews_investors_investor_id",
                        column: x => x.investor_id,
                        principalSchema: "investors",
                        principalTable: "investors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "aml_screening_hits",
                schema: "aml",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    aml_screening_case_id = table.Column<Guid>(type: "uuid", nullable: false),
                    list_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    matched_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    risk_level = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    is_resolved = table.Column<bool>(type: "boolean", nullable: false),
                    row_version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_aml_screening_hits", x => x.id);
                    table.ForeignKey(
                        name: "fk_aml_screening_hits_aml_screening_cases_aml_screening_case_id",
                        column: x => x.aml_screening_case_id,
                        principalSchema: "aml",
                        principalTable: "aml_screening_cases",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_aml_screening_cases_investor_id_screened_at_utc",
                schema: "aml",
                table: "aml_screening_cases",
                columns: new[] { "investor_id", "screened_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_aml_screening_cases_status",
                schema: "aml",
                table: "aml_screening_cases",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_aml_screening_hits_aml_screening_case_id",
                schema: "aml",
                table: "aml_screening_hits",
                column: "aml_screening_case_id");

            migrationBuilder.CreateIndex(
                name: "ix_aml_screening_hits_risk_level_is_resolved",
                schema: "aml",
                table: "aml_screening_hits",
                columns: new[] { "risk_level", "is_resolved" });

            migrationBuilder.CreateIndex(
                name: "ix_beneficial_owners_investor_id_identity_number",
                schema: "investors",
                table: "beneficial_owners",
                columns: new[] { "investor_id", "identity_number" });

            migrationBuilder.CreateIndex(
                name: "ix_beneficial_owners_is_politically_exposed",
                schema: "investors",
                table: "beneficial_owners",
                column: "is_politically_exposed");

            migrationBuilder.CreateIndex(
                name: "ix_duplicate_detection_results_investor_id_match_type_matched_",
                schema: "investors",
                table: "duplicate_detection_results",
                columns: new[] { "investor_id", "match_type", "matched_value" });

            migrationBuilder.CreateIndex(
                name: "ix_duplicate_detection_results_status",
                schema: "investors",
                table: "duplicate_detection_results",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_investor_bank_accounts_account_number",
                schema: "investors",
                table: "investor_bank_accounts",
                column: "account_number");

            migrationBuilder.CreateIndex(
                name: "ix_investor_bank_accounts_high_risk_flag_is_active",
                schema: "investors",
                table: "investor_bank_accounts",
                columns: new[] { "high_risk_flag", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_investor_bank_accounts_investor_id_account_number_currency",
                schema: "investors",
                table: "investor_bank_accounts",
                columns: new[] { "investor_id", "account_number", "currency" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_investor_change_logs_high_risk_flag",
                schema: "investors",
                table: "investor_change_logs",
                column: "high_risk_flag");

            migrationBuilder.CreateIndex(
                name: "ix_investor_change_logs_investor_id_changed_at_utc",
                schema: "investors",
                table: "investor_change_logs",
                columns: new[] { "investor_id", "changed_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_investor_contacts_contact_type_value",
                schema: "investors",
                table: "investor_contacts",
                columns: new[] { "contact_type", "value" });

            migrationBuilder.CreateIndex(
                name: "ix_investor_contacts_investor_id_is_primary",
                schema: "investors",
                table: "investor_contacts",
                columns: new[] { "investor_id", "is_primary" });

            migrationBuilder.CreateIndex(
                name: "ix_investor_mandates_investor_id_is_active",
                schema: "investors",
                table: "investor_mandates",
                columns: new[] { "investor_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_investor_profile_corporates_investor_id",
                schema: "investors",
                table: "investor_profile_corporates",
                column: "investor_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_investor_profile_corporates_registration_number",
                schema: "investors",
                table: "investor_profile_corporates",
                column: "registration_number");

            migrationBuilder.CreateIndex(
                name: "ix_investor_profile_groups_investor_id",
                schema: "investors",
                table: "investor_profile_groups",
                column: "investor_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_investor_profile_groups_registration_number",
                schema: "investors",
                table: "investor_profile_groups",
                column: "registration_number");

            migrationBuilder.CreateIndex(
                name: "ix_investor_profile_individuals_identity_number",
                schema: "investors",
                table: "investor_profile_individuals",
                column: "identity_number");

            migrationBuilder.CreateIndex(
                name: "ix_investor_profile_individuals_investor_id",
                schema: "investors",
                table: "investor_profile_individuals",
                column: "investor_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_investor_profile_joints_investor_id",
                schema: "investors",
                table: "investor_profile_joints",
                column: "investor_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_investor_profile_joints_primary_identity_number",
                schema: "investors",
                table: "investor_profile_joints",
                column: "primary_identity_number");

            migrationBuilder.CreateIndex(
                name: "ix_investor_profile_joints_secondary_identity_number",
                schema: "investors",
                table: "investor_profile_joints",
                column: "secondary_identity_number");

            migrationBuilder.CreateIndex(
                name: "ix_investor_risk_classifications_investor_id_status",
                schema: "investors",
                table: "investor_risk_classifications",
                columns: new[] { "investor_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_investor_risk_classifications_risk_category",
                schema: "investors",
                table: "investor_risk_classifications",
                column: "risk_category");

            migrationBuilder.CreateIndex(
                name: "ix_investor_tax_profiles_investor_id",
                schema: "investors",
                table: "investor_tax_profiles",
                column: "investor_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_investor_tax_profiles_tax_number",
                schema: "investors",
                table: "investor_tax_profiles",
                column: "tax_number");

            migrationBuilder.CreateIndex(
                name: "ix_investors_email",
                schema: "investors",
                table: "investors",
                column: "email");

            migrationBuilder.CreateIndex(
                name: "ix_investors_investor_number",
                schema: "investors",
                table: "investors",
                column: "investor_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_investors_investor_type",
                schema: "investors",
                table: "investors",
                column: "investor_type");

            migrationBuilder.CreateIndex(
                name: "ix_investors_phone_number",
                schema: "investors",
                table: "investors",
                column: "phone_number");

            migrationBuilder.CreateIndex(
                name: "ix_investors_status",
                schema: "investors",
                table: "investors",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_kyc_documents_expiry_date",
                schema: "documents",
                table: "kyc_documents",
                column: "expiry_date");

            migrationBuilder.CreateIndex(
                name: "ix_kyc_documents_investor_id_document_type",
                schema: "documents",
                table: "kyc_documents",
                columns: new[] { "investor_id", "document_type" });

            migrationBuilder.CreateIndex(
                name: "ix_kyc_documents_status",
                schema: "documents",
                table: "kyc_documents",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_kyc_requirements_investor_id_document_type",
                schema: "kyc",
                table: "kyc_requirements",
                columns: new[] { "investor_id", "document_type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_kyc_requirements_status",
                schema: "kyc",
                table: "kyc_requirements",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_kyc_reviews_investor_id_performed_at_utc",
                schema: "kyc",
                table: "kyc_reviews",
                columns: new[] { "investor_id", "performed_at_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "aml_screening_hits",
                schema: "aml");

            migrationBuilder.DropTable(
                name: "beneficial_owners",
                schema: "investors");

            migrationBuilder.DropTable(
                name: "duplicate_detection_results",
                schema: "investors");

            migrationBuilder.DropTable(
                name: "investor_bank_accounts",
                schema: "investors");

            migrationBuilder.DropTable(
                name: "investor_change_logs",
                schema: "investors");

            migrationBuilder.DropTable(
                name: "investor_contacts",
                schema: "investors");

            migrationBuilder.DropTable(
                name: "investor_mandates",
                schema: "investors");

            migrationBuilder.DropTable(
                name: "investor_profile_corporates",
                schema: "investors");

            migrationBuilder.DropTable(
                name: "investor_profile_groups",
                schema: "investors");

            migrationBuilder.DropTable(
                name: "investor_profile_individuals",
                schema: "investors");

            migrationBuilder.DropTable(
                name: "investor_profile_joints",
                schema: "investors");

            migrationBuilder.DropTable(
                name: "investor_risk_classifications",
                schema: "investors");

            migrationBuilder.DropTable(
                name: "investor_tax_profiles",
                schema: "investors");

            migrationBuilder.DropTable(
                name: "kyc_documents",
                schema: "documents");

            migrationBuilder.DropTable(
                name: "kyc_requirements",
                schema: "kyc");

            migrationBuilder.DropTable(
                name: "kyc_reviews",
                schema: "kyc");

            migrationBuilder.DropTable(
                name: "aml_screening_cases",
                schema: "aml");

            migrationBuilder.DropTable(
                name: "investors",
                schema: "investors");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmploymentVerify.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddVerificationEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "companies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    registration_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    hr_contact_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    hr_email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    hr_phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    province = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    postal_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    force_call = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_verified = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_companies", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "verification_requests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    requestor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    id_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    sa_id_number = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    passport_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    passport_country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    company_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: true),
                    job_title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    employment_start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    employment_end_date = table.Column<DateOnly>(type: "date", nullable: true),
                    hr_contact_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    hr_email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    hr_phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    popia_consent_given = table.Column<bool>(type: "boolean", nullable: false),
                    accuracy_confirmed = table.Column<bool>(type: "boolean", nullable: false),
                    consent_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    consent_recorded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "Pending"),
                    verification_method = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_verification_requests", x => x.id);
                    table.ForeignKey(
                        name: "FK_verification_requests_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_verification_requests_users_requestor_id",
                        column: x => x.requestor_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "email_verification_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    verification_request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_used = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_email_verification_tokens", x => x.id);
                    table.ForeignKey(
                        name: "FK_email_verification_tokens_verification_requests_verificatio~",
                        column: x => x.verification_request_id,
                        principalTable: "verification_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "operator_notes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    verification_request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    operator_id = table.Column<Guid>(type: "uuid", nullable: false),
                    call_outcome = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_operator_notes", x => x.id);
                    table.ForeignKey(
                        name: "FK_operator_notes_users_operator_id",
                        column: x => x.operator_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_operator_notes_verification_requests_verification_request_id",
                        column: x => x.verification_request_id,
                        principalTable: "verification_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "verification_responses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    verification_request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    responded_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    response_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    confirmed_job_title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    confirmed_start_date = table.Column<DateOnly>(type: "date", nullable: true),
                    confirmed_end_date = table.Column<DateOnly>(type: "date", nullable: true),
                    is_currently_employed = table.Column<bool>(type: "boolean", nullable: true),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    responded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_verification_responses", x => x.id);
                    table.ForeignKey(
                        name: "FK_verification_responses_verification_requests_verification_r~",
                        column: x => x.verification_request_id,
                        principalTable: "verification_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_companies_name",
                table: "companies",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "IX_companies_registration_number",
                table: "companies",
                column: "registration_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_email_verification_tokens_token",
                table: "email_verification_tokens",
                column: "token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_email_verification_tokens_verification_request_id",
                table: "email_verification_tokens",
                column: "verification_request_id");

            migrationBuilder.CreateIndex(
                name: "IX_operator_notes_operator_id",
                table: "operator_notes",
                column: "operator_id");

            migrationBuilder.CreateIndex(
                name: "IX_operator_notes_verification_request_id",
                table: "operator_notes",
                column: "verification_request_id");

            migrationBuilder.CreateIndex(
                name: "IX_verification_requests_company_id",
                table: "verification_requests",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "IX_verification_requests_requestor_id",
                table: "verification_requests",
                column: "requestor_id");

            migrationBuilder.CreateIndex(
                name: "IX_verification_requests_status",
                table: "verification_requests",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_verification_responses_verification_request_id",
                table: "verification_responses",
                column: "verification_request_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "email_verification_tokens");

            migrationBuilder.DropTable(
                name: "operator_notes");

            migrationBuilder.DropTable(
                name: "verification_responses");

            migrationBuilder.DropTable(
                name: "verification_requests");

            migrationBuilder.DropTable(
                name: "companies");
        }
    }
}

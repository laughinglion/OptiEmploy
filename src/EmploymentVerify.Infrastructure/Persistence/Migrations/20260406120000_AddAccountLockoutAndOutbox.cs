using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmploymentVerify.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountLockoutAndOutbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Account lockout fields on users
            migrationBuilder.AddColumn<int>(
                name: "failed_login_attempts",
                table: "users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "locked_until",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            // Email outbox table
            migrationBuilder.CreateTable(
                name: "outbox_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    to_address = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    subject = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    html_body = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    attempt_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    last_error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox_messages", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_sent_at",
                table: "outbox_messages",
                column: "sent_at");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_created_at",
                table: "outbox_messages",
                column: "created_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "outbox_messages");

            migrationBuilder.DropColumn(name: "failed_login_attempts", table: "users");
            migrationBuilder.DropColumn(name: "locked_until", table: "users");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmploymentVerify.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPasswordResetAndProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "password_reset_token",
                table: "users",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "password_reset_token_expires_at",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "phone_number",
                table: "users",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_password_reset_token",
                table: "users",
                column: "password_reset_token",
                unique: true,
                filter: "password_reset_token IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_users_password_reset_token", table: "users");
            migrationBuilder.DropColumn(name: "password_reset_token", table: "users");
            migrationBuilder.DropColumn(name: "password_reset_token_expires_at", table: "users");
            migrationBuilder.DropColumn(name: "phone_number", table: "users");
        }
    }
}

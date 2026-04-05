using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmploymentVerify.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCostAmountAndUnableToVerify : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "cost_amount",
                table: "verification_requests",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "cost_amount",
                table: "verification_requests");
        }
    }
}

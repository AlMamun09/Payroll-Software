using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PayrollSoftware.Data.Migrations
{
    /// <inheritdoc />
    public partial class NewFieldInPayroll : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AbsentDays",
                table: "Payrolls",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PaidLeaveDays",
                table: "Payrolls",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PayableDays",
                table: "Payrolls",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PresentDays",
                table: "Payrolls",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalDays",
                table: "Payrolls",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UnpaidLeaveDays",
                table: "Payrolls",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AbsentDays",
                table: "Payrolls");

            migrationBuilder.DropColumn(
                name: "PaidLeaveDays",
                table: "Payrolls");

            migrationBuilder.DropColumn(
                name: "PayableDays",
                table: "Payrolls");

            migrationBuilder.DropColumn(
                name: "PresentDays",
                table: "Payrolls");

            migrationBuilder.DropColumn(
                name: "TotalDays",
                table: "Payrolls");

            migrationBuilder.DropColumn(
                name: "UnpaidLeaveDays",
                table: "Payrolls");
        }
    }
}

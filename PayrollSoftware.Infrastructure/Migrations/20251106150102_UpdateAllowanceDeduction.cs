using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PayrollSoftware.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAllowanceDeduction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EffectiveTo",
                table: "AllowanceDeductions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "EmployeeId",
                table: "AllowanceDeductions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "AllowanceDeductions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "PayrollId",
                table: "AllowanceDeductions",
                type: "uniqueidentifier",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EffectiveTo",
                table: "AllowanceDeductions");

            migrationBuilder.DropColumn(
                name: "EmployeeId",
                table: "AllowanceDeductions");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "AllowanceDeductions");

            migrationBuilder.DropColumn(
                name: "PayrollId",
                table: "AllowanceDeductions");
        }
    }
}

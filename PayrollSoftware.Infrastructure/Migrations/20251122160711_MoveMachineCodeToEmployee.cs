using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PayrollSoftware.Data.Migrations
{
    /// <inheritdoc />
    public partial class MoveMachineCodeToEmployee : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MachineCode",
                table: "Attendances");

            migrationBuilder.AddColumn<int>(
                name: "MachineCode",
                table: "Employees",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MachineCode",
                table: "Employees");

            migrationBuilder.AddColumn<int>(
                name: "MachineCode",
                table: "Attendances",
                type: "int",
                nullable: true);
        }
    }
}

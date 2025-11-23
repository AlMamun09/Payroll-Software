using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PayrollSoftware.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMachineCodeToEmployee : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MachineCode",
                table: "Attendances",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MachineCode",
                table: "Attendances");
        }
    }
}

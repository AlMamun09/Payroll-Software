using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PayrollSoftware.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveForeignKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Payrolls");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Payrolls",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}

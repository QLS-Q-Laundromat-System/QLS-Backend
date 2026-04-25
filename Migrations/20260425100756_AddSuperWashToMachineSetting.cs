using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QLS.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddSuperWashToMachineSetting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AddSuperWash",
                table: "MachineSettings",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AddSuperWash",
                table: "MachineSettings");
        }
    }
}

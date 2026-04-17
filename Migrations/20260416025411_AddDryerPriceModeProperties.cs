using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QLS.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddDryerPriceModeProperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ExtensionTimeoutMinutes",
                table: "PriceModePerSessions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MinInitialSteps",
                table: "PriceModePerSessions",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExtensionTimeoutMinutes",
                table: "PriceModePerSessions");

            migrationBuilder.DropColumn(
                name: "MinInitialSteps",
                table: "PriceModePerSessions");
        }
    }
}

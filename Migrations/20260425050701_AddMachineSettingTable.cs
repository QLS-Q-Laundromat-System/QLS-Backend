using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QLS.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddMachineSettingTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MachineSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MachineId = table.Column<Guid>(type: "uuid", nullable: false),
                    Coin = table.Column<int>(type: "integer", nullable: false),
                    Price = table.Column<int[]>(type: "integer[]", nullable: false),
                    WashingTime = table.Column<int[]>(type: "integer[]", nullable: true),
                    WaterLevel = table.Column<int[]>(type: "integer[]", nullable: true),
                    RinsingTime = table.Column<int[]>(type: "integer[]", nullable: true),
                    RinsingCount = table.Column<int[]>(type: "integer[]", nullable: true),
                    SpinSpeed = table.Column<string[]>(type: "text[]", nullable: true),
                    TwinSpray = table.Column<string>(type: "text", nullable: true),
                    DropCount = table.Column<int>(type: "integer", nullable: true),
                    NonStopRinsing = table.Column<bool>(type: "boolean", nullable: true),
                    DryCycleTime = table.Column<int[]>(type: "integer[]", nullable: true),
                    TopOffTime = table.Column<int[]>(type: "integer[]", nullable: true),
                    TopOff = table.Column<string>(type: "text", nullable: true),
                    SensingDry = table.Column<string>(type: "text", nullable: true),
                    TopOffPrice = table.Column<int[]>(type: "integer[]", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MachineSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MachineSettings_Machines_MachineId",
                        column: x => x.MachineId,
                        principalTable: "Machines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MachineSettings_MachineId",
                table: "MachineSettings",
                column: "MachineId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MachineSettings");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QLS.Backend.Migrations
{
    /// <inheritdoc />
    public partial class UpdateMachineModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MachineSessions_Machines_MachineId",
                table: "MachineSessions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Machines",
                table: "Machines");

            migrationBuilder.DropColumn(
                name: "MachineId",
                table: "Machines");

            migrationBuilder.Sql("ALTER TABLE \"MachineSessions\" ALTER COLUMN \"MachineId\" TYPE uuid USING \"MachineId\"::uuid;");

            migrationBuilder.AddColumn<Guid>(
                name: "Id",
                table: "Machines",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "Esp32MacAddress",
                table: "Machines",
                type: "character varying(17)",
                maxLength: 17,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LgDeviceId",
                table: "Machines",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Machines",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ZigbeeNetworkId",
                table: "Machines",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Machines",
                table: "Machines",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MachineSessions_Machines_MachineId",
                table: "MachineSessions",
                column: "MachineId",
                principalTable: "Machines",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MachineSessions_Machines_MachineId",
                table: "MachineSessions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Machines",
                table: "Machines");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "Machines");

            migrationBuilder.DropColumn(
                name: "Esp32MacAddress",
                table: "Machines");

            migrationBuilder.DropColumn(
                name: "LgDeviceId",
                table: "Machines");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "Machines");

            migrationBuilder.DropColumn(
                name: "ZigbeeNetworkId",
                table: "Machines");

            migrationBuilder.AlterColumn<string>(
                name: "MachineId",
                table: "MachineSessions",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<string>(
                name: "MachineId",
                table: "Machines",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Machines",
                table: "Machines",
                column: "MachineId");

            migrationBuilder.AddForeignKey(
                name: "FK_MachineSessions_Machines_MachineId",
                table: "MachineSessions",
                column: "MachineId",
                principalTable: "Machines",
                principalColumn: "MachineId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

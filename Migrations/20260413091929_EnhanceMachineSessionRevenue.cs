using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QLS.Backend.Migrations
{
    /// <inheritdoc />
    public partial class EnhanceMachineSessionRevenue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ActualEndTime",
                table: "MachineSessions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "MachineSessions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<decimal>(
                name: "PricePaid",
                table: "MachineSessions",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "StoreId",
                table: "MachineSessions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<int>(
                name: "TotalMinutes",
                table: "MachineSessions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "TransactionId",
                table: "MachineSessions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "MachineSessions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateIndex(
                name: "IX_MachineSessions_StoreId",
                table: "MachineSessions",
                column: "StoreId");

            migrationBuilder.AddForeignKey(
                name: "FK_MachineSessions_Stores_StoreId",
                table: "MachineSessions",
                column: "StoreId",
                principalTable: "Stores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MachineSessions_Stores_StoreId",
                table: "MachineSessions");

            migrationBuilder.DropIndex(
                name: "IX_MachineSessions_StoreId",
                table: "MachineSessions");

            migrationBuilder.DropColumn(
                name: "ActualEndTime",
                table: "MachineSessions");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "MachineSessions");

            migrationBuilder.DropColumn(
                name: "PricePaid",
                table: "MachineSessions");

            migrationBuilder.DropColumn(
                name: "StoreId",
                table: "MachineSessions");

            migrationBuilder.DropColumn(
                name: "TotalMinutes",
                table: "MachineSessions");

            migrationBuilder.DropColumn(
                name: "TransactionId",
                table: "MachineSessions");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "MachineSessions");
        }
    }
}

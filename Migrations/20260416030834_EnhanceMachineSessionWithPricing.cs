using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QLS.Backend.Migrations
{
    /// <inheritdoc />
    public partial class EnhanceMachineSessionWithPricing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CycleName",
                table: "MachineSessions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsExtension",
                table: "MachineSessions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "PriceListId",
                table: "MachineSessions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PricingMode",
                table: "MachineSessions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxAmount",
                table: "MachineSessions",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "WeightKg",
                table: "MachineSessions",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MachineSessions_PriceListId",
                table: "MachineSessions",
                column: "PriceListId");

            migrationBuilder.AddForeignKey(
                name: "FK_MachineSessions_PriceLists_PriceListId",
                table: "MachineSessions",
                column: "PriceListId",
                principalTable: "PriceLists",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MachineSessions_PriceLists_PriceListId",
                table: "MachineSessions");

            migrationBuilder.DropIndex(
                name: "IX_MachineSessions_PriceListId",
                table: "MachineSessions");

            migrationBuilder.DropColumn(
                name: "CycleName",
                table: "MachineSessions");

            migrationBuilder.DropColumn(
                name: "IsExtension",
                table: "MachineSessions");

            migrationBuilder.DropColumn(
                name: "PriceListId",
                table: "MachineSessions");

            migrationBuilder.DropColumn(
                name: "PricingMode",
                table: "MachineSessions");

            migrationBuilder.DropColumn(
                name: "TaxAmount",
                table: "MachineSessions");

            migrationBuilder.DropColumn(
                name: "WeightKg",
                table: "MachineSessions");
        }
    }
}

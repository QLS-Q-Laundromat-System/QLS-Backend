using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QLS.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddPricingEnhancements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BrandId",
                table: "TimeSlots",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "TimeSlots",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CycleName",
                table: "PriceModePerSessions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MinimumPrice",
                table: "PriceModePerKgs",
                type: "numeric(12,0)",
                precision: 12,
                scale: 0,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "BrandId",
                table: "PriceLists",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "PriceLists",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PromotionLabel",
                table: "PriceLists",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxPercentage",
                table: "PriceLists",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "IX_TimeSlots_BrandId",
                table: "TimeSlots",
                column: "BrandId");

            migrationBuilder.CreateIndex(
                name: "IX_PriceLists_BrandId",
                table: "PriceLists",
                column: "BrandId");

            migrationBuilder.AddForeignKey(
                name: "FK_PriceLists_Brands_BrandId",
                table: "PriceLists",
                column: "BrandId",
                principalTable: "Brands",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TimeSlots_Brands_BrandId",
                table: "TimeSlots",
                column: "BrandId",
                principalTable: "Brands",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PriceLists_Brands_BrandId",
                table: "PriceLists");

            migrationBuilder.DropForeignKey(
                name: "FK_TimeSlots_Brands_BrandId",
                table: "TimeSlots");

            migrationBuilder.DropIndex(
                name: "IX_TimeSlots_BrandId",
                table: "TimeSlots");

            migrationBuilder.DropIndex(
                name: "IX_PriceLists_BrandId",
                table: "PriceLists");

            migrationBuilder.DropColumn(
                name: "BrandId",
                table: "TimeSlots");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "TimeSlots");

            migrationBuilder.DropColumn(
                name: "CycleName",
                table: "PriceModePerSessions");

            migrationBuilder.DropColumn(
                name: "MinimumPrice",
                table: "PriceModePerKgs");

            migrationBuilder.DropColumn(
                name: "BrandId",
                table: "PriceLists");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "PriceLists");

            migrationBuilder.DropColumn(
                name: "PromotionLabel",
                table: "PriceLists");

            migrationBuilder.DropColumn(
                name: "TaxPercentage",
                table: "PriceLists");
        }
    }
}

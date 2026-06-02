using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QLS.Backend.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceZaloLoyaltyAuthWithOtp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "PointClaimTokens";
                DELETE FROM "LoyaltyPointTransactions";
                DELETE FROM "LoyaltyCustomers";
                """);

            migrationBuilder.DropIndex(
                name: "IX_LoyaltyCustomers_BrandId_ZaloUserId",
                table: "LoyaltyCustomers");

            migrationBuilder.DropColumn(
                name: "ZaloOAUserId",
                table: "LoyaltyCustomers");

            migrationBuilder.DropColumn(
                name: "ZaloUserId",
                table: "LoyaltyCustomers");

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "LoyaltyCustomers",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsEmailVerified",
                table: "LoyaltyCustomers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPhoneNumberVerified",
                table: "LoyaltyCustomers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PasswordHash",
                table: "LoyaltyCustomers",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "LoyaltyCustomers",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LoyaltyOtpChallenges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BrandId = table.Column<Guid>(type: "uuid", nullable: false),
                    Identifier = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Channel = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Purpose = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CodeHash = table.Column<string>(type: "text", nullable: false),
                    FailedAttempts = table.Column<int>(type: "integer", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ConsumedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoyaltyOtpChallenges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoyaltyOtpChallenges_Brands_BrandId",
                        column: x => x.BrandId,
                        principalTable: "Brands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyCustomers_BrandId_Email",
                table: "LoyaltyCustomers",
                columns: new[] { "BrandId", "Email" },
                unique: true,
                filter: "\"Email\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyCustomers_BrandId_PhoneNumber",
                table: "LoyaltyCustomers",
                columns: new[] { "BrandId", "PhoneNumber" },
                unique: true,
                filter: "\"PhoneNumber\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyOtpChallenges_BrandId_Identifier_Purpose_CreatedAt",
                table: "LoyaltyOtpChallenges",
                columns: new[] { "BrandId", "Identifier", "Purpose", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LoyaltyOtpChallenges");

            migrationBuilder.DropIndex(
                name: "IX_LoyaltyCustomers_BrandId_Email",
                table: "LoyaltyCustomers");

            migrationBuilder.DropIndex(
                name: "IX_LoyaltyCustomers_BrandId_PhoneNumber",
                table: "LoyaltyCustomers");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "LoyaltyCustomers");

            migrationBuilder.DropColumn(
                name: "IsEmailVerified",
                table: "LoyaltyCustomers");

            migrationBuilder.DropColumn(
                name: "IsPhoneNumberVerified",
                table: "LoyaltyCustomers");

            migrationBuilder.DropColumn(
                name: "PasswordHash",
                table: "LoyaltyCustomers");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "LoyaltyCustomers");

            migrationBuilder.AddColumn<string>(
                name: "ZaloOAUserId",
                table: "LoyaltyCustomers",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ZaloUserId",
                table: "LoyaltyCustomers",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyCustomers_BrandId_ZaloUserId",
                table: "LoyaltyCustomers",
                columns: new[] { "BrandId", "ZaloUserId" },
                unique: true);
        }
    }
}

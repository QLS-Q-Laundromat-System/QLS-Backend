using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QLS.Backend.Migrations
{
    /// <inheritdoc />
    public partial class SecureZaloLoyaltyAuth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "PointClaimTokens";
                DELETE FROM "LoyaltyPointTransactions";
                DELETE FROM "LoyaltyCustomers";
                """);

            migrationBuilder.DropColumn(
                name: "ZaloOAUserId",
                table: "LoyaltyCustomers");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ZaloOAUserId",
                table: "LoyaltyCustomers",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }
    }
}

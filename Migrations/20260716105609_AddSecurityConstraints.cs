using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QLS.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddSecurityConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_GatewayTransactionId",
                table: "PaymentTransactions",
                column: "GatewayTransactionId",
                unique: true,
                filter: "\"GatewayTransactionId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MachineSessions_PaymentCode",
                table: "MachineSessions",
                column: "PaymentCode",
                unique: true,
                filter: "\"PaymentCode\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_Username",
                table: "Accounts",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PaymentTransactions_GatewayTransactionId",
                table: "PaymentTransactions");

            migrationBuilder.DropIndex(
                name: "IX_MachineSessions_PaymentCode",
                table: "MachineSessions");

            migrationBuilder.DropIndex(
                name: "IX_Accounts_Username",
                table: "Accounts");
        }
    }
}

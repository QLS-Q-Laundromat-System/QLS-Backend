using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QLS.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddLoyaltyModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LoyaltyCustomers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BrandId = table.Column<Guid>(type: "uuid", nullable: false),
                    ZaloUserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ZaloOAUserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    FullName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    AvatarUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    TotalPoints = table.Column<int>(type: "integer", nullable: false),
                    CustomerType = table.Column<int>(type: "integer", nullable: false),
                    StudentVerificationStatus = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoyaltyCustomers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoyaltyCustomers_Brands_BrandId",
                        column: x => x.BrandId,
                        principalTable: "Brands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LoyaltyPointTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BrandId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    MachineSessionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Points = table.Column<int>(type: "integer", nullable: false),
                    RemainingPoints = table.Column<int>(type: "integer", nullable: false),
                    ExpiredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoyaltyPointTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoyaltyPointTransactions_Brands_BrandId",
                        column: x => x.BrandId,
                        principalTable: "Brands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LoyaltyPointTransactions_LoyaltyCustomers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "LoyaltyCustomers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LoyaltyPointTransactions_MachineSessions_MachineSessionId",
                        column: x => x.MachineSessionId,
                        principalTable: "MachineSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "PointClaimTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    BrandId = table.Column<Guid>(type: "uuid", nullable: false),
                    MachineSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentTransactionId = table.Column<Guid>(type: "uuid", nullable: true),
                    PaidAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PointsToEarn = table.Column<int>(type: "integer", nullable: false),
                    IsClaimed = table.Column<bool>(type: "boolean", nullable: false),
                    ExpiredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ClaimedByCustomerId = table.Column<Guid>(type: "uuid", nullable: true),
                    ClaimedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PointClaimTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PointClaimTokens_Brands_BrandId",
                        column: x => x.BrandId,
                        principalTable: "Brands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PointClaimTokens_LoyaltyCustomers_ClaimedByCustomerId",
                        column: x => x.ClaimedByCustomerId,
                        principalTable: "LoyaltyCustomers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PointClaimTokens_MachineSessions_MachineSessionId",
                        column: x => x.MachineSessionId,
                        principalTable: "MachineSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PointClaimTokens_PaymentTransactions_PaymentTransactionId",
                        column: x => x.PaymentTransactionId,
                        principalTable: "PaymentTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyCustomers_BrandId_ZaloUserId",
                table: "LoyaltyCustomers",
                columns: new[] { "BrandId", "ZaloUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyPointTransactions_BrandId",
                table: "LoyaltyPointTransactions",
                column: "BrandId");

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyPointTransactions_CustomerId",
                table: "LoyaltyPointTransactions",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyPointTransactions_MachineSessionId_Type",
                table: "LoyaltyPointTransactions",
                columns: new[] { "MachineSessionId", "Type" },
                unique: true,
                filter: "\"MachineSessionId\" IS NOT NULL AND \"Type\" = 0");

            migrationBuilder.CreateIndex(
                name: "IX_PointClaimTokens_BrandId",
                table: "PointClaimTokens",
                column: "BrandId");

            migrationBuilder.CreateIndex(
                name: "IX_PointClaimTokens_ClaimedByCustomerId",
                table: "PointClaimTokens",
                column: "ClaimedByCustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_PointClaimTokens_MachineSessionId",
                table: "PointClaimTokens",
                column: "MachineSessionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PointClaimTokens_PaymentTransactionId",
                table: "PointClaimTokens",
                column: "PaymentTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_PointClaimTokens_Token",
                table: "PointClaimTokens",
                column: "Token",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LoyaltyPointTransactions");

            migrationBuilder.DropTable(
                name: "PointClaimTokens");

            migrationBuilder.DropTable(
                name: "LoyaltyCustomers");
        }
    }
}

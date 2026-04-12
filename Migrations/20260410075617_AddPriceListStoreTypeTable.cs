using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QLS.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddPriceListStoreTypeTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PriceListStoreTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PriceListId = table.Column<Guid>(type: "uuid", nullable: false),
                    StoreTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    OverridePriority = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceListStoreTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PriceListStoreTypes_PriceLists_PriceListId",
                        column: x => x.PriceListId,
                        principalTable: "PriceLists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PriceListStoreTypes_StoreTypes_StoreTypeId",
                        column: x => x.StoreTypeId,
                        principalTable: "StoreTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PriceListStoreTypes_PriceListId_StoreTypeId",
                table: "PriceListStoreTypes",
                columns: new[] { "PriceListId", "StoreTypeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PriceListStoreTypes_StoreTypeId",
                table: "PriceListStoreTypes",
                column: "StoreTypeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PriceListStoreTypes");
        }
    }
}

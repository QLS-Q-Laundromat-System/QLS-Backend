using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QLS.Backend.Migrations
{
    /// <inheritdoc />
    public partial class RenameMachineBranchIdToStoreId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Machines_Stores_BranchId",
                table: "Machines");

            migrationBuilder.RenameColumn(
                name: "BranchId",
                table: "Machines",
                newName: "StoreId");

            migrationBuilder.RenameIndex(
                name: "IX_Machines_BranchId",
                table: "Machines",
                newName: "IX_Machines_StoreId");

            migrationBuilder.AddForeignKey(
                name: "FK_Machines_Stores_StoreId",
                table: "Machines",
                column: "StoreId",
                principalTable: "Stores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Machines_Stores_StoreId",
                table: "Machines");

            migrationBuilder.RenameColumn(
                name: "StoreId",
                table: "Machines",
                newName: "BranchId");

            migrationBuilder.RenameIndex(
                name: "IX_Machines_StoreId",
                table: "Machines",
                newName: "IX_Machines_BranchId");

            migrationBuilder.AddForeignKey(
                name: "FK_Machines_Stores_BranchId",
                table: "Machines",
                column: "BranchId",
                principalTable: "Stores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

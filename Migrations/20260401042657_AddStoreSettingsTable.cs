using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace QLS.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddStoreSettingsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StoreSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StoreId = table.Column<string>(type: "text", nullable: false),
                    DryerStepMinutes = table.Column<int>(type: "integer", nullable: false),
                    DryerStepPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    DryerMinInitialMinutes = table.Column<int>(type: "integer", nullable: false),
                    DryerGracePeriodMinutes = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoreSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StoreSettings_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "StoreId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StoreSettings_StoreId",
                table: "StoreSettings",
                column: "StoreId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StoreSettings");
        }
    }
}

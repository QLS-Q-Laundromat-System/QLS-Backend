using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QLS.Backend.Migrations
{
    /// <inheritdoc />
    public partial class UpdateModelSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS ""PaymentConfigs"" (
                    ""Id"" uuid NOT NULL,
                    ""BrandId"" uuid NOT NULL,
                    ""Provider"" text NOT NULL,
                    ""IsActive"" boolean NOT NULL,
                    ""BankCode"" text,
                    ""AccountNumber"" text,
                    ""AccountName"" text,
                    ""ApiKey"" text,
                    ""SecretKey"" text,
                    ""UpdatedAt"" timestamp with time zone NOT NULL,
                    CONSTRAINT ""PK_PaymentConfigs"" PRIMARY KEY (""Id""),
                    CONSTRAINT ""FK_PaymentConfigs_Brands_BrandId"" FOREIGN KEY (""BrandId"") REFERENCES ""Brands"" (""Id"") ON DELETE CASCADE
                );
            ");

            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ""IX_PaymentConfigs_BrandId""
                ON ""PaymentConfigs"" (""BrandId"");
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PaymentConfigs");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QLS.Backend.Migrations
{
    /// <inheritdoc />
    public partial class SyncDatabaseSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE ""MachineSessions"" 
                ADD COLUMN IF NOT EXISTS ""PaymentCode"" character varying(20) NULL;
            ");

            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS ""PaymentTransactions"" (
                    ""Id"" uuid NOT NULL,
                    ""MachineSessionId"" uuid NULL,
                    ""Amount"" numeric(18,2) NOT NULL,
                    ""PaymentMethod"" character varying(50) NOT NULL,
                    ""GatewayTransactionId"" character varying(100) NULL,
                    ""TransactionContent"" text NULL,
                    ""Status"" character varying(20) NOT NULL,
                    ""RawData"" text NULL,
                    ""CreatedAt"" timestamp with time zone NOT NULL,
                    CONSTRAINT ""PK_PaymentTransactions"" PRIMARY KEY (""Id""),
                    CONSTRAINT ""FK_PaymentTransactions_MachineSessions_MachineSessionId"" FOREIGN KEY (""MachineSessionId"") REFERENCES ""MachineSessions"" (""Id"")
                );
            ");

            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ""IX_PaymentTransactions_MachineSessionId""
                ON ""PaymentTransactions"" (""MachineSessionId"");
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PaymentTransactions");

            migrationBuilder.DropColumn(
                name: "PaymentCode",
                table: "MachineSessions");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LedgerCore.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase6_Stage2_AuditMetadataAndMoney : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Audit_DeviceId",
                table: "LedgerTransactions",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Audit_IpAddress",
                table: "LedgerTransactions",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "Audit_UserId",
                table: "LedgerTransactions",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "LedgerEntries",
                type: "decimal(19,4)",
                precision: 19,
                scale: 4,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,4)",
                oldPrecision: 18,
                oldScale: 4);

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "LedgerEntries",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Audit_DeviceId",
                table: "LedgerTransactions");

            migrationBuilder.DropColumn(
                name: "Audit_IpAddress",
                table: "LedgerTransactions");

            migrationBuilder.DropColumn(
                name: "Audit_UserId",
                table: "LedgerTransactions");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "LedgerEntries");

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "LedgerEntries",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(19,4)",
                oldPrecision: 19,
                oldScale: 4);
        }
    }
}

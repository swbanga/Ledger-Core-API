using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LedgerCore.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase3_CQRS_AccountBalances : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Channel",
                table: "LedgerTransactions");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "LedgerTransactions");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "LedgerTransactions");

            migrationBuilder.DropColumn(
                name: "IpAddress",
                table: "LedgerTransactions");

            migrationBuilder.RenameColumn(
                name: "Type",
                table: "LedgerTransactions",
                newName: "TransactionType");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "LedgerTransactions",
                newName: "TimestampUtc");

            migrationBuilder.AddColumn<string>(
                name: "CorrelationId",
                table: "LedgerTransactions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "AccountBalances",
                columns: table => new
                {
                    AccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrentBalance = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    LastUpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountBalances", x => x.AccountId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountBalances");

            migrationBuilder.DropColumn(
                name: "CorrelationId",
                table: "LedgerTransactions");

            migrationBuilder.RenameColumn(
                name: "TransactionType",
                table: "LedgerTransactions",
                newName: "Type");

            migrationBuilder.RenameColumn(
                name: "TimestampUtc",
                table: "LedgerTransactions",
                newName: "CreatedAt");

            migrationBuilder.AddColumn<string>(
                name: "Channel",
                table: "LedgerTransactions",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "LedgerTransactions",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "LedgerTransactions",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "IpAddress",
                table: "LedgerTransactions",
                type: "nvarchar(45)",
                maxLength: 45,
                nullable: false,
                defaultValue: "");
        }
    }
}

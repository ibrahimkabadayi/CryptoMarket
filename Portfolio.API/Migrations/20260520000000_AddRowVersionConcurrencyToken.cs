using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Portfolio.API.Migrations
{
    /// <inheritdoc />
    public partial class AddRowVersionConcurrencyToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Wallet",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Transaction",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Asset",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "LimitOrder",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "TreasuryBalance",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Wallet");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Transaction");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Asset");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "LimitOrder");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "TreasuryBalance");
        }
    }
}

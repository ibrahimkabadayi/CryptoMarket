using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Portfolio.API.Migrations
{
    /// <inheritdoc />
    public partial class ConcurrencyRowAdded : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "Wallets",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "TreasuryBalances",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "Transaction",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "LimitOrders",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "Asset",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "xmin",
                table: "Wallets");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "TreasuryBalances");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "Transaction");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "LimitOrders");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "Asset");
        }
    }
}

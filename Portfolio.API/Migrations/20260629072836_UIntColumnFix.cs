using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Portfolio.API.Migrations
{
    /// <inheritdoc />
    public partial class UIntColumnFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "RowVersion",
                table: "Wallets",
                newName: "xmin");

            migrationBuilder.RenameColumn(
                name: "RowVersion",
                table: "TreasuryBalances",
                newName: "xmin");

            migrationBuilder.RenameColumn(
                name: "RowVersion",
                table: "Transaction",
                newName: "xmin");

            migrationBuilder.RenameColumn(
                name: "RowVersion",
                table: "LimitOrders",
                newName: "xmin");

            migrationBuilder.RenameColumn(
                name: "RowVersion",
                table: "Asset",
                newName: "xmin");

            migrationBuilder.AlterColumn<uint>(
                name: "xmin",
                table: "Wallets",
                type: "xid",
                rowVersion: true,
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldRowVersion: true);

            migrationBuilder.AlterColumn<uint>(
                name: "xmin",
                table: "TreasuryBalances",
                type: "xid",
                rowVersion: true,
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldRowVersion: true);

            migrationBuilder.AlterColumn<uint>(
                name: "xmin",
                table: "Transaction",
                type: "xid",
                rowVersion: true,
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldRowVersion: true);

            migrationBuilder.AlterColumn<uint>(
                name: "xmin",
                table: "LimitOrders",
                type: "xid",
                rowVersion: true,
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldRowVersion: true);

            migrationBuilder.AlterColumn<uint>(
                name: "xmin",
                table: "Asset",
                type: "xid",
                rowVersion: true,
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldRowVersion: true);
        }
    }
}

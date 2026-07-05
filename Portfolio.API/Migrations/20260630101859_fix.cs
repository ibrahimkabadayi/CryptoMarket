using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Portfolio.API.Migrations
{
    /// <inheritdoc />
    public partial class fix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Asset_Wallets_WalletId",
                table: "Asset");

            migrationBuilder.DropForeignKey(
                name: "FK_Transaction_Wallets_WalletId",
                table: "Transaction");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Transaction",
                table: "Transaction");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Asset",
                table: "Asset");

            migrationBuilder.RenameTable(
                name: "Transaction",
                newName: "Transactions");

            migrationBuilder.RenameTable(
                name: "Asset",
                newName: "Assets");

            migrationBuilder.RenameIndex(
                name: "IX_Transaction_WalletId",
                table: "Transactions",
                newName: "IX_Transactions_WalletId");

            migrationBuilder.RenameIndex(
                name: "IX_Asset_WalletId",
                table: "Assets",
                newName: "IX_Assets_WalletId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Transactions",
                table: "Transactions",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Assets",
                table: "Assets",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Assets_Wallets_WalletId",
                table: "Assets",
                column: "WalletId",
                principalTable: "Wallets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_Wallets_WalletId",
                table: "Transactions",
                column: "WalletId",
                principalTable: "Wallets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Assets_Wallets_WalletId",
                table: "Assets");

            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_Wallets_WalletId",
                table: "Transactions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Transactions",
                table: "Transactions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Assets",
                table: "Assets");

            migrationBuilder.RenameTable(
                name: "Transactions",
                newName: "Transaction");

            migrationBuilder.RenameTable(
                name: "Assets",
                newName: "Asset");

            migrationBuilder.RenameIndex(
                name: "IX_Transactions_WalletId",
                table: "Transaction",
                newName: "IX_Transaction_WalletId");

            migrationBuilder.RenameIndex(
                name: "IX_Assets_WalletId",
                table: "Asset",
                newName: "IX_Asset_WalletId");         

            migrationBuilder.AddPrimaryKey(
                name: "PK_Transaction",
                table: "Transaction",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Asset",
                table: "Asset",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Asset_Wallets_WalletId",
                table: "Asset",
                column: "WalletId",
                principalTable: "Wallets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Transaction_Wallets_WalletId",
                table: "Transaction",
                column: "WalletId",
                principalTable: "Wallets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}

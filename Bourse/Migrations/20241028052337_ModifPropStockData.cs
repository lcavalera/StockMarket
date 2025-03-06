using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bourse.Migrations
{
    /// <inheritdoc />
    public partial class ModifPropStockData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StockData_Indices_IndiceId",
                table: "StockData");

            migrationBuilder.AlterColumn<int>(
                name: "IndiceId",
                table: "StockData",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_StockData_Indices_IndiceId",
                table: "StockData",
                column: "IndiceId",
                principalTable: "Indices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StockData_Indices_IndiceId",
                table: "StockData");

            migrationBuilder.AlterColumn<int>(
                name: "IndiceId",
                table: "StockData",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddForeignKey(
                name: "FK_StockData_Indices_IndiceId",
                table: "StockData",
                column: "IndiceId",
                principalTable: "Indices",
                principalColumn: "Id");
        }
    }
}

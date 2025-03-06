using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bourse.Migrations
{
    /// <inheritdoc />
    public partial class AjoutParamStockDataRsiSma : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<float>(
                name: "RSI_14",
                table: "StockData",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "SMA_14",
                table: "StockData",
                type: "REAL",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RSI_14",
                table: "StockData");

            migrationBuilder.DropColumn(
                name: "SMA_14",
                table: "StockData");
        }
    }
}

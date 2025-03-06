using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bourse.Migrations
{
    /// <inheritdoc />
    public partial class AjoutProps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "RegularMarketDayHigh",
                table: "RealPrices",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "RegularMarketDayLow",
                table: "RealPrices",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "RegularMarketOpen",
                table: "RealPrices",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "RegularMarketDayHigh",
                table: "Indices",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "RegularMarketDayLow",
                table: "Indices",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "RegularMarketOpen",
                table: "Indices",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "RegularMarketPreviousClose",
                table: "Indices",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "RegularMarketVolume",
                table: "Indices",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RegularMarketDayHigh",
                table: "RealPrices");

            migrationBuilder.DropColumn(
                name: "RegularMarketDayLow",
                table: "RealPrices");

            migrationBuilder.DropColumn(
                name: "RegularMarketOpen",
                table: "RealPrices");

            migrationBuilder.DropColumn(
                name: "RegularMarketDayHigh",
                table: "Indices");

            migrationBuilder.DropColumn(
                name: "RegularMarketDayLow",
                table: "Indices");

            migrationBuilder.DropColumn(
                name: "RegularMarketOpen",
                table: "Indices");

            migrationBuilder.DropColumn(
                name: "RegularMarketPreviousClose",
                table: "Indices");

            migrationBuilder.DropColumn(
                name: "RegularMarketVolume",
                table: "Indices");
        }
    }
}

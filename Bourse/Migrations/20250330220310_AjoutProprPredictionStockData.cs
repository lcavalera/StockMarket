using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bourse.Migrations
{
    /// <inheritdoc />
    public partial class AjoutProprPredictionStockData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<float>(
                name: "AverageVolume",
                table: "StockData",
                type: "REAL",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "BollingerLower",
                table: "StockData",
                type: "REAL",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "BollingerUpper",
                table: "StockData",
                type: "REAL",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "EMA_14",
                table: "StockData",
                type: "REAL",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "FuturePrice",
                table: "StockData",
                type: "REAL",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "MACD",
                table: "StockData",
                type: "REAL",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DateUpdated",
                table: "Indices",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "TEXT",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AverageVolume",
                table: "StockData");

            migrationBuilder.DropColumn(
                name: "BollingerLower",
                table: "StockData");

            migrationBuilder.DropColumn(
                name: "BollingerUpper",
                table: "StockData");

            migrationBuilder.DropColumn(
                name: "EMA_14",
                table: "StockData");

            migrationBuilder.DropColumn(
                name: "FuturePrice",
                table: "StockData");

            migrationBuilder.DropColumn(
                name: "MACD",
                table: "StockData");

            migrationBuilder.AlterColumn<DateTime>(
                name: "DateUpdated",
                table: "Indices",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "TEXT");
        }
    }
}

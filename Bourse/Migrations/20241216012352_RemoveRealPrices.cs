using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bourse.Migrations
{
    /// <inheritdoc />
    public partial class RemoveRealPrices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RealPrices");

            migrationBuilder.AddColumn<string>(
                name: "ExchangeTimezoneShortName",
                table: "Indices",
                type: "TEXT",
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExchangeTimezoneShortName",
                table: "Indices");

            migrationBuilder.DropColumn(
                name: "RegularMarketDayHigh",
                table: "Indices");

            migrationBuilder.DropColumn(
                name: "RegularMarketDayLow",
                table: "Indices");

            migrationBuilder.CreateTable(
                name: "RealPrices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    IndiceId = table.Column<int>(type: "INTEGER", nullable: false),
                    Bourse = table.Column<string>(type: "TEXT", nullable: true),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Exchange = table.Column<string>(type: "TEXT", nullable: true),
                    ExchangeTimezoneName = table.Column<string>(type: "TEXT", nullable: true),
                    ExchangeTimezoneShortName = table.Column<string>(type: "TEXT", nullable: true),
                    QuoteType = table.Column<string>(type: "TEXT", nullable: true),
                    RegularMarketChange = table.Column<double>(type: "REAL", nullable: true),
                    RegularMarketChangePercent = table.Column<double>(type: "REAL", nullable: true),
                    RegularMarketDayHigh = table.Column<double>(type: "REAL", nullable: true),
                    RegularMarketDayLow = table.Column<double>(type: "REAL", nullable: true),
                    RegularMarketOpen = table.Column<double>(type: "REAL", nullable: true),
                    RegularMarketPreviousClose = table.Column<double>(type: "REAL", nullable: true),
                    RegularMarketPrice = table.Column<double>(type: "REAL", nullable: true),
                    RegularMarketVolume = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RealPrices", x => new { x.Id, x.IndiceId });
                    table.ForeignKey(
                        name: "FK_RealPrices_Indices_IndiceId",
                        column: x => x.IndiceId,
                        principalTable: "Indices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RealPrices_IndiceId",
                table: "RealPrices",
                column: "IndiceId");
        }
    }
}

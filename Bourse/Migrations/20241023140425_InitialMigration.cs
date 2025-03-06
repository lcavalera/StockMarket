using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bourse.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Indices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Symbol = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Indices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RealPrices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IndiceId = table.Column<int>(type: "INTEGER", nullable: false),
                    RegularMarketPrice = table.Column<double>(type: "REAL", nullable: false),
                    RegularMarketChange = table.Column<double>(type: "REAL", nullable: false),
                    RegularMarketChangePercent = table.Column<double>(type: "REAL", nullable: false),
                    RegularMarketPreviousClose = table.Column<double>(type: "REAL", nullable: false),
                    RegularMarketVolume = table.Column<long>(type: "INTEGER", nullable: false),
                    Market = table.Column<string>(type: "TEXT", nullable: false),
                    ExchangeTimezoneName = table.Column<string>(type: "TEXT", nullable: false),
                    ExchangeTimezoneShortName = table.Column<string>(type: "TEXT", nullable: false),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false)
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RealPrices");

            migrationBuilder.DropTable(
                name: "Indices");
        }
    }
}

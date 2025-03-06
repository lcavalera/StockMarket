using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bourse.Migrations
{
    /// <inheritdoc />
    public partial class AjoutProprieteRealPrice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Market",
                table: "RealPrices",
                newName: "QuoteType");

            migrationBuilder.AddColumn<string>(
                name: "Exchange",
                table: "RealPrices",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Exchange",
                table: "Indices",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExchangeTimezoneName",
                table: "Indices",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QuoteType",
                table: "Indices",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Region",
                table: "Indices",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Exchange",
                table: "RealPrices");

            migrationBuilder.DropColumn(
                name: "Exchange",
                table: "Indices");

            migrationBuilder.DropColumn(
                name: "ExchangeTimezoneName",
                table: "Indices");

            migrationBuilder.DropColumn(
                name: "QuoteType",
                table: "Indices");

            migrationBuilder.DropColumn(
                name: "Region",
                table: "Indices");

            migrationBuilder.RenameColumn(
                name: "QuoteType",
                table: "RealPrices",
                newName: "Market");
        }
    }
}

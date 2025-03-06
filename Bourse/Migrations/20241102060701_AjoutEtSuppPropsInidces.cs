using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bourse.Migrations
{
    /// <inheritdoc />
    public partial class AjoutEtSuppPropsInidces : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "RegularMarketDayLow",
                table: "Indices",
                newName: "RegularMarketPrice");

            migrationBuilder.RenameColumn(
                name: "RegularMarketDayHigh",
                table: "Indices",
                newName: "RegularMarketChange");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "RegularMarketPrice",
                table: "Indices",
                newName: "RegularMarketDayLow");

            migrationBuilder.RenameColumn(
                name: "RegularMarketChange",
                table: "Indices",
                newName: "RegularMarketDayHigh");
        }
    }
}

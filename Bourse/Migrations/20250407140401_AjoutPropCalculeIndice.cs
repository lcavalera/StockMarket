using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bourse.Migrations
{
    /// <inheritdoc />
    public partial class AjoutPropCalculeIndice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RegularMarketChange",
                table: "Indices");

            migrationBuilder.DropColumn(
                name: "RegularMarketChangePercent",
                table: "Indices");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "RegularMarketChange",
                table: "Indices",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "RegularMarketChangePercent",
                table: "Indices",
                type: "REAL",
                nullable: true);
        }
    }
}

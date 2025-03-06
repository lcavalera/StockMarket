using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bourse.Migrations
{
    /// <inheritdoc />
    public partial class AjoutPropsIndexPrediction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<float>(
                name: "CurrentPrice",
                table: "StockData",
                type: "REAL",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "TEXT");

            migrationBuilder.AddColumn<float>(
                name: "Probability",
                table: "Indices",
                type: "REAL",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "Score",
                table: "Indices",
                type: "REAL",
                nullable: false,
                defaultValue: 0f);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Probability",
                table: "Indices");

            migrationBuilder.DropColumn(
                name: "Score",
                table: "Indices");

            migrationBuilder.AlterColumn<decimal>(
                name: "CurrentPrice",
                table: "StockData",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(float),
                oldType: "REAL");
        }
    }
}

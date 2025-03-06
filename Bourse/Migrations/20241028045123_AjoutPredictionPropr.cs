using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bourse.Migrations
{
    /// <inheritdoc />
    public partial class AjoutPredictionPropr : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsIncreasing",
                table: "Indices",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "StockData",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CurrentPrice = table.Column<decimal>(type: "TEXT", nullable: false),
                    IsIncreasing = table.Column<bool>(type: "INTEGER", nullable: false),
                    IndiceId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockData", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockData_Indices_IndiceId",
                        column: x => x.IndiceId,
                        principalTable: "Indices",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_StockData_IndiceId",
                table: "StockData",
                column: "IndiceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StockData");

            migrationBuilder.DropColumn(
                name: "IsIncreasing",
                table: "Indices");
        }
    }
}

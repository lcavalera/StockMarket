using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bourse.Migrations
{
    /// <inheritdoc />
    public partial class AjoutPropsIndiceEarningsInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<float>(
                name: "SMA_14",
                table: "StockData",
                type: "REAL",
                nullable: false,
                defaultValue: 0f,
                oldClrType: typeof(float),
                oldType: "REAL",
                oldNullable: true);

            migrationBuilder.AlterColumn<float>(
                name: "RSI_14",
                table: "StockData",
                type: "REAL",
                nullable: false,
                defaultValue: 0f,
                oldClrType: typeof(float),
                oldType: "REAL",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EarningsInfoId",
                table: "Indices",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EarningsInfo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Company = table.Column<string>(type: "TEXT", nullable: false),
                    Symbol = table.Column<string>(type: "TEXT", nullable: false),
                    Url = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EarningsInfo", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Indices_EarningsInfoId",
                table: "Indices",
                column: "EarningsInfoId");

            migrationBuilder.AddForeignKey(
                name: "FK_Indices_EarningsInfo_EarningsInfoId",
                table: "Indices",
                column: "EarningsInfoId",
                principalTable: "EarningsInfo",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Indices_EarningsInfo_EarningsInfoId",
                table: "Indices");

            migrationBuilder.DropTable(
                name: "EarningsInfo");

            migrationBuilder.DropIndex(
                name: "IX_Indices_EarningsInfoId",
                table: "Indices");

            migrationBuilder.DropColumn(
                name: "EarningsInfoId",
                table: "Indices");

            migrationBuilder.AlterColumn<float>(
                name: "SMA_14",
                table: "StockData",
                type: "REAL",
                nullable: true,
                oldClrType: typeof(float),
                oldType: "REAL");

            migrationBuilder.AlterColumn<float>(
                name: "RSI_14",
                table: "StockData",
                type: "REAL",
                nullable: true,
                oldClrType: typeof(float),
                oldType: "REAL");
        }
    }
}

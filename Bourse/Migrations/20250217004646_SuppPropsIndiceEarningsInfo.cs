using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bourse.Migrations
{
    /// <inheritdoc />
    public partial class SuppPropsIndiceEarningsInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
                    Company = table.Column<string>(type: "TEXT", nullable: false),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
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
    }
}

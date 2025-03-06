using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bourse.Migrations
{
    /// <inheritdoc />
    public partial class AjoutDatePrevision : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "DatePrevision",
                table: "Indices",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DatePrevision",
                table: "Indices");
        }
    }
}

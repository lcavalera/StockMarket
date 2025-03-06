using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bourse.Migrations
{
    /// <inheritdoc />
    public partial class SuppScorePropIndices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Score",
                table: "Indices");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<float>(
                name: "Score",
                table: "Indices",
                type: "REAL",
                nullable: false,
                defaultValue: 0f);
        }
    }
}

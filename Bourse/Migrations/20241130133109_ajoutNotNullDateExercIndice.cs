using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bourse.Migrations
{
    /// <inheritdoc />
    public partial class ajoutNotNullDateExercIndice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "DatesExercicesFinancieres",
                table: "Indices",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "DatesExercicesFinancieres",
                table: "Indices",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);
        }
    }
}

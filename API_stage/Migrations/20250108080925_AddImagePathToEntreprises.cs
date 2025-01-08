using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API_stage.Migrations
{
    /// <inheritdoc />
    public partial class AddImagePathToEntreprises : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImagePath",
                table: "Entreprises",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImagePath",
                table: "Entreprises");
        }
    }
}

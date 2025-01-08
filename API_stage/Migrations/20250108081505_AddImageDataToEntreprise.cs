using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API_stage.Migrations
{
    /// <inheritdoc />
    public partial class AddImageDataToEntreprise : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImagePath",
                table: "Entreprises");

            migrationBuilder.AddColumn<byte[]>(
                name: "ImageData",
                table: "Entreprises",
                type: "bytea",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageData",
                table: "Entreprises");

            migrationBuilder.AddColumn<string>(
                name: "ImagePath",
                table: "Entreprises",
                type: "text",
                nullable: true);
        }
    }
}

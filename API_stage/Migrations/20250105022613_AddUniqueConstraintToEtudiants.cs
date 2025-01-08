using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API_stage.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueConstraintToEtudiants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Etudiants_Nom_Prenom_Contact",
                table: "Etudiants",
                columns: new[] { "Nom", "Prenom", "Contact" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Etudiants_Nom_Prenom_Contact",
                table: "Etudiants");
        }
    }
}

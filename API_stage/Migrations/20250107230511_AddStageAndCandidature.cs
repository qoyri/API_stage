using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace API_stage.Migrations
{
    /// <inheritdoc />
    public partial class AddStageAndCandidature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EtudiantId",
                table: "Stages");

            migrationBuilder.AlterColumn<string>(
                name: "Statut",
                table: "Stages",
                type: "text",
                nullable: false,
                defaultValue: "En attente",
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Stages",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Duree",
                table: "Stages",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Lieu",
                table: "Stages",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Titre",
                table: "Stages",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TypeContrat",
                table: "Stages",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "Candidatures",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EtudiantId = table.Column<int>(type: "integer", nullable: false),
                    StageId = table.Column<int>(type: "integer", nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    Statut = table.Column<string>(type: "text", nullable: false, defaultValue: "En attente")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Candidatures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Candidatures_Etudiants_EtudiantId",
                        column: x => x.EtudiantId,
                        principalTable: "Etudiants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Candidatures_Stages_StageId",
                        column: x => x.StageId,
                        principalTable: "Stages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Stages_EntrepriseId",
                table: "Stages",
                column: "EntrepriseId");

            migrationBuilder.CreateIndex(
                name: "IX_Candidatures_EtudiantId",
                table: "Candidatures",
                column: "EtudiantId");

            migrationBuilder.CreateIndex(
                name: "IX_Candidatures_StageId",
                table: "Candidatures",
                column: "StageId");

            migrationBuilder.AddForeignKey(
                name: "FK_Stages_Entreprises_EntrepriseId",
                table: "Stages",
                column: "EntrepriseId",
                principalTable: "Entreprises",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Stages_Entreprises_EntrepriseId",
                table: "Stages");

            migrationBuilder.DropTable(
                name: "Candidatures");

            migrationBuilder.DropIndex(
                name: "IX_Stages_EntrepriseId",
                table: "Stages");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Stages");

            migrationBuilder.DropColumn(
                name: "Duree",
                table: "Stages");

            migrationBuilder.DropColumn(
                name: "Lieu",
                table: "Stages");

            migrationBuilder.DropColumn(
                name: "Titre",
                table: "Stages");

            migrationBuilder.DropColumn(
                name: "TypeContrat",
                table: "Stages");

            migrationBuilder.AlterColumn<string>(
                name: "Statut",
                table: "Stages",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldDefaultValue: "En attente");

            migrationBuilder.AddColumn<int>(
                name: "EtudiantId",
                table: "Stages",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}

namespace API_stage.DTO;

public class StageDTO
{
    public int Id { get; set; }
    public string Titre { get; set; }
    public string Description { get; set; }
    public int EntrepriseId { get; set; }
    public string EntrepriseNom { get; set; } // Nom de l'entreprise
    public string Lieu { get; set; }
    public string Duree { get; set; }
    public string TypeContrat { get; set; }
    public DateTime DateDebut { get; set; }
    public DateTime DateFin { get; set; }
    public string Statut { get; set; }
}
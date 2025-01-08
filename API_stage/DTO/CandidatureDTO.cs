namespace API_stage.DTO;

public class CandidatureDTO
{
    public int Id { get; set; }
    public int EtudiantId { get; set; }
    public string EtudiantNom { get; set; } // Nom complet de l'étudiant
    public string EtudiantContact { get; set; } // Contact de l'étudiant
    public int StageId { get; set; }
    public string StageTitre { get; set; } // Titre du stage
    public string Message { get; set; }
    public string Statut { get; set; }
}
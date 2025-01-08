namespace API_stage.Models
{
    public class Candidature
    {
        public int Id { get; set; }
        public int EtudiantId { get; set; } // Lien avec l'étudiant
        public Etudiant Etudiant { get; set; } // Relation avec Etudiant
        public int StageId { get; set; } // Lien avec le stage
        public Stage Stage { get; set; } // Relation avec Stage
        public string Message { get; set; } // Message de la candidature
        public string Statut { get; set; } // "En attente", "Acceptée", "Refusée"
    }
}
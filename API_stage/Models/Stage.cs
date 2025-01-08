namespace API_stage.Models
{
    public class Stage
    {
        public int Id { get; set; }
        public string Titre { get; set; } // Titre du stage
        public string Description { get; set; } // Description du stage
        public int EntrepriseId { get; set; } // Lien avec Entreprise
        public Entreprise Entreprise { get; set; } // Relation avec Entreprise
        public string Lieu { get; set; } // Lieu du stage
        public string Duree { get; set; } // Durée du stage (ex. : "6 mois")
        public string TypeContrat { get; set; } // "Stage" / "Alternance"
        public DateTime DateDebut { get; set; } // Début du stage
        public DateTime DateFin { get; set; } // Fin du stage
        public string Statut { get; set; } // "En attente", "Accepté", "Refusé", etc.
        public ICollection<Candidature> Candidatures { get; set; } // Candidatures liées au stage
    }
}
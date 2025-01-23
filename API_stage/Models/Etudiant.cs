using Microsoft.EntityFrameworkCore;

namespace API_stage.Models
{
    [Index(nameof(Nom), nameof(Prenom), nameof(Contact), IsUnique = true)]
    public class Etudiant
    {
        public int Id { get; set; }
        public string Nom { get; set; }
        public string Prenom { get; set; }
        public string Contact { get; set; }
        public string Promo { get; set; } // SIO ou SNIR
        public string ReseauxSociaux { get; set; } // JSON ou une structure adaptée

        public byte[]? ImageData { get; set; } // Image complète
        public byte[]? ThumbnailData { get; set; } // Miniature

        public User? User { get; set; }
        public ICollection<Candidature>? Candidatures { get; set; } // Liste des candidatures faites par l'étudiant
    }
}
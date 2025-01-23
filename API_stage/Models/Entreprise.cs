namespace API_stage.Models
{
    public class Entreprise
    {
        public int Id { get; set; }
        public string Nom { get; set; }
        public string Adresse { get; set; }
        public string Contact { get; set; }
        public string Description { get; set; }
        public byte[]? ImageData { get; set; } // Image complète
        public byte[]? ThumbnailData { get; set; } // Nouvelle miniature

        public ICollection<Stage> Stages { get; set; } // Liste des stages proposés par l'entreprise

        public ICollection<Conversation> Conversations { get; set; } // Ajouté
    }
}
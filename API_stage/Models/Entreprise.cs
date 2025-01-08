namespace API_stage.Models
{
    public class Entreprise
    {
        public int Id { get; set; }
        public string Nom { get; set; }
        public string Adresse { get; set; }
        public string Contact { get; set; }
        public string Description { get; set; }
        public byte[]? ImageData { get; set; } // Nouveau champ pour stocker l'image binaire


        public ICollection<Stage> Stages { get; set; } // Liste des stages proposés par l'entreprise
    }
}
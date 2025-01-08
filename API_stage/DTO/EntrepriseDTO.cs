namespace API_stage.DTO
{
    public class EntrepriseDTO
    {
        public int Id { get; set; }
        public string Nom { get; set; }
        public string Adresse { get; set; }
        public string Contact { get; set; }
        public string Description { get; set; }
        public string? ImageData { get; set; } // Chaîne encodée en Base64
    }
}
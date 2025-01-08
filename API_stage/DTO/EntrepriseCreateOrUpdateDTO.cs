namespace API_stage.DTO
{
    public class EntrepriseCreateOrUpdateDTO
    {
        public string Nom { get; set; }
        public string Adresse { get; set; }
        public string Contact { get; set; }

        public string Description { get; set; }
        // Pas de ImageData ici, car il est calculé à partir de imageFile
    }
}
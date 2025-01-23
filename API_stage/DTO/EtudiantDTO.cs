namespace API_stage.DTO;

public class EtudiantDto
{
    public int Id { get; set; }
    public string Nom { get; set; }
    public string Prenom { get; set; }
    public string Contact { get; set; }
    public string Promo { get; set; }
    public string ReseauxSociaux { get; set; }

    public string Username { get; set; }
    public string? ThumbnailData { get; set; } // Chaîne encodée en Base64 pour la miniature
}
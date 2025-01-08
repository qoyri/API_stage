namespace API_stage.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public int RoleId { get; set; } // Lié à la table Role
        public DateTime CreatedAt { get; set; }

        public Role Role { get; set; } // Relation avec Role

        public int? EtudiantId { get; set; } // Nullable pour d'autres types d'utilisateurs

        public Etudiant? Etudiant { get; set; }
    }
}
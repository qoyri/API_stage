namespace API_stage.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public int RoleId { get; set; } // Lié à la table Role
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // Valeur par défaut

        public Role Role { get; set; } // Relation avec Role

        public int? EtudiantId { get; set; } // Nullable pour d'autres types d'utilisateurs
        public Etudiant? Etudiant { get; set; }

        public int? EntrepriseId { get; set; } // Nullable pour permettre le lien avec une entreprise
        public Entreprise? Entreprise { get; set; } // Relation

        // Navigation inverse (facultatif, pour des besoins spécifiques)
        public ICollection<Message> Messages { get; set; } // Messages envoyés par cet utilisateur
        public ICollection<Conversation> ConversationsAsParticipant1 { get; set; }
        public ICollection<Conversation> ConversationsAsParticipant2 { get; set; }
    }
}
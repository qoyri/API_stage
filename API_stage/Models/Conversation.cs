namespace API_stage.Models
{
    public class Conversation
    {
        public int Id { get; set; }

        // Deux participants génériques, reliés à la table User
        public int Participant1Id { get; set; }
        public User Participant1 { get; set; } // Navigation vers le premier utilisateur

        public int Participant2Id { get; set; }
        public User Participant2 { get; set; } // Navigation vers le deuxième utilisateur

        // Collection des messages liés à la conversation
        public ICollection<Message> Messages { get; set; }
    }

    public class Message
    {
        public int Id { get; set; }

        // Clé étrangère vers la conversation
        public int ConversationId { get; set; }
        public Conversation Conversation { get; set; } // Navigation vers la conversation

        // Clé étrangère vers l'utilisateur qui a envoyé le message
        public int EnvoyeurId { get; set; }
        public User Envoyeur { get; set; } // Navigation vers l'utilisateur envoyeur

        public string Contenu { get; set; } // Le contenu du message
        public DateTime DateEnvoi { get; set; } // Date et heure de l'envoi
    }
}
namespace API_stage.Models
{
    public class Role
    {
        public int Id { get; set; } // Exemple : 1 = Admin; 2 = Étudiant
        public string Name { get; set; }

        public ICollection<User> Users { get; set; } // Relation avec User
    }
}
using API_stage.Models;
using Microsoft.EntityFrameworkCore;

namespace API_stage.Models
{
    public class StageDbContext : DbContext
    {
        public StageDbContext(DbContextOptions<StageDbContext> options) : base(options)
        {
        }

        public DbSet<Etudiant> Etudiants { get; set; }
        public DbSet<Entreprise> Entreprises { get; set; }
        public DbSet<Stage> Stages { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Candidature> Candidatures { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuration pour la table Stages
            modelBuilder.Entity<Stage>(entity =>
            {
                entity.HasKey(s => s.Id); // Clé primaire
                entity.Property(s => s.Titre).IsRequired().HasMaxLength(100);
                entity.Property(s => s.TypeContrat).IsRequired();
                entity.Property(s => s.Statut).HasDefaultValue("En attente");
                entity.HasOne(s => s.Entreprise)
                    .WithMany(e => e.Stages)
                    .HasForeignKey(s => s.EntrepriseId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configuration pour la table Candidatures
            modelBuilder.Entity<Candidature>(entity =>
            {
                entity.HasKey(c => c.Id); // Clé primaire
                entity.Property(c => c.Statut).HasDefaultValue("En attente");
                entity.HasOne(c => c.Etudiant)
                    .WithMany(e => e.Candidatures)
                    .HasForeignKey(c => c.EtudiantId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(c => c.Stage)
                    .WithMany(s => s.Candidatures)
                    .HasForeignKey(c => c.StageId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
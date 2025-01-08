using API_stage.DTO;
using API_stage.Method;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using API_stage.Models;
using API_stage.Request;

[ApiController]
[Route("api/v1/[controller]")]
public class EtudiantsController : ControllerBase
{
    private readonly StageDbContext _context;

    public EtudiantsController(StageDbContext context)
    {
        _context = context;
    }

    // LISTE DES ÉTUDIANTS (Accessible par Admin uniquement)
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<Etudiant>>> GetEtudiants()
    {
        return await _context.Etudiants.ToListAsync();
    }

    // CRÉATION D'UN ÉTUDIANT (Accessible par Admin uniquement)
    [HttpPost("create-etudiant")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateEtudiant([FromBody] CreateEtudiantRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { message = "Requête invalide", errors = ModelState });
        }

        // Vérifiez l'existence d'un étudiant similaire
        var existingEtudiant = await _context.Etudiants
            .FirstOrDefaultAsync(e =>
                e.Nom.ToLower() == request.Nom.ToLower() &&
                e.Prenom.ToLower() == request.Prenom.ToLower() &&
                e.Contact == request.Contact
            );

        if (existingEtudiant != null)
        {
            return BadRequest(new
            {
                message = "Un étudiant similaire existe déjà.",
                etudiantExist = new
                {
                    existingEtudiant.Id,
                    existingEtudiant.Nom,
                    existingEtudiant.Prenom,
                    existingEtudiant.Contact
                }
            });
        }

        // Création de l'étudiant s'il n'existe pas
        var etudiant = new Etudiant
        {
            Nom = request.Nom,
            Prenom = request.Prenom,
            Contact = request.Contact,
            Promo = request.Promo,
            ReseauxSociaux = request.ReseauxSociaux
        };

        _context.Etudiants.Add(etudiant);
        await _context.SaveChangesAsync();

        var defaultPassword = GenerateRandomPassword.GenerateRandomPasswords(); // Générer un mot de passe
        var hashedPassword = GenerateRandomPassword.HashPassword(defaultPassword); // Hasher le mot de passe

        var user = new User
        {
            Username = $"{etudiant.Prenom.ToLower()}.{etudiant.Nom.ToLower()}",
            Email = request.Contact,
            Password = hashedPassword,
            RoleId = 2, // Rôle Étudiant
            EtudiantId = etudiant.Id
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Préparation de la réponse
        var response = new CreateEtudiantResponse
        {
            Message = "Étudiant créé avec succès.",
            Etudiant = new EtudiantDto
            {
                Id = etudiant.Id,
                Nom = etudiant.Nom,
                Prenom = etudiant.Prenom,
                Contact = etudiant.Contact,
                Promo = etudiant.Promo,
                ReseauxSociaux = etudiant.ReseauxSociaux,
                Username = user.Username
            },
            Identifiants = new IdentifiantsDto
            {
                Username = user.Username,
                Password = defaultPassword
            }
        };

        return Ok(response);
    }

    public class CreateEtudiantResponse
    {
        /// <summary>
        /// Message de confirmation.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Détails sur l'étudiant créé.
        /// </summary>
        public EtudiantDto Etudiant { get; set; }

        /// <summary>
        /// Identifiants générés pour le compte utilisateur de l'étudiant.
        /// </summary>
        public IdentifiantsDto Identifiants { get; set; }
    }

    // MODIFICATION DU PROFIL ÉTUDIANT (Étudiant connecté uniquement)
    [HttpPut("edit-profile")]
    [Authorize(Roles = "Etudiant")]
    public async Task<ActionResult> EditProfile([FromBody] EditProfileRequest request)
    {
        // Récupérer l'étudiant connecté via son username (stocké dans le token JWT)
        var username = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        // Trouver l'étudiant correspondant dans la base de données
        var etudiant = await _context.Etudiants.FirstOrDefaultAsync(e => e.Contact == username);
        if (etudiant == null)
        {
            return NotFound(new { message = "Profil de l'étudiant introuvable." });
        }

        // Mettre à jour les champs autorisés
        etudiant.Nom = request.Nom ?? etudiant.Nom;
        etudiant.Prenom = request.Prenom ?? etudiant.Prenom;
        etudiant.Promo = request.Promo ?? etudiant.Promo;
        etudiant.ReseauxSociaux = request.ReseauxSociaux ?? etudiant.ReseauxSociaux;

        await _context.SaveChangesAsync();
        return Ok(etudiant);
    }

    // DELETE /api/v1/etudiants/{id}
    // Accessible uniquement par les Admins
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteEtudiant(int id)
    {
        // Rechercher l'étudiant
        var etudiant = await _context.Etudiants.FindAsync(id);
        if (etudiant == null)
        {
            return NotFound($"L'étudiant avec l'ID {id} n'existe pas.");
        }

        // Vérifier s'il existe un compte utilisateur lié à cet étudiant
        var user = await _context.Users.FirstOrDefaultAsync(u => u.EtudiantId == id);
        if (user != null)
        {
            _context.Users.Remove(user); // Supprimer le compte utilisateur associé
        }

        // Supprimer l'étudiant
        _context.Etudiants.Remove(etudiant);
        await _context.SaveChangesAsync();

        return Ok($"L'étudiant avec l'ID {id} a été supprimé avec succès.");
    }
}
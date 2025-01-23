using API_stage.DTO;
using API_stage.Method;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using API_stage.Models;
using API_stage.Request;
using SixLabors.ImageSharp.Processing;

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
    [Authorize(Roles = "Admin,Entreprise")]
    public async Task<ActionResult> GetEtudiantsPaginated(int page = 1, int pageSize = 10)
    {
        // Validation des paramètres
        if (page < 1 || pageSize < 1)
        {
            return BadRequest("Les paramètres page et pageSize doivent être supérieurs à zéro.");
        }

        // Récupération paginée des étudiants
        var etudiants = await _context.Etudiants
            .Skip((page - 1) * pageSize) // Ignorer les enregistrements des pages précédentes
            .Take(pageSize) // Prendre seulement pageSize enregistrements
            .ToListAsync();

        // Préparer les données sous forme d'objets DTO
        var etudiantsDTO = etudiants.Select(e => new
        {
            Id = e.Id,
            Nom = e.Nom,
            Prenom = e.Prenom,
            Contact = e.Contact,
            Promo = e.Promo,
            ReseauxSociaux = e.ReseauxSociaux,

            // Optionnel : si vous voulez ajouter les miniatures
            ImageData = e.ThumbnailData != null ? Convert.ToBase64String(e.ThumbnailData) : null
        }).ToList();

        // Ajout d'un objet de pagination à la réponse
        var totalEtudiants = await _context.Etudiants.CountAsync();
        var response = new
        {
            TotalItems = totalEtudiants,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalEtudiants / (double)pageSize),
            Data = etudiantsDTO
        };

        return Ok(response);
    }

    // CRÉATION D'UN ÉTUDIANT (Accessible par Admin uniquement)
    [HttpPost("create-etudiant")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateEtudiant([FromForm] CreateEtudiantRequest request, IFormFile? imageFile)
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

        // Gérer l'image
        byte[]? imageData = null;
        byte[]? thumbnailData = null;
        if (imageFile != null)
        {
            using (var memoryStream = new MemoryStream())
            {
                await imageFile.CopyToAsync(memoryStream);
                imageData = memoryStream.ToArray();
                thumbnailData = CreateThumbnail(imageData); // Générer la miniature
            }
        }

        // Création de l'étudiant
        var etudiant = new Etudiant
        {
            Nom = request.Nom,
            Prenom = request.Prenom,
            Contact = request.Contact,
            Promo = request.Promo,
            ReseauxSociaux = request.ReseauxSociaux,
            ImageData = imageData,
            ThumbnailData = thumbnailData
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
            EtudiantId = etudiant.Id,
            CreatedAt = DateTime.UtcNow // Assigner une date de création explicite
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
    public async Task<ActionResult> EditProfile([FromForm] EditProfileRequest request, IFormFile? imageFile)
    {
        // Récupérer l'étudiant connecté via son username (stocké dans le token JWT)
        var username = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        // Trouver l'étudiant correspondant dans la base de données
        var etudiant = await _context.Etudiants.FirstOrDefaultAsync(e => e.Contact == username);
        if (etudiant == null)
        {
            return NotFound(new { message = "Profil de l'étudiant introuvable." });
        }

        // Mise à jour des champs autorisés
        etudiant.Nom = request.Nom ?? etudiant.Nom;
        etudiant.Prenom = request.Prenom ?? etudiant.Prenom;
        etudiant.Promo = request.Promo ?? etudiant.Promo;
        etudiant.ReseauxSociaux = request.ReseauxSociaux ?? etudiant.ReseauxSociaux;

        // Mise à jour de l'image
        if (imageFile != null)
        {
            using (var memoryStream = new MemoryStream())
            {
                await imageFile.CopyToAsync(memoryStream);
                var imageData = memoryStream.ToArray();

                // Mettre à jour l'image complète et la miniature
                etudiant.ImageData = imageData;
                etudiant.ThumbnailData = CreateThumbnail(imageData);
            }
        }

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
    
    [Authorize(Roles = "Admin,Etudiant")]
    [HttpGet("{id}/image")]
    public async Task<ActionResult> GetEtudiantImage(int id)
    {
        var etudiant = await _context.Etudiants.FindAsync(id);

        if (etudiant == null || etudiant.ImageData == null)
        {
            return NotFound("Image non disponible pour cet étudiant.");
        }

        return File(etudiant.ImageData, "image/jpeg");
    }

    [Authorize(Roles = "Admin,Etudiant")]
    [HttpGet("{id}")]
    public async Task<ActionResult> GetEtudiantById(int id)
    {
        // Récupérer l'étudiant par ID
        var etudiant = await _context.Etudiants.FindAsync(id);

        if (etudiant == null)
        {
            return NotFound($"L'étudiant avec l'ID {id} n'existe pas.");
        }

        // Convertir en DTO pour retourner des données pertinentes
        var etudiantDTO = new EtudiantDto
        {
            Id = etudiant.Id,
            Nom = etudiant.Nom,
            Prenom = etudiant.Prenom,
            Contact = etudiant.Contact,
            Promo = etudiant.Promo,
            ReseauxSociaux = etudiant.ReseauxSociaux,
            ThumbnailData = etudiant.ThumbnailData != null ? Convert.ToBase64String(etudiant.ThumbnailData) : null
        };

        return Ok(etudiantDTO);
    }
    
    private byte[] CreateThumbnail(byte[] originalImage, int targetHeight = 180)
    {
        using (var inputStream = new MemoryStream(originalImage))
        using (var image = SixLabors.ImageSharp.Image.Load(inputStream)) // Charger l'image d'origine
        {
            // Calculer la nouvelle largeur en maintenant le ratio d'aspect
            var aspectRatio = (double)image.Width / image.Height;
            var targetWidth = (int)(targetHeight * aspectRatio); // Redimensionner proportionnellement

            // Redimensionner l'image avec la hauteur cible et la largeur calculée
            image.Mutate(x => x.Resize(targetWidth, targetHeight));

            using (var outputStream = new MemoryStream())
            {
                // Enregistrer l'image redimensionnée au format JPEG
                image.Save(outputStream, new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder
                {
                    Quality = 75 // Ajuster la qualité pour optimiser la taille
                });
                return outputStream.ToArray(); // Retourner l'image en tant que tableau d'octets
            }
        }
    }
}
using System.Net.Mime;
using API_stage.Models;
using API_stage.DTO;
using API_stage.Method;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace API_stage.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class EntrepriseController : ControllerBase
    {
        private readonly StageDbContext _context;

        public EntrepriseController(StageDbContext context)
        {
            _context = context;
        }

        // GET /api/v1/entreprises
        [Authorize(Roles = "Admin,Etudiant")]
        [HttpGet]
        public async Task<ActionResult> GetEntreprises(int page = 1, int pageSize = 10)
        {
            // Validation des paramètres
            if (page < 1 || pageSize < 1)
            {
                return BadRequest("Les paramètres page et pageSize doivent être supérieurs à zéro.");
            }

            // Récupération paginée des entreprises
            var entreprises = await _context.Entreprises
                .Skip((page - 1) * pageSize) // Ignorer les enregistrements des pages précédentes
                .Take(pageSize) // Prendre seulement pageSize enregistrements
                .ToListAsync();

            // Préparer les données sous forme d'objets DTO (avec miniatures seulement)
            var entreprisesDTO = entreprises.Select(e => new EntrepriseDTO
            {
                Id = e.Id,
                Nom = e.Nom,
                Adresse = e.Adresse,
                Contact = e.Contact,
                Description = e.Description,

                // Retourner uniquement la miniature
                ImageData = e.ThumbnailData != null
                    ? Convert.ToBase64String(e.ThumbnailData)
                    : null
            }).ToList();

            // Ajout d'un objet de pagination à la réponse
            var totalEntreprises = await _context.Entreprises.CountAsync();
            var response = new
            {
                TotalItems = totalEntreprises,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalEntreprises / (double)pageSize),
                Data = entreprisesDTO
            };

            return Ok(response);
        }

        // Endpoint d'administration accessible uniquement aux administrateurs
        // POST /api/v1/entreprises/create-Entreprise
        [Authorize(Roles = "Admin")]
        [HttpPost("create-Entreprise")]
        public async Task<ActionResult<Entreprise>> CreateEntreprise(
            [FromForm] EntrepriseCreateOrUpdateDTO entrepriseDto,
            IFormFile? imageFile
        )
        {
            if (string.IsNullOrEmpty(entrepriseDto.Nom) ||
                string.IsNullOrEmpty(entrepriseDto.Contact) ||
                string.IsNullOrEmpty(entrepriseDto.Adresse))
            {
                return BadRequest("Les champs Nom, Adresse et Contact sont obligatoires.");
            }

            // Gestion de l'image
            byte[]? imageData = null;
            byte[]? thumbnailData = null;

            if (imageFile != null)
            {
                using (var memoryStream = new MemoryStream())
                {
                    await imageFile.CopyToAsync(memoryStream);
                    imageData = memoryStream.ToArray();
                    thumbnailData = CreateThumbnail(imageData); // Générer une miniature
                }
            }

            // Création de l'entreprise
            var entreprise = new Entreprise
            {
                Nom = entrepriseDto.Nom,
                Adresse = entrepriseDto.Adresse,
                Contact = entrepriseDto.Contact,
                Description = entrepriseDto.Description,
                ImageData = imageData, // Stocker l'image complète
                ThumbnailData = thumbnailData // Stocker la miniature
            };

            _context.Entreprises.Add(entreprise);
            await _context.SaveChangesAsync();

            // Générer un mot de passe aléatoire
            var defaultPassword = GenerateRandomPassword.GenerateRandomPasswords(); // Générer un mot de passe aléatoire
            var hashedPassword = GenerateRandomPassword.HashPassword(defaultPassword); // Hasher le mot de passe

            // Créer un utilisateur pour l'entreprise
            var user = new User
            {
                Username = entreprise.Nom.Replace(" ", "").ToLower(), // Utiliser le nom de l'entreprise sans espaces
                Email = entreprise.Contact,
                Password = hashedPassword,
                RoleId = 3, // Supposons que 3 est le rôle "Entreprise"
                EntrepriseId = entreprise.Id
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Préparation de la réponse avec les identifiants de connexion
            var response = new
            {
                message = "Entreprise créée avec succès, et un compte utilisateur a été généré.",
                entreprise = new EntrepriseDTO
                {
                    Id = entreprise.Id,
                    Nom = entreprise.Nom,
                    Adresse = entreprise.Adresse,
                    Contact = entreprise.Contact,
                    Description = entreprise.Description,
                    ImageData = thumbnailData != null ? Convert.ToBase64String(thumbnailData) : null
                },
                identifiants = new
                {
                    Username = user.Username,
                    Password = defaultPassword
                }
            };

            return CreatedAtAction(nameof(GetEntreprises), new { id = entreprise.Id }, response);
        }

        // PUT /api/v1/entreprises/{id}
        // Accessible uniquement par les Admins
        // PUT /api/v1/entreprises/edit-Entreprise/{id}
        [Authorize(Roles = "Admin")]
        [HttpPut("edit-Entreprise/{id:int}")]
        public async Task<IActionResult> UpdateEntreprise(
            int id,
            [FromForm] EntrepriseCreateOrUpdateDTO entrepriseDto,
            IFormFile? imageFile
        )
        {
            var existingEntreprise = await _context.Entreprises.FindAsync(id);
            if (existingEntreprise == null)
            {
                return NotFound($"L'entreprise avec l'ID {id} n'existe pas.");
            }

            // Mise à jour des champs textuels
            existingEntreprise.Nom = entrepriseDto.Nom;
            existingEntreprise.Adresse = entrepriseDto.Adresse;
            existingEntreprise.Contact = entrepriseDto.Contact;
            existingEntreprise.Description = entrepriseDto.Description;

            // Mise à jour de l'image si un fichier est fourni
            if (imageFile != null)
            {
                using (var memoryStream = new MemoryStream())
                {
                    await imageFile.CopyToAsync(memoryStream);
                    var imageData = memoryStream.ToArray();

                    // Mettre à jour l'image complète
                    existingEntreprise.ImageData = imageData;

                    // Régénérer la miniature
                    existingEntreprise.ThumbnailData = CreateThumbnail(imageData);
                }
            }

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE /api/v1/entreprises/{id}
        // Accessible uniquement par les Admins
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEntreprise(int id)
        {
            var entreprise = await _context.Entreprises.FindAsync(id);

            if (entreprise == null)
            {
                return NotFound($"L'entreprise avec l'ID {id} n'existe pas.");
            }

            // Vérifier s'il existe un compte utilisateur lié à cette entreprise
            var user = await _context.Users.FirstOrDefaultAsync(u => u.EntrepriseId == id);
            if (user != null)
            {
                _context.Users.Remove(user); // Supprimer le compte utilisateur associé
            }

            // Supprimer l'entreprise
            _context.Entreprises.Remove(entreprise);
            await _context.SaveChangesAsync();

            return Ok($"L'entreprise avec l'ID {id} et son compte utilisateur associé ont été supprimés avec succès.");
        }

        [Authorize(Roles = "Admin,Etudiant")]
        [HttpGet("{id}/image")]
        public async Task<ActionResult> GetEntrepriseImage(int id)
        {
            var entreprise = await _context.Entreprises.FindAsync(id);

            if (entreprise == null || entreprise.ImageData == null)
            {
                return NotFound("Image non disponible pour cette entreprise.");
            }

            return File(entreprise.ImageData, "image/jpeg");
        }

        [Authorize(Roles = "Admin,Etudiant")]
        [HttpGet("{id}")]
        public async Task<ActionResult> GetEntrepriseById(int id)
        {
            // Récupérer l'entreprise par ID
            var entreprise = await _context.Entreprises.FindAsync(id);

            if (entreprise == null)
            {
                return NotFound($"L'entreprise avec l'ID {id} n'existe pas.");
            }

            // Récupérer l'utilisateur lié à l'entreprise
            var user = await _context.Users.FirstOrDefaultAsync(u => u.EntrepriseId == id);

            // Convertir en DTO
            var entrepriseDTO = new EntrepriseDTO
            {
                Id = entreprise.Id,
                Nom = entreprise.Nom,
                Adresse = entreprise.Adresse,
                Contact = entreprise.Contact,
                Description = entreprise.Description,
                ImageData = entreprise.ImageData != null ? Convert.ToBase64String(entreprise.ImageData) : null,
                UserId = user?.Id // Ajouter le UserId
            };

            return Ok(entrepriseDTO);
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
}
using API_stage.Models;
using API_stage.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
        public async Task<ActionResult<IEnumerable<EntrepriseDTO>>> GetEntreprises()
        {
            var entreprises = await _context.Set<Entreprise>().ToListAsync();

            var entreprisesDTO = entreprises.Select(e => new EntrepriseDTO
            {
                Id = e.Id,
                Nom = e.Nom,
                Adresse = e.Adresse,
                Contact = e.Contact,
                Description = e.Description,
                ImageData = e.ImageData != null ? Convert.ToBase64String(e.ImageData) : null
            }).ToList();

            return Ok(entreprisesDTO);
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

            byte[]? imageData = null;
            if (imageFile != null)
            {
                using (var memoryStream = new MemoryStream())
                {
                    await imageFile.CopyToAsync(memoryStream);
                    imageData = memoryStream.ToArray();
                }
            }

            var entreprise = new Entreprise
            {
                Nom = entrepriseDto.Nom,
                Adresse = entrepriseDto.Adresse,
                Contact = entrepriseDto.Contact,
                Description = entrepriseDto.Description,
                ImageData = imageData // Stocker les données binaires dans le champ ImageData
            };

            _context.Entreprises.Add(entreprise);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetEntreprises), new { id = entreprise.Id }, entreprise);
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
                    existingEntreprise.ImageData = memoryStream.ToArray();
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

            _context.Entreprises.Remove(entreprise);
            await _context.SaveChangesAsync();

            return Ok($"L'entreprise avec l'ID {id} a été supprimée avec succès.");
        }
    }
}
using API_stage.Models;
using API_stage.DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API_stage.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StagesController : ControllerBase
    {
        private readonly StageDbContext _context;

        public StagesController(StageDbContext context)
        {
            _context = context;
        }

        // 1. Créer un Stage
        [HttpPost]
        public async Task<ActionResult<StageDTO>> CreateStage([FromBody] StageDTO stageDto)
        {
            var stage = new Stage
            {
                Titre = stageDto.Titre,
                Description = stageDto.Description,
                EntrepriseId = stageDto.EntrepriseId,
                Lieu = stageDto.Lieu,
                Duree = stageDto.Duree,
                TypeContrat = stageDto.TypeContrat,
                DateDebut = stageDto.DateDebut,
                DateFin = stageDto.DateFin,
                Statut = stageDto.Statut
            };

            _context.Stages.Add(stage);
            await _context.SaveChangesAsync();

            // Mappez vers DTO pour la réponse
            var responseDto = new StageDTO
            {
                Id = stage.Id,
                Titre = stage.Titre,
                Description = stage.Description,
                EntrepriseId = stage.EntrepriseId,
                Lieu = stage.Lieu,
                Duree = stage.Duree,
                TypeContrat = stage.TypeContrat,
                DateDebut = stage.DateDebut,
                DateFin = stage.DateFin,
                Statut = stage.Statut
            };

            return CreatedAtAction(nameof(GetStageById), new { id = stage.Id }, responseDto);
        }

        // 2. Récupérer Tous les Stages (avec filtres)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<StageDTO>>> GetStages([FromQuery] string? statut,
            [FromQuery] int? entreprise_id, [FromQuery] int? etudiant_id)
        {
            var query = _context.Stages.Include(s => s.Entreprise).AsQueryable();

            // Appliquer les filtres
            if (!string.IsNullOrEmpty(statut))
            {
                query = query.Where(s => s.Statut == statut);
            }

            if (entreprise_id.HasValue)
            {
                query = query.Where(s => s.EntrepriseId == entreprise_id);
            }

            if (etudiant_id.HasValue)
            {
                query = query.Where(s => s.Candidatures.Any(c => c.EtudiantId == etudiant_id));
            }

            // Projeter les résultats dans StageDto
            var stages = await query.Select(s => new StageDTO
            {
                Id = s.Id,
                Titre = s.Titre,
                Description = s.Description,
                EntrepriseId = s.EntrepriseId,
                EntrepriseNom = s.Entreprise.Nom,
                Lieu = s.Lieu,
                Duree = s.Duree,
                TypeContrat = s.TypeContrat,
                DateDebut = s.DateDebut,
                DateFin = s.DateFin,
                Statut = s.Statut
            }).ToListAsync();

            return Ok(stages);
        }

        // 3. Candidater à un Stage
        [HttpPost("{stageId}/candidatures")]
        public async Task<IActionResult> Postuler(int stageId, [FromBody] CandidatureDTO candidatureDto)
        {
            var stage = await _context.Stages.FindAsync(stageId);
            if (stage == null)
            {
                return NotFound($"Le stage avec l'ID {stageId} n'existe pas.");
            }

            var etudiant = await _context.Etudiants.FindAsync(candidatureDto.EtudiantId);
            if (etudiant == null)
            {
                return NotFound($"L'étudiant avec l'ID {candidatureDto.EtudiantId} n'existe pas.");
            }

            var candidature = new Candidature
            {
                StageId = stageId,
                EtudiantId = candidatureDto.EtudiantId,
                Message = candidatureDto.Message,
                Statut = "En attente"
            };

            _context.Candidatures.Add(candidature);
            await _context.SaveChangesAsync();

            return Ok($"L'étudiant {etudiant.Nom} a postulé pour le stage {stage.Titre}.");
        }

        // 4. Récupérer les Détails d’un Stage
        [HttpGet("{id}")]
        public async Task<ActionResult<StageDTO>> GetStageById(int id)
        {
            var stage = await _context.Stages
                .Include(s => s.Entreprise)
                .Include(s => s.Candidatures)
                .ThenInclude(c => c.Etudiant) // Inclut les étudiants liés aux candidatures
                .FirstOrDefaultAsync(s => s.Id == id);

            if (stage == null)
            {
                return NotFound($"Le stage avec l'ID {id} n'existe pas.");
            }

            var stageDto = new StageDTO
            {
                Id = stage.Id,
                Titre = stage.Titre,
                Description = stage.Description,
                EntrepriseId = stage.EntrepriseId,
                EntrepriseNom = stage.Entreprise.Nom,
                Lieu = stage.Lieu,
                Duree = stage.Duree,
                TypeContrat = stage.TypeContrat,
                DateDebut = stage.DateDebut,
                DateFin = stage.DateFin,
                Statut = stage.Statut
            };

            return Ok(stageDto);
        }

        // 5. Modifier un Stage
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateStage(int id, [FromBody] StageDTO stageDto)
        {
            var stage = await _context.Stages.FindAsync(id);
            if (stage == null)
            {
                return NotFound($"Le stage avec l'ID {id} n'existe pas.");
            }

            stage.Titre = stageDto.Titre;
            stage.Description = stageDto.Description;
            stage.Lieu = stageDto.Lieu;
            stage.DateDebut = stageDto.DateDebut;
            stage.DateFin = stageDto.DateFin;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // 6. Supprimer un Stage
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteStage(int id)
        {
            var stage = await _context.Stages.FindAsync(id);
            if (stage == null)
            {
                return NotFound($"Le stage avec l'ID {id} n'existe pas.");
            }

            _context.Stages.Remove(stage);
            await _context.SaveChangesAsync();

            return Ok($"Le stage avec l'ID {id} a été supprimé.");
        }

        // 7. Changer le Statut d’un Stage
        [HttpPatch("{id}/statut")]
        public async Task<IActionResult> UpdateStageStatut(int id, [FromBody] string statut)
        {
            var stage = await _context.Stages.FindAsync(id);
            if (stage == null)
            {
                return NotFound($"Le stage avec l'ID {id} n'existe pas.");
            }

            stage.Statut = statut;
            await _context.SaveChangesAsync();

            return Ok($"Le statut du stage avec l'ID {id} a été mis à jour en '{statut}'.");
        }
    }
}
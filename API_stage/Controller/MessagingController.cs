using System.ComponentModel.DataAnnotations;
using API_stage.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using API_stage.Request;

[Authorize(Roles = "Admin,Entreprise,Etudiant")]
[ApiController]
[Route("api/v1/[controller]")]
public class MessagingController : ControllerBase
{
    private readonly StageDbContext _context;

    public MessagingController(StageDbContext context)
    {
        _context = context;
    }

    [Authorize(Roles = "Admin,Entreprise,Etudiant")]
    [HttpPost("conversations")]
    public async Task<IActionResult> CreateConversation([FromBody] CreateConversationRequest request)
    {
        // Récupérer l'identité de l'utilisateur connecté à partir du token
        var nameIdentifier = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(nameIdentifier))
        {
            return BadRequest("Valeur NameIdentifier introuvable dans le jeton.");
        }

        int participant1Id;
        if (!int.TryParse(nameIdentifier, out participant1Id))
        {
            // Si ce n'est pas un identifiant numérique, rechercher l'utilisateur par son nom d'utilisateur
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == nameIdentifier);

            if (user == null)
            {
                return NotFound("Utilisateur introuvable.");
            }

            participant1Id = user.Id;
        }

        // Vérifier si une conversation existe déjà entre ces deux utilisateurs
        var existingConversation = await _context.Conversations
            .FirstOrDefaultAsync(c =>
                (c.Participant1Id == participant1Id && c.Participant2Id == request.Participant2Id) ||
                (c.Participant1Id == request.Participant2Id && c.Participant2Id == participant1Id));

        if (existingConversation != null)
        {
            return BadRequest("Une conversation existe déjà entre ces deux utilisateurs.");
        }

        // Créer une nouvelle conversation
        var conversation = new Conversation
        {
            Participant1Id = participant1Id, // ID récupéré depuis le token
            Participant2Id = request.Participant2Id,
        };

        _context.Conversations.Add(conversation);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            Message = "Conversation créée avec succès.",
            ConversationId = conversation.Id
        });
    }

    public class CreateConversationRequest
    {
        public int Participant2Id { get; set; } // Deuxième utilisateur (peut être un étudiant ou une entreprise)
    }

    [Authorize(Roles = "Admin,Etudiant")]
    [HttpGet("conversations")]
    public async Task<IActionResult> GetConversationsForUser()
    {
        // Récupérer l'identité de l'utilisateur connecté
        var nameIdentifier = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(nameIdentifier))
        {
            return BadRequest("Valeur NameIdentifier introuvable dans le jeton.");
        }

        if (!int.TryParse(nameIdentifier, out var userId))
        {
            // Identifier l'utilisateur par son Username si ce n'est pas un entier
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == nameIdentifier);

            if (user == null)
            {
                return NotFound("Utilisateur introuvable.");
            }

            userId = user.Id;
        }

        // Récupérer toutes les conversations où l'utilisateur est un participant
        var conversations = await _context.Conversations
            .Where(c => c.Participant1Id == userId ||
                        c.Participant2Id == userId) // Filtre sur les conversations de l'utilisateur
            .Select(c => new
            {
                ConversationId = c.Id,
                Interlocuteur = c.Participant1Id == userId
                    ? new // Si l'utilisateur est Participant1, récupérer Participant2
                    {
                        c.Participant2.Id,
                        c.Participant2.Username,
                        c.Participant2.Role.Name
                    }
                    : new // Sinon, récupérer Participant1
                    {
                        c.Participant1.Id,
                        c.Participant1.Username,
                        c.Participant1.Role.Name
                    }
            })
            .ToListAsync();

        return Ok(conversations);
    }

    [Authorize(Roles = "Admin,Etudiant,Entreprise")]
    [HttpGet("conversations/{conversationId}")]
    public async Task<IActionResult> GetConversationMessages(int conversationId)
    {
        // Récupérer l'identité de l'utilisateur connecté
        var nameIdentifier = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(nameIdentifier))
        {
            return BadRequest("Valeur NameIdentifier introuvable dans le jeton.");
        }

        // Vérifiez si le champ NameIdentifier est un entier ou un nom d'utilisateur
        int userId;
        if (!int.TryParse(nameIdentifier, out userId))
        {
            // Si ce n'est pas un entier, recherchez l'ID utilisateur en fonction de son nom
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == nameIdentifier);

            if (user == null)
            {
                return NotFound("Utilisateur introuvable.");
            }

            userId = user.Id; // Utilisez l'ID numérique de l'utilisateur
        }

        // Vérifier si la conversation existe
        var conversation = await _context.Conversations
            .Include(c => c.Messages) // Charger les messages liés
            .FirstOrDefaultAsync(c => c.Id == conversationId);

        if (conversation == null)
        {
            return NotFound($"La conversation avec l'ID {conversationId} n'existe pas.");
        }

        // Vérifiez si l'utilisateur fait partie de la conversation
        if (conversation.Participant1Id != userId && conversation.Participant2Id != userId)
        {
            return BadRequest("L'utilisateur connecté ne fait pas partie de cette conversation.");
        }

        // Trier les messages et ajouter un champ IsSelf
        var messages = conversation.Messages
            .OrderBy(m => m.DateEnvoi)
            .Select(m => new
            {
                m.EnvoyeurId, // Identifiant de l'envoyeur
                m.Contenu, // Contenu du message
                m.DateEnvoi, // Date d'envoi
                IsSelf = m.EnvoyeurId == userId // Indique si c'est l'utilisateur connecté qui a envoyé ce message
            })
            .ToList();

        // Retourner la liste des messages
        return Ok(new
        {
            ConversationId = conversationId,
            Messages = messages
        });
    }

    [Authorize]
    [HttpPost("conversations/{conversationId}/messages")]
    public async Task<IActionResult> SendMessage(int conversationId, [FromBody] SendMessageRequest request)
    {
        // Récupérer l'identité utilisateur connecté à partir du token
        var nameIdentifier = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(nameIdentifier))
        {
            return BadRequest("Valeur NameIdentifier introuvable dans le jeton.");
        }

        int userId;
        if (!int.TryParse(nameIdentifier, out userId))
        {
            // Si ce n'est pas un entier, rechercher l'ID utilisateur par son nom d'utilisateur
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == nameIdentifier);

            if (user == null)
            {
                return NotFound("Utilisateur introuvable.");
            }

            userId = user.Id; // Utilisez l'ID trouvé
        }

        // Vérifier si la conversation existe
        var conversation = await _context.Conversations.FindAsync(conversationId);

        if (conversation == null)
        {
            return NotFound($"La conversation avec l'ID {conversationId} n'existe pas.");
        }

        // Vérifier si l'utilisateur connecté fait partie de la conversation
        if (conversation.Participant1Id != userId && conversation.Participant2Id != userId)
        {
            return BadRequest("Vous ne faites pas partie de cette conversation.");
        }

        // Créer un nouveau message
        var message = new Message
        {
            ConversationId = conversationId,
            EnvoyeurId = userId, // Défini automatiquement à partir du token
            Contenu = request.Contenu,
            DateEnvoi = DateTime.UtcNow
        };

        // Ajouter le message à la base de données
        _context.Messages.Add(message);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            Message = "Message envoyé avec succès.",
            Data = new
            {
                message.ConversationId,
                message.EnvoyeurId,
                message.Contenu,
                message.DateEnvoi
            }
        });
    }

    public class SendMessageRequest
    {
        [Required]
        [StringLength(1000, ErrorMessage = "Le contenu du message ne doit pas dépasser 1000 caractères.")]
        public string Contenu { get; set; } // Le contenu du message
    }

    public class AddMessageRequest
    {
        public string Envoyeur { get; set; } // Peut typiquement être récupéré par un utilisateur connecté
        public string Contenu { get; set; }
    }
}
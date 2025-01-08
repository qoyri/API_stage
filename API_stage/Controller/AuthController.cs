using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using API_stage.Models;
using API_stage.Request;
using Microsoft.AspNetCore.Identity;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;

namespace API_stage.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly StageDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(StageDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // POST api/v1/auth/login
        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            // Vérifie que l'utilisateur existe
            var user = _context.Users.Include(u => u.Role).FirstOrDefault(u => u.Username == request.Username);

            if (user == null || !VerifyPassword(request.Password, user.Password))
            {
                return Unauthorized(new { message = "Nom d'utilisateur ou mot de passe incorrect." });
            }

            // Générer le JWT
            var token = GenerateToken(user);

            // Retourne les informations avec le token
            return Ok(new
            {
                Token = token,
                Username = user.Username,
                Role = user.Role.Name
            });
        }

        // Vérifie le hachage de mot de passe
        private bool VerifyPassword(string password, string hashedPassword)
        {
            var hasher = new PasswordHasher<User>();
            var result = hasher.VerifyHashedPassword(null!, hashedPassword, password);
            return result == PasswordVerificationResult.Success;
        }

        // Génère un JWT Token
        private string GenerateToken(User user)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]);
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Username),
                new Claim(ClaimTypes.Role, user.Role.Name) // Inclure le rôle de l'utilisateur dans le token
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(60), // Durée de validité du token
                Issuer = jwtSettings["Issuer"], // Ajout explicite de l'issuer !
                Audience = jwtSettings["Audience"], // Ajout explicite de l'audience (optionnel si la validation de l'audience est activée)
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature
                )
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }

        [Authorize] // Seul un utilisateur connecté peut accéder à ce point de terminaison
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            // Vérifiez si l'utilisateur authentifié est valide
            var username = User.FindFirst(ClaimTypes.NameIdentifier)?.Value; // Récupère le username à partir du token JWT
            if (string.IsNullOrEmpty(username))
            {
                return Unauthorized(new { message = "Utilisateur non authentifié." });
            }

            // Récupérer l'utilisateur associé au nom d'utilisateur
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null)
            {
                return NotFound(new { message = "Utilisateur non trouvé." });
            }

            // Vérifier le mot de passe actuel
            if (!VerifyPassword(request.CurrentPassword, user.Password))
            {
                return BadRequest(new { message = "Le mot de passe actuel est incorrect." });
            }

            // Vérifier que le nouveau mot de passe est différent de l'ancien
            if (request.CurrentPassword == request.NewPassword)
            {
                return BadRequest(new { message = "Le nouveau mot de passe ne peut pas être identique à l'ancien." });
            }

            // Hacher le nouveau mot de passe
            var hasher = new PasswordHasher<User>();
            user.Password = hasher.HashPassword(user, request.NewPassword);

            // Mettre à jour le mot de passe dans la base de données
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            // Réponse : succès
            return Ok(new { message = "Votre mot de passe a été mis à jour avec succès." });
        }
    }
}
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace API_stage.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class HealthController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public HealthController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // Endpoint : /api/v1/health/ping
        [HttpGet("ping")]
        public IActionResult Ping()
        {
            // Simple réponse pour indiquer que le serveur est en vie
            return Ok(new { message = "Server is up and running!" });
        }

        // Endpoint : /api/v1/health/check-token
        [HttpGet("check-token")]
        public IActionResult CheckToken()
        {
            // Récupère le token JWT de l'en-tête d'autorisation
            var authorizationHeader = Request.Headers["Authorization"].ToString();
            if (string.IsNullOrEmpty(authorizationHeader) || !authorizationHeader.StartsWith("Bearer "))
            {
                // Si pas de token fourni, juste une réponse générique
                return Ok(new { message = "Server is up, but no token was provided." });
            }

            var token = authorizationHeader.Replace("Bearer ", string.Empty);

            try
            {
                var jwtSettings = _configuration.GetSection("JwtSettings");
                var key = Encoding.UTF8.GetBytes(
                    jwtSettings["Key"]); // Récupérer la clé de signature à partir de la config

                var tokenHandler = new JwtSecurityTokenHandler();
                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings["Issuer"], // ValidIssuer spécifié dans appsettings.json
                    ValidAudience = jwtSettings["Audience"], // ValidAudience spécifié dans appsettings.json
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ClockSkew = TimeSpan.Zero // Suppression du délai accepté pour la validation
                }, out SecurityToken validatedToken);

                // Si le token est valide, récupère les claims
                var jwtToken = validatedToken as JwtSecurityToken;
                var claims = jwtToken?.Claims.Select(c => new { c.Type, c.Value });

                return Ok(new
                {
                    message = "Token is valid.",
                    claims
                });
            }
            catch (SecurityTokenExpiredException)
            {
                return Unauthorized(new { message = "Token is expired." });
            }
            catch (Exception)
            {
                return Unauthorized(new { message = "Invalid token." });
            }
        }
    }
}
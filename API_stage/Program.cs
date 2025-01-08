using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using API_stage.Models;

var builder = WebApplication.CreateBuilder(args);

// Configuration de la base de données
builder.Services.AddDbContext<StageDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configuration de l'authentification et de l'autorisation JWT
ConfigureAuthentication(builder.Services, builder.Configuration);

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
    options.AddPolicy("Etudiant", policy => policy.RequireRole("Etudiant"));
});

// Ajouter les services nécessaires pour les contrôleurs et Swagger
builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve
    );

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Entrez 'Bearer' [espace] et votre token JWT.\n\nExemple : 'Bearer abc12345'"
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] { }
        }
    });
});

var app = builder.Build();

// Activer Swagger en environnement approprié
if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "API Documentation");
        c.RoutePrefix = "home"; // Swagger sera accessible via http://<host>:<port>/home
    });
}

app.UseHttpsRedirection();
app.UseAuthentication(); // Doit être appelé avant UseAuthorization
app.UseAuthorization();

app.MapControllers();

// Initialisation de la base de données (seeding)
SeedDatabase(app);

app.Run();

// Setup de l'authentification JWT
void ConfigureAuthentication(IServiceCollection services, IConfiguration configuration)
{
    var jwtSettings = configuration.GetSection("JwtSettings");
    var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]);

    services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                // Validation des paramètres du token
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateIssuerSigningKey = true,
                ValidateLifetime = true,

                // Valeurs spécifiques provenant de la configuration
                ValidIssuer = jwtSettings["Issuer"],
                ValidAudience = jwtSettings["Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(key),

                // Suppression de l'écart permis pour la validation des temps
                ClockSkew = TimeSpan.Zero
            };

            // Journaux en cas de problème d'authentification
            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    Console.WriteLine($"Authentication failed: {context.Exception.Message}");
                    return Task.CompletedTask;
                },
                OnTokenValidated = context =>
                {
                    Console.WriteLine("Token successfully validated");
                    return Task.CompletedTask;
                }
            };
        });
}

// Fonction pour initialiser la base de données avec des données de test
void SeedDatabase(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<StageDbContext>();

    // Seeding des rôles (exemple)
    if (!context.Roles.Any())
    {
        context.Roles.Add(new Role { Name = "admin" }); // RoleId = 1
        context.Roles.Add(new Role { Name = "user" }); // RoleId = 2
        context.SaveChanges();
    }

    // Seeding d'un utilisateur administrateur
    if (!context.Users.Any(u => u.Username == "admin"))
    {
        var passwordHasher = new PasswordHasher<User>();
        var adminUser = new User
        {
            Username = "admin",
            Email = "admin@example.com",
            RoleId = 1, // Rôle admin
            CreatedAt = DateTime.UtcNow,
        };
        adminUser.Password = passwordHasher.HashPassword(adminUser, "Admin123");

        context.Users.Add(adminUser);
        context.SaveChanges();
    }
}
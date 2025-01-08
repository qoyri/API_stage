using API_stage.Models;
using Microsoft.AspNetCore.Identity;

namespace API_stage.Method;

public static class GenerateRandomPassword
{
    // Génère un mot de passe aléatoire
    public static string GenerateRandomPasswords()
    {
        const int length = 12;
        const string validChars = "ABCDEFGHJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@$?_-";
        var random = new Random();

        return new string(Enumerable.Repeat(validChars, length)
            .Select(chars => chars[random.Next(chars.Length)])
            .ToArray());
    }

    // Hash un mot de passe avec la classe PasswordHasher
    public static string HashPassword(string password)
    {
        var hasher = new PasswordHasher<User>();
        return hasher.HashPassword(null!, password);
    }
}
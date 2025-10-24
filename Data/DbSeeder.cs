// ====================================================================
// DbSeeder.cs : Initialise la base de données avec les données par défaut
// ====================================================================
// Crée les 3 rôles et l'utilisateur admin par défaut.

using Microsoft.EntityFrameworkCore;

namespace CTSAR.Booking.Data;

/// <summary>
/// Classe utilitaire pour initialiser la base de données.
/// Crée les rôles et l'admin par défaut au démarrage de l'application.
/// </summary>
public static class DbSeeder
{
    /// <summary>
    /// Initialise la base de données avec les données par défaut.
    /// </summary>
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        // Vérifier si les rôles existent déjà
        if (await context.Roles.AnyAsync())
        {
            // Les rôles existent déjà, pas besoin de seed
            return;
        }

        // ================================================================
        // CRÉATION DES 3 RÔLES
        // ================================================================

        var roles = new List<Role>
        {
            new Role
            {
                Name = "Administrateur",
                Description = "Accès complet à toutes les fonctionnalités de l'application"
            },
            new Role
            {
                Name = "Moniteur",
                Description = "Peut valider les réservations et gérer les séances de tir"
            },
            new Role
            {
                Name = "Membre",
                Description = "Peut créer des réservations et s'inscrire aux séances"
            }
        };

        context.Roles.AddRange(roles);
        await context.SaveChangesAsync();

        // ================================================================
        // CRÉATION DE L'UTILISATEUR ADMIN PAR DÉFAUT
        // ================================================================

        // Email et mot de passe par défaut
        const string adminEmail = "admin@ctsar.fr";
        const string adminPassword = "Admin123!";

        // Hasher le mot de passe avec BCrypt
        string passwordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword);

        var adminUser = new User
        {
            Email = adminEmail,
            PasswordHash = passwordHash,
            Nom = "Administrateur",
            Prenom = "CTSAR",
            PreferenceLangue = "fr",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        context.Users.Add(adminUser);
        await context.SaveChangesAsync();

        // Attribuer le rôle Administrateur à l'admin
        var adminRole = await context.Roles.FirstAsync(r => r.Name == "Administrateur");

        var userRole = new UserRole
        {
            UserId = adminUser.Id,
            RoleId = adminRole.Id
        };

        context.UserRoles.Add(userRole);
        await context.SaveChangesAsync();

        Console.WriteLine("✅ Base de données initialisée avec succès !");
        Console.WriteLine($"   Admin créé : {adminEmail} / {adminPassword}");
    }
}

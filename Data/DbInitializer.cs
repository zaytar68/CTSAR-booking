// ====================================================================
// DbInitializer : Initialise la base de données avec des données de départ
// ====================================================================
// Ce fichier crée automatiquement :
// - Les rôles (Administrateur, Moniteur, Membre)
// - Un compte administrateur par défaut
// - Des utilisateurs de test pour le développement

using Microsoft.AspNetCore.Identity;
using CTSAR.Booking.Constants;

namespace CTSAR.Booking.Data;

/// <summary>
/// Service d'initialisation de la base de données.
/// S'exécute au démarrage de l'application pour créer les données de base.
/// </summary>
public static class DbInitializer
{
    /// <summary>
    /// Initialise les rôles et crée les utilisateurs de test.
    /// Cette méthode est appelée au démarrage de l'app (voir Program.cs).
    /// </summary>
    /// <param name="userManager">Service de gestion des utilisateurs</param>
    /// <param name="roleManager">Service de gestion des rôles</param>
    public static async Task InitializeAsync(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        // ================================================================
        // ÉTAPE 1 : CRÉER LES RÔLES s'ils n'existent pas
        // ================================================================

        foreach (var roleName in RoleConstants.TousLesRoles)
        {
            // Vérifie si le rôle existe déjà
            var roleExist = await roleManager.RoleExistsAsync(roleName);

            if (!roleExist)
            {
                // Le rôle n'existe pas, on le crée
                await roleManager.CreateAsync(new IdentityRole(roleName));
                Console.WriteLine($"✅ Rôle créé : {roleName}");
            }
        }

        // ================================================================
        // ÉTAPE 2 : CRÉER L'ADMINISTRATEUR PAR DÉFAUT
        // ================================================================

        const string adminEmail = "admin@ctsar.fr";
        const string adminPassword = "Admin123!";  // ⚠️ À changer en production !

        // Vérifie si l'admin existe déjà
        var adminUser = await userManager.FindByEmailAsync(adminEmail);

        if (adminUser == null)
        {
            // L'admin n'existe pas, on le crée
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,  // Email déjà confirmé
                Nom = "Admin",
                Prenom = "Système",
                PreferenceLangue = "fr"
            };

            // Crée l'utilisateur avec son mot de passe
            var result = await userManager.CreateAsync(adminUser, adminPassword);

            if (result.Succeeded)
            {
                // Assigne le rôle Administrateur
                await userManager.AddToRoleAsync(adminUser, RoleConstants.Administrateur);
                Console.WriteLine($"✅ Administrateur créé : {adminEmail} / {adminPassword}");
            }
            else
            {
                Console.WriteLine($"❌ Erreur lors de la création de l'admin : {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
        }

        // ================================================================
        // ÉTAPE 3 : CRÉER DES UTILISATEURS DE TEST (développement)
        // ================================================================

        // Créer 2 moniteurs de test
        await CreerUtilisateurSiInexistant(
            userManager,
            "moniteur1@ctsar.fr",
            "Monitor123!",
            "Moniteur",
            "Un",
            RoleConstants.Moniteur
        );

        await CreerUtilisateurSiInexistant(
            userManager,
            "moniteur2@ctsar.fr",
            "Monitor123!",
            "Moniteur",
            "Deux",
            RoleConstants.Moniteur
        );

        // Créer 5 membres de test
        await CreerUtilisateurSiInexistant(
            userManager,
            "membre1@ctsar.fr",
            "Membre123!",
            "Dupont",
            "Jean",
            RoleConstants.Membre
        );

        await CreerUtilisateurSiInexistant(
            userManager,
            "membre2@ctsar.fr",
            "Membre123!",
            "Martin",
            "Marie",
            RoleConstants.Membre
        );

        await CreerUtilisateurSiInexistant(
            userManager,
            "membre3@ctsar.fr",
            "Membre123!",
            "Bernard",
            "Pierre",
            RoleConstants.Membre
        );

        await CreerUtilisateurSiInexistant(
            userManager,
            "membre4@ctsar.fr",
            "Membre123!",
            "Dubois",
            "Sophie",
            RoleConstants.Membre
        );

        await CreerUtilisateurSiInexistant(
            userManager,
            "membre5@ctsar.fr",
            "Membre123!",
            "Lefebvre",
            "Luc",
            RoleConstants.Membre
        );

        Console.WriteLine("✅ Initialisation de la base de données terminée !");
    }

    /// <summary>
    /// Méthode utilitaire pour créer un utilisateur s'il n'existe pas déjà.
    /// </summary>
    /// <param name="userManager">Service de gestion des utilisateurs</param>
    /// <param name="email">Email de l'utilisateur</param>
    /// <param name="password">Mot de passe</param>
    /// <param name="nom">Nom de famille</param>
    /// <param name="prenom">Prénom</param>
    /// <param name="role">Rôle à assigner</param>
    private static async Task CreerUtilisateurSiInexistant(
        UserManager<ApplicationUser> userManager,
        string email,
        string password,
        string nom,
        string prenom,
        string role)
    {
        // Vérifie si l'utilisateur existe déjà
        var user = await userManager.FindByEmailAsync(email);

        if (user == null)
        {
            // L'utilisateur n'existe pas, on le crée
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                Nom = nom,
                Prenom = prenom,
                PreferenceLangue = "fr"
            };

            var result = await userManager.CreateAsync(user, password);

            if (result.Succeeded)
            {
                // Assigne le rôle
                await userManager.AddToRoleAsync(user, role);
                Console.WriteLine($"✅ Utilisateur créé : {email} ({role})");
            }
            else
            {
                Console.WriteLine($"❌ Erreur lors de la création de {email} : {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
        }
    }
}

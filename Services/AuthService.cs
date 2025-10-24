// ====================================================================
// AuthService.cs : Service d'authentification custom
// ====================================================================
// Gère toutes les opérations d'authentification sans ASP.NET Core Identity.

using CTSAR.Booking.Data;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

namespace CTSAR.Booking.Services;

/// <summary>
/// Service pour gérer l'authentification custom.
/// Remplace UserManager et SignInManager d'Identity.
/// </summary>
public class AuthService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        ApplicationDbContext context,
        ILogger<AuthService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // ================================================================
    // MÉTHODES D'AUTHENTIFICATION
    // ================================================================

    /// <summary>
    /// Authentifie un utilisateur avec email et mot de passe.
    /// </summary>
    /// <returns>L'utilisateur avec ses rôles si succès, null sinon</returns>
    public async Task<User?> LoginAsync(string email, string password)
    {
        try
        {
            // Rechercher l'utilisateur par email
            var user = await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                _logger.LogWarning("Tentative de connexion avec un email inexistant : {Email}", email);
                return null;
            }

            if (!user.IsActive)
            {
                _logger.LogWarning("Tentative de connexion d'un compte désactivé : {Email}", email);
                return null;
            }

            // Vérifier le mot de passe avec BCrypt
            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);

            if (!isPasswordValid)
            {
                _logger.LogWarning("Mot de passe incorrect pour l'utilisateur : {Email}", email);
                return null;
            }

            _logger.LogInformation("Connexion réussie pour l'utilisateur : {Email}", email);
            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la connexion de l'utilisateur : {Email}", email);
            return null;
        }
    }

    /// <summary>
    /// Crée un nouveau compte utilisateur.
    /// </summary>
    public async Task<(bool Success, string? ErrorMessage, User? User)> RegisterAsync(
        string email,
        string password,
        string nom,
        string prenom,
        string roleName = "Membre")
    {
        try
        {
            // Vérifier si l'email existe déjà
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);

            if (existingUser != null)
            {
                return (false, "Un compte avec cet email existe déjà", null);
            }

            // Vérifier que le rôle existe
            var role = await _context.Roles
                .FirstOrDefaultAsync(r => r.Name == roleName);

            if (role == null)
            {
                _logger.LogError("Tentative de création d'utilisateur avec un rôle inexistant : {RoleName}", roleName);
                return (false, "Le rôle spécifié n'existe pas", null);
            }

            // Hasher le mot de passe avec BCrypt
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

            // Créer l'utilisateur
            var user = new User
            {
                Email = email,
                PasswordHash = passwordHash,
                Nom = nom,
                Prenom = prenom,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Ajouter le rôle à l'utilisateur
            var userRole = new UserRole
            {
                UserId = user.Id,
                RoleId = role.Id
            };

            _context.UserRoles.Add(userRole);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Nouvel utilisateur créé : {Email} avec le rôle {RoleName}", email, roleName);

            // Recharger l'utilisateur avec ses rôles
            user = await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstAsync(u => u.Id == user.Id);

            return (true, null, user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la création de l'utilisateur : {Email}", email);
            return (false, "Une erreur est survenue lors de la création du compte", null);
        }
    }

    /// <summary>
    /// Change le mot de passe d'un utilisateur.
    /// </summary>
    public async Task<(bool Success, string? ErrorMessage)> ChangePasswordAsync(
        int userId,
        string oldPassword,
        string newPassword)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                return (false, "Utilisateur introuvable");
            }

            // Vérifier l'ancien mot de passe
            bool isOldPasswordValid = BCrypt.Net.BCrypt.Verify(oldPassword, user.PasswordHash);

            if (!isOldPasswordValid)
            {
                _logger.LogWarning("Tentative de changement de mot de passe avec un ancien mot de passe incorrect : {UserId}", userId);
                return (false, "L'ancien mot de passe est incorrect");
            }

            // Hasher le nouveau mot de passe
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);

            await _context.SaveChangesAsync();

            _logger.LogInformation("Mot de passe changé avec succès pour l'utilisateur : {UserId}", userId);
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors du changement de mot de passe pour l'utilisateur : {UserId}", userId);
            return (false, "Une erreur est survenue lors du changement de mot de passe");
        }
    }

    // ================================================================
    // MÉTHODES UTILITAIRES
    // ================================================================

    /// <summary>
    /// Récupère un utilisateur par son ID avec ses rôles.
    /// </summary>
    public async Task<User?> GetUserByIdAsync(int userId)
    {
        return await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId);
    }

    /// <summary>
    /// Récupère un utilisateur par son email avec ses rôles.
    /// </summary>
    public async Task<User?> GetUserByEmailAsync(string email)
    {
        return await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == email);
    }

    /// <summary>
    /// Vérifie si un utilisateur a un rôle spécifique.
    /// </summary>
    public async Task<bool> IsInRoleAsync(int userId, string roleName)
    {
        return await _context.UserRoles
            .Include(ur => ur.Role)
            .AnyAsync(ur => ur.UserId == userId && ur.Role.Name == roleName);
    }

    /// <summary>
    /// Récupère tous les rôles d'un utilisateur.
    /// </summary>
    public async Task<List<string>> GetUserRolesAsync(int userId)
    {
        return await _context.UserRoles
            .Include(ur => ur.Role)
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.Role.Name)
            .ToListAsync();
    }
}

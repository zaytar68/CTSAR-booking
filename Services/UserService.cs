// ====================================================================
// UserService.cs : Service de gestion des utilisateurs (VERSION CUSTOM)
// ====================================================================
// Réécrit pour utiliser notre système d'authentification custom
// au lieu d'ASP.NET Core Identity.

using Microsoft.EntityFrameworkCore;
using CTSAR.Booking.Data;
using CTSAR.Booking.DTOs;
using CTSAR.Booking.Constants;

namespace CTSAR.Booking.Services;

/// <summary>
/// Service pour gérer les utilisateurs de l'application.
/// Gère toutes les opérations CRUD (Create, Read, Update, Delete).
/// Version custom sans ASP.NET Core Identity.
/// </summary>
public class UserService
{
    private readonly ApplicationDbContext _context;
    private readonly AuthService _authService;
    private readonly ILogger<UserService> _logger;

    public UserService(
        ApplicationDbContext context,
        AuthService authService,
        ILogger<UserService> logger)
    {
        _context = context;
        _authService = authService;
        _logger = logger;
    }

    // ================================================================
    // MÉTHODES DE LECTURE (READ)
    // ================================================================

    /// <summary>
    /// Récupère TOUS les utilisateurs avec leurs rôles.
    /// </summary>
    public async Task<List<UserDto>> GetAllUsersAsync()
    {
        try
        {
            var users = await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Where(u => u.IsActive)
                .OrderBy(u => u.Nom)
                .ThenBy(u => u.Prenom)
                .ToListAsync();

            return users.Select(u => new UserDto
            {
                Id = u.Id.ToString(),
                Email = u.Email,
                Nom = u.Nom,
                Prenom = u.Prenom,
                PreferenceLangue = u.PreferenceLangue,
                NotifMail = u.NotifMail,
                Notif2 = u.Notif2,
                Notif3 = u.Notif3,
                // NomComplet est calculé automatiquement par la propriété
                Roles = u.UserRoles.Select(ur => ur.Role.Name).ToList()
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération de tous les utilisateurs");
            return new List<UserDto>();
        }
    }

    /// <summary>
    /// Récupère un utilisateur par son ID.
    /// </summary>
    public async Task<UserDto?> GetUserByIdAsync(string userId)
    {
        try
        {
            if (!int.TryParse(userId, out int id))
            {
                return null;
            }

            var user = await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return null;
            }

            return new UserDto
            {
                Id = user.Id.ToString(),
                Email = user.Email,
                Nom = user.Nom,
                Prenom = user.Prenom,
                PreferenceLangue = user.PreferenceLangue,
                NotifMail = user.NotifMail,
                Notif2 = user.Notif2,
                Notif3 = user.Notif3,
                // NomComplet est calculé automatiquement par la propriété
                Roles = user.UserRoles.Select(ur => ur.Role.Name).ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération de l'utilisateur {UserId}", userId);
            return null;
        }
    }

    /// <summary>
    /// Récupère tous les rôles disponibles.
    /// </summary>
    public async Task<List<string>> GetAllRolesAsync()
    {
        try
        {
            return await _context.Roles
                .Select(r => r.Name)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération des rôles");
            return new List<string>();
        }
    }

    // ================================================================
    // MÉTHODES DE CRÉATION (CREATE)
    // ================================================================

    /// <summary>
    /// Crée un nouvel utilisateur.
    /// </summary>
    public async Task<(bool Success, string? ErrorMessage, string? UserId)> CreateUserAsync(
        string email,
        string password,
        string nom,
        string prenom,
        string role)
    {
        try
        {
            // Vérifier que le rôle existe
            if (!RoleNames.All.Contains(role))
            {
                return (false, $"Le rôle '{role}' n'est pas valide", null);
            }

            // Utiliser AuthService pour créer l'utilisateur
            var (success, errorMessage, user) = await _authService.RegisterAsync(
                email, password, nom, prenom, role);

            if (!success || user == null)
            {
                return (false, errorMessage ?? "Erreur lors de la création de l'utilisateur", null);
            }

            _logger.LogInformation("Utilisateur créé : {Email} avec le rôle {Role}", email, role);
            return (true, null, user.Id.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la création de l'utilisateur {Email}", email);
            return (false, "Une erreur est survenue lors de la création de l'utilisateur", null);
        }
    }

    // ================================================================
    // MÉTHODES DE MISE À JOUR (UPDATE)
    // ================================================================

    /// <summary>
    /// Met à jour les informations d'un utilisateur.
    /// </summary>
    public async Task<(bool Success, string? ErrorMessage)> UpdateUserAsync(
        string userId,
        string email,
        string nom,
        string prenom,
        string role,
        string? newPassword = null,
        string? preferenceLangue = null,
        bool? notifMail = null,
        bool? notif2 = null,
        bool? notif3 = null)
    {
        try
        {
            if (!int.TryParse(userId, out int id))
            {
                return (false, "ID utilisateur invalide");
            }

            var user = await _context.Users
                .Include(u => u.UserRoles)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return (false, "Utilisateur introuvable");
            }

            // Mettre à jour les informations de base
            user.Email = email;
            user.Nom = nom;
            user.Prenom = prenom;

            // Mettre à jour la préférence de langue si fournie
            if (!string.IsNullOrWhiteSpace(preferenceLangue))
            {
                user.PreferenceLangue = preferenceLangue;
            }

            // Mettre à jour les préférences de notification si fournies
            if (notifMail.HasValue)
            {
                user.NotifMail = notifMail.Value;
            }
            if (notif2.HasValue)
            {
                user.Notif2 = notif2.Value;
            }
            if (notif3.HasValue)
            {
                user.Notif3 = notif3.Value;
            }

            // Mettre à jour le mot de passe si fourni
            if (!string.IsNullOrWhiteSpace(newPassword))
            {
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            }

            // Mettre à jour le rôle
            var newRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == role);
            if (newRole == null)
            {
                return (false, $"Le rôle '{role}' n'existe pas");
            }

            // Supprimer l'ancien rôle
            var oldUserRole = user.UserRoles.FirstOrDefault();
            if (oldUserRole != null)
            {
                _context.UserRoles.Remove(oldUserRole);
            }

            // Ajouter le nouveau rôle
            var newUserRole = new UserRole
            {
                UserId = user.Id,
                RoleId = newRole.Id
            };
            _context.UserRoles.Add(newUserRole);

            await _context.SaveChangesAsync();

            _logger.LogInformation("Utilisateur mis à jour : {Email}", email);
            return (true, $"L'utilisateur {prenom} {nom} a été modifié avec succès");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la mise à jour de l'utilisateur {UserId}", userId);
            return (false, "Une erreur est survenue lors de la mise à jour");
        }
    }

    // ================================================================
    // MÉTHODES DE SUPPRESSION (DELETE)
    // ================================================================

    /// <summary>
    /// Désactive un utilisateur (soft delete).
    /// </summary>
    public async Task<(bool Success, string? ErrorMessage)> DeactivateUserAsync(string userId)
    {
        try
        {
            if (!int.TryParse(userId, out int id))
            {
                return (false, "ID utilisateur invalide");
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return (false, "Utilisateur introuvable");
            }

            user.IsActive = false;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Utilisateur désactivé : {Email}", user.Email);
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la désactivation de l'utilisateur {UserId}", userId);
            return (false, "Une erreur est survenue lors de la désactivation");
        }
    }

    // ================================================================
    // SURCHARGES POUR COMPATIBILITÉ AVEC LES DTOs
    // ================================================================

    /// <summary>
    /// Crée un utilisateur à partir d'un CreateUserDto (surcharge pour compatibilité).
    /// </summary>
    public async Task<(bool Success, string? ErrorMessage)> CreateUserAsync(CTSAR.Booking.DTOs.CreateUserDto dto)
    {
        // Valider qu'au moins un rôle est sélectionné
        if (dto.Roles == null || dto.Roles.Count == 0)
        {
            return (false, "Au moins un rôle doit être sélectionné");
        }

        try
        {
            // Vérifier que tous les rôles sont valides
            foreach (var roleName in dto.Roles)
            {
                if (!RoleNames.All.Contains(roleName))
                {
                    _logger.LogWarning("Tentative de création d'utilisateur avec un rôle invalide: {RoleName}", roleName);
                    return (false, $"Le rôle '{roleName}' n'est pas valide");
                }
            }

            // Vérifier si l'email existe déjà
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (existingUser != null)
            {
                return (false, "Un compte avec cet email existe déjà");
            }

            // Hasher le mot de passe avec BCrypt
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            // Créer l'utilisateur
            var user = new User
            {
                Email = dto.Email,
                PasswordHash = passwordHash,
                Nom = dto.Nom,
                Prenom = dto.Prenom,
                PreferenceLangue = dto.PreferenceLangue,
                NotifMail = dto.NotifMail,
                Notif2 = dto.Notif2,
                Notif3 = dto.Notif3,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Ajouter TOUS les rôles sélectionnés
            foreach (var roleName in dto.Roles)
            {
                var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
                if (role != null)
                {
                    var userRole = new UserRole
                    {
                        UserId = user.Id,
                        RoleId = role.Id
                    };
                    _context.UserRoles.Add(userRole);
                }
            }

            await _context.SaveChangesAsync();

            var rolesText = string.Join(", ", dto.Roles);
            _logger.LogInformation("Utilisateur créé: {Email} avec les rôles: {Roles}", dto.Email, rolesText);

            return (true, $"L'utilisateur {dto.Prenom} {dto.Nom} a été créé avec succès avec les rôles: {rolesText}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la création de l'utilisateur {Email}", dto.Email);
            return (false, "Une erreur est survenue lors de la création de l'utilisateur");
        }
    }

    /// <summary>
    /// Met à jour un utilisateur à partir d'un UpdateUserDto (surcharge pour compatibilité).
    /// </summary>
    public async Task<(bool Success, string? ErrorMessage)> UpdateUserAsync(CTSAR.Booking.DTOs.UpdateUserDto dto)
    {
        // Valider qu'au moins un rôle est sélectionné
        if (dto.Roles == null || dto.Roles.Count == 0)
        {
            return (false, "Au moins un rôle doit être sélectionné");
        }

        try
        {
            if (!int.TryParse(dto.Id, out int id))
            {
                return (false, "ID utilisateur invalide");
            }

            var user = await _context.Users
                .Include(u => u.UserRoles)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return (false, "Utilisateur introuvable");
            }

            // Vérifier que tous les rôles sont valides
            foreach (var roleName in dto.Roles)
            {
                if (!RoleNames.All.Contains(roleName))
                {
                    _logger.LogWarning("Tentative de mise à jour avec un rôle invalide: {RoleName}", roleName);
                    return (false, $"Le rôle '{roleName}' n'est pas valide");
                }
            }

            // Mettre à jour les informations de base
            user.Email = dto.Email;
            user.Nom = dto.Nom;
            user.Prenom = dto.Prenom;
            user.PreferenceLangue = dto.PreferenceLangue;
            user.NotifMail = dto.NotifMail;
            user.Notif2 = dto.Notif2;
            user.Notif3 = dto.Notif3;

            // Supprimer TOUS les anciens rôles
            var oldUserRoles = user.UserRoles.ToList();
            foreach (var oldUserRole in oldUserRoles)
            {
                _context.UserRoles.Remove(oldUserRole);
            }

            // Ajouter TOUS les nouveaux rôles
            foreach (var roleName in dto.Roles)
            {
                var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
                if (role != null)
                {
                    var newUserRole = new UserRole
                    {
                        UserId = user.Id,
                        RoleId = role.Id
                    };
                    _context.UserRoles.Add(newUserRole);
                }
            }

            await _context.SaveChangesAsync();

            var rolesText = string.Join(", ", dto.Roles);
            _logger.LogInformation("Utilisateur mis à jour: {Email} avec les rôles: {Roles}", dto.Email, rolesText);

            return (true, $"L'utilisateur {dto.Prenom} {dto.Nom} a été modifié avec succès. Rôles: {rolesText}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la mise à jour de l'utilisateur {UserId}", dto.Id);
            return (false, "Une erreur est survenue lors de la mise à jour");
        }
    }

    /// <summary>
    /// Change le mot de passe à partir d'un ChangePasswordDto (surcharge pour compatibilité).
    /// </summary>
    public async Task<(bool Success, string? ErrorMessage)> ChangePasswordAsync(CTSAR.Booking.DTOs.ChangePasswordDto dto)
    {
        return await ChangePasswordAsync(dto.UserId, dto.CurrentPassword, dto.NewPassword);
    }

    // ================================================================
    // MÉTHODES UTILITAIRES
    // ================================================================

    /// <summary>
    /// Vérifie si un utilisateur a un rôle spécifique.
    /// </summary>
    public async Task<bool> IsInRoleAsync(string userId, string roleName)
    {
        if (!int.TryParse(userId, out int id))
        {
            return false;
        }

        return await _authService.IsInRoleAsync(id, roleName);
    }

    /// <summary>
    /// Verrouille un utilisateur (empêche la connexion).
    /// </summary>
    public async Task<(bool Success, string? ErrorMessage)> LockUserAsync(string userId)
    {
        try
        {
            if (!int.TryParse(userId, out int id))
            {
                return (false, "Identifiant utilisateur invalide");
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return (false, "Utilisateur introuvable");
            }

            user.IsActive = false;
            await _context.SaveChangesAsync();
            _logger.LogInformation("Utilisateur {Email} verrouillé", user.Email);
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors du verrouillage de l'utilisateur {UserId}", userId);
            return (false, "Une erreur est survenue lors du verrouillage");
        }
    }

    /// <summary>
    /// Déverrouille un utilisateur (réactive le compte).
    /// </summary>
    public async Task<(bool Success, string? ErrorMessage)> UnlockUserAsync(string userId)
    {
        try
        {
            if (!int.TryParse(userId, out int id))
            {
                return (false, "Identifiant utilisateur invalide");
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return (false, "Utilisateur introuvable");
            }

            user.IsActive = true;
            await _context.SaveChangesAsync();
            _logger.LogInformation("Utilisateur {Email} déverrouillé", user.Email);
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors du déverrouillage de l'utilisateur {UserId}", userId);
            return (false, "Une erreur est survenue lors du déverrouillage");
        }
    }

    /// <summary>
    /// Supprime définitivement un utilisateur (soft delete en fait).
    /// </summary>
    public async Task<(bool Success, string? ErrorMessage)> DeleteUserAsync(string userId)
    {
        // Pour l'instant, on utilise DeactivateUserAsync (soft delete)
        return await DeactivateUserAsync(userId);
    }

    /// <summary>
    /// Change le mot de passe d'un utilisateur.
    /// </summary>
    public async Task<(bool Success, string? ErrorMessage)> ChangePasswordAsync(string userId, string currentPassword, string newPassword)
    {
        try
        {
            if (!int.TryParse(userId, out int id))
            {
                return (false, "Identifiant utilisateur invalide");
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return (false, "Utilisateur introuvable");
            }

            // Utilise AuthService.ChangePasswordAsync
            return await _authService.ChangePasswordAsync(id, currentPassword, newPassword);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors du changement de mot de passe pour {UserId}", userId);
            return (false, "Une erreur est survenue lors du changement de mot de passe");
        }
    }
}

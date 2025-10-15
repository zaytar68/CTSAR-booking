// ====================================================================
// UserService.cs : Service de gestion des utilisateurs
// ====================================================================
// Ce fichier contient toute la LOGIQUE MÉTIER pour gérer les utilisateurs.
// Il fait le lien entre l'interface (pages Blazor) et la base de données.
//
// POURQUOI UN SERVICE ?
// - Séparer la logique métier de l'interface
// - Centraliser toutes les opérations sur les utilisateurs
// - Faciliter les tests unitaires
// - Réutiliser le code dans plusieurs pages

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using CTSAR.Booking.Data;
using CTSAR.Booking.DTOs;
using CTSAR.Booking.Constants;

namespace CTSAR.Booking.Services;

/// <summary>
/// Service pour gérer les utilisateurs de l'application.
/// Gère toutes les opérations CRUD (Create, Read, Update, Delete).
/// Utilise UserManager et RoleManager d'ASP.NET Core Identity.
/// </summary>
public class UserService
{
    // ================================================================
    // DÉPENDANCES INJECTÉES
    // ================================================================
    // Ces objets sont fournis automatiquement par le système
    // d'injection de dépendances configuré dans Program.cs

    /// <summary>
    /// UserManager : Service Identity pour gérer les utilisateurs.
    /// Permet de créer, modifier, supprimer des utilisateurs.
    /// Gère aussi les mots de passe, les rôles, etc.
    /// </summary>
    private readonly UserManager<ApplicationUser> _userManager;

    /// <summary>
    /// RoleManager : Service Identity pour gérer les rôles.
    /// Permet de vérifier, créer, modifier des rôles.
    /// </summary>
    private readonly RoleManager<IdentityRole> _roleManager;

    /// <summary>
    /// Logger : Pour enregistrer les messages d'information et d'erreur.
    /// Utile pour déboguer et surveiller l'application.
    /// </summary>
    private readonly ILogger<UserService> _logger;

    /// <summary>
    /// Constructeur : Reçoit toutes les dépendances nécessaires.
    /// Appelé automatiquement par le système d'injection de dépendances.
    /// </summary>
    public UserService(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ILogger<UserService> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    // ================================================================
    // MÉTHODES DE LECTURE (READ)
    // ================================================================

    /// <summary>
    /// Récupère TOUS les utilisateurs avec leurs rôles.
    /// Retourne une liste de UserDto (sans les mots de passe).
    /// Utilisé pour afficher la liste des utilisateurs dans l'interface admin.
    /// </summary>
    /// <returns>Liste de tous les utilisateurs</returns>
    public async Task<List<UserDto>> GetAllUsersAsync()
    {
        try
        {
            _logger.LogInformation("Récupération de tous les utilisateurs");

            // Récupère tous les utilisateurs de la base de données
            var users = await _userManager.Users.ToListAsync();

            // Convertit chaque ApplicationUser en UserDto
            var userDtos = new List<UserDto>();

            foreach (var user in users)
            {
                // Récupère les rôles de cet utilisateur
                var roles = await _userManager.GetRolesAsync(user);

                // Crée un UserDto avec toutes les infos
                var userDto = new UserDto
                {
                    Id = user.Id,
                    Nom = user.Nom,
                    Prenom = user.Prenom,
                    Email = user.Email ?? string.Empty,
                    PhoneNumber = user.PhoneNumber,
                    PreferenceLangue = user.PreferenceLangue,
                    Roles = roles.ToList(),
                    LockoutEnabled = user.LockoutEnabled,
                    LockoutEnd = user.LockoutEnd
                };

                userDtos.Add(userDto);
            }

            _logger.LogInformation($"{userDtos.Count} utilisateurs récupérés");
            return userDtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération des utilisateurs");
            throw;
        }
    }

    /// <summary>
    /// Récupère UN utilisateur par son ID.
    /// Retourne null si l'utilisateur n'existe pas.
    /// </summary>
    /// <param name="userId">ID de l'utilisateur à récupérer</param>
    /// <returns>UserDto ou null si non trouvé</returns>
    public async Task<UserDto?> GetUserByIdAsync(string userId)
    {
        try
        {
            _logger.LogInformation($"Récupération de l'utilisateur {userId}");

            // Cherche l'utilisateur dans la base
            var user = await _userManager.FindByIdAsync(userId);

            // Si pas trouvé, retourne null
            if (user == null)
            {
                _logger.LogWarning($"Utilisateur {userId} non trouvé");
                return null;
            }

            // Récupère ses rôles
            var roles = await _userManager.GetRolesAsync(user);

            // Crée et retourne le DTO
            var userDto = new UserDto
            {
                Id = user.Id,
                Nom = user.Nom,
                Prenom = user.Prenom,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber,
                PreferenceLangue = user.PreferenceLangue,
                Roles = roles.ToList(),
                LockoutEnabled = user.LockoutEnabled,
                LockoutEnd = user.LockoutEnd
            };

            return userDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Erreur lors de la récupération de l'utilisateur {userId}");
            throw;
        }
    }

    /// <summary>
    /// Récupère tous les utilisateurs ayant un rôle spécifique.
    /// Exemple : GetUsersByRoleAsync("Moniteur") retourne tous les moniteurs.
    /// </summary>
    /// <param name="roleName">Nom du rôle (Administrateur, Moniteur, Membre)</param>
    /// <returns>Liste des utilisateurs ayant ce rôle</returns>
    public async Task<List<UserDto>> GetUsersByRoleAsync(string roleName)
    {
        try
        {
            _logger.LogInformation($"Récupération des utilisateurs avec le rôle {roleName}");

            // Récupère tous les utilisateurs ayant ce rôle
            var users = await _userManager.GetUsersInRoleAsync(roleName);

            // Convertit en UserDto
            var userDtos = new List<UserDto>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);

                var userDto = new UserDto
                {
                    Id = user.Id,
                    Nom = user.Nom,
                    Prenom = user.Prenom,
                    Email = user.Email ?? string.Empty,
                    PhoneNumber = user.PhoneNumber,
                    PreferenceLangue = user.PreferenceLangue,
                    Roles = roles.ToList(),
                    LockoutEnabled = user.LockoutEnabled,
                    LockoutEnd = user.LockoutEnd
                };

                userDtos.Add(userDto);
            }

            _logger.LogInformation($"{userDtos.Count} utilisateurs avec le rôle {roleName} récupérés");
            return userDtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Erreur lors de la récupération des utilisateurs avec le rôle {roleName}");
            throw;
        }
    }

    // ================================================================
    // MÉTHODES DE CRÉATION (CREATE)
    // ================================================================

    /// <summary>
    /// Crée un nouvel utilisateur.
    /// Valide les données, crée l'utilisateur, assigne les rôles.
    /// Retourne (succès: true/false, message: description du résultat).
    /// </summary>
    /// <param name="createUserDto">Données du nouvel utilisateur</param>
    /// <returns>Tuple (succès, message)</returns>
    public async Task<(bool Success, string Message)> CreateUserAsync(CreateUserDto createUserDto)
    {
        try
        {
            _logger.LogInformation($"Création de l'utilisateur {createUserDto.Email}");

            // 1. VÉRIFICATIONS PRÉALABLES
            // ----------------------------

            // Vérifie que l'email n'existe pas déjà
            var existingUser = await _userManager.FindByEmailAsync(createUserDto.Email);
            if (existingUser != null)
            {
                _logger.LogWarning($"L'email {createUserDto.Email} existe déjà");
                return (false, "Cet email est déjà utilisé par un autre compte");
            }

            // Vérifie que tous les rôles existent
            foreach (var roleName in createUserDto.Roles)
            {
                var roleExists = await _roleManager.RoleExistsAsync(roleName);
                if (!roleExists)
                {
                    _logger.LogWarning($"Le rôle {roleName} n'existe pas");
                    return (false, $"Le rôle '{roleName}' n'existe pas");
                }
            }

            // 2. CRÉATION DE L'UTILISATEUR
            // -----------------------------

            // Crée un nouvel ApplicationUser avec les données
            var user = new ApplicationUser
            {
                UserName = createUserDto.Email,  // On utilise l'email comme username
                Email = createUserDto.Email,
                Nom = createUserDto.Nom,
                Prenom = createUserDto.Prenom,
                PhoneNumber = createUserDto.PhoneNumber,
                PreferenceLangue = createUserDto.PreferenceLangue,
                EmailConfirmed = true  // Pas besoin de confirmer l'email pour le moment
            };

            // Crée l'utilisateur avec son mot de passe
            var result = await _userManager.CreateAsync(user, createUserDto.Password);

            // Si la création a échoué, retourne les erreurs
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogError($"Échec de création de l'utilisateur : {errors}");
                return (false, $"Échec de création : {errors}");
            }

            // 3. ASSIGNATION DES RÔLES
            // -------------------------

            // Assigne chaque rôle à l'utilisateur
            foreach (var roleName in createUserDto.Roles)
            {
                var roleResult = await _userManager.AddToRoleAsync(user, roleName);
                if (!roleResult.Succeeded)
                {
                    var errors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
                    _logger.LogError($"Échec d'ajout du rôle {roleName} : {errors}");
                    // Continue quand même avec les autres rôles
                }
            }

            _logger.LogInformation($"Utilisateur {user.Email} créé avec succès");
            return (true, "Utilisateur créé avec succès");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la création de l'utilisateur");
            return (false, $"Erreur inattendue : {ex.Message}");
        }
    }

    // ================================================================
    // MÉTHODES DE MODIFICATION (UPDATE)
    // ================================================================

    /// <summary>
    /// Met à jour un utilisateur existant.
    /// Modifie les infos personnelles et les rôles.
    /// NE MODIFIE PAS le mot de passe (utiliser ChangePasswordAsync pour ça).
    /// </summary>
    /// <param name="updateUserDto">Nouvelles données de l'utilisateur</param>
    /// <returns>Tuple (succès, message)</returns>
    public async Task<(bool Success, string Message)> UpdateUserAsync(UpdateUserDto updateUserDto)
    {
        try
        {
            _logger.LogInformation($"Mise à jour de l'utilisateur {updateUserDto.Id}");

            // 1. RÉCUPÉRATION DE L'UTILISATEUR
            // ---------------------------------

            var user = await _userManager.FindByIdAsync(updateUserDto.Id);
            if (user == null)
            {
                _logger.LogWarning($"Utilisateur {updateUserDto.Id} non trouvé");
                return (false, "Utilisateur non trouvé");
            }

            // 2. VÉRIFICATION DE L'EMAIL
            // ---------------------------

            // Si l'email a changé, vérifie qu'il n'est pas déjà utilisé
            if (user.Email != updateUserDto.Email)
            {
                var existingUser = await _userManager.FindByEmailAsync(updateUserDto.Email);
                if (existingUser != null && existingUser.Id != user.Id)
                {
                    _logger.LogWarning($"L'email {updateUserDto.Email} est déjà utilisé");
                    return (false, "Cet email est déjà utilisé par un autre compte");
                }
            }

            // 3. MISE À JOUR DES INFORMATIONS
            // --------------------------------

            user.Nom = updateUserDto.Nom;
            user.Prenom = updateUserDto.Prenom;
            user.Email = updateUserDto.Email;
            user.UserName = updateUserDto.Email;  // Le username suit l'email
            user.PhoneNumber = updateUserDto.PhoneNumber;
            user.PreferenceLangue = updateUserDto.PreferenceLangue;

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogError($"Échec de mise à jour : {errors}");
                return (false, $"Échec de mise à jour : {errors}");
            }

            // 4. MISE À JOUR DES RÔLES
            // ------------------------

            // Récupère les rôles actuels
            var currentRoles = await _userManager.GetRolesAsync(user);

            // Trouve les rôles à ajouter (dans updateUserDto mais pas dans currentRoles)
            var rolesToAdd = updateUserDto.Roles.Except(currentRoles).ToList();

            // Trouve les rôles à supprimer (dans currentRoles mais pas dans updateUserDto)
            var rolesToRemove = currentRoles.Except(updateUserDto.Roles).ToList();

            // Ajoute les nouveaux rôles
            if (rolesToAdd.Any())
            {
                var addResult = await _userManager.AddToRolesAsync(user, rolesToAdd);
                if (!addResult.Succeeded)
                {
                    var errors = string.Join(", ", addResult.Errors.Select(e => e.Description));
                    _logger.LogError($"Échec d'ajout des rôles : {errors}");
                }
            }

            // Supprime les anciens rôles
            if (rolesToRemove.Any())
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
                if (!removeResult.Succeeded)
                {
                    var errors = string.Join(", ", removeResult.Errors.Select(e => e.Description));
                    _logger.LogError($"Échec de suppression des rôles : {errors}");
                }
            }

            _logger.LogInformation($"Utilisateur {user.Email} mis à jour avec succès");
            return (true, "Utilisateur mis à jour avec succès");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la mise à jour de l'utilisateur");
            return (false, $"Erreur inattendue : {ex.Message}");
        }
    }

    /// <summary>
    /// Change le mot de passe d'un utilisateur.
    /// Nécessite l'ancien mot de passe pour des raisons de sécurité.
    /// </summary>
    /// <param name="changePasswordDto">Données pour le changement de mot de passe</param>
    /// <returns>Tuple (succès, message)</returns>
    public async Task<(bool Success, string Message)> ChangePasswordAsync(ChangePasswordDto changePasswordDto)
    {
        try
        {
            _logger.LogInformation($"Changement de mot de passe pour l'utilisateur {changePasswordDto.UserId}");

            // Récupère l'utilisateur
            var user = await _userManager.FindByIdAsync(changePasswordDto.UserId);
            if (user == null)
            {
                _logger.LogWarning($"Utilisateur {changePasswordDto.UserId} non trouvé");
                return (false, "Utilisateur non trouvé");
            }

            // Change le mot de passe (vérifie l'ancien automatiquement)
            var result = await _userManager.ChangePasswordAsync(
                user,
                changePasswordDto.CurrentPassword,
                changePasswordDto.NewPassword);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogError($"Échec du changement de mot de passe : {errors}");
                return (false, $"Échec : {errors}");
            }

            _logger.LogInformation($"Mot de passe changé avec succès pour {user.Email}");
            return (true, "Mot de passe changé avec succès");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors du changement de mot de passe");
            return (false, $"Erreur inattendue : {ex.Message}");
        }
    }

    // ================================================================
    // MÉTHODES DE SUPPRESSION (DELETE)
    // ================================================================

    /// <summary>
    /// Supprime un utilisateur de la base de données.
    /// ATTENTION : Cette action est irréversible !
    /// Supprime aussi toutes les données liées (rôles, etc.).
    /// </summary>
    /// <param name="userId">ID de l'utilisateur à supprimer</param>
    /// <returns>Tuple (succès, message)</returns>
    public async Task<(bool Success, string Message)> DeleteUserAsync(string userId)
    {
        try
        {
            _logger.LogInformation($"Suppression de l'utilisateur {userId}");

            // Récupère l'utilisateur
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning($"Utilisateur {userId} non trouvé");
                return (false, "Utilisateur non trouvé");
            }

            // Supprime l'utilisateur (et toutes ses données liées automatiquement)
            var result = await _userManager.DeleteAsync(user);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogError($"Échec de suppression : {errors}");
                return (false, $"Échec de suppression : {errors}");
            }

            _logger.LogInformation($"Utilisateur {user.Email} supprimé avec succès");
            return (true, "Utilisateur supprimé avec succès");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la suppression de l'utilisateur");
            return (false, $"Erreur inattendue : {ex.Message}");
        }
    }

    // ================================================================
    // MÉTHODES DE VERROUILLAGE (LOCKOUT)
    // ================================================================

    /// <summary>
    /// Verrouille un utilisateur (empêche la connexion).
    /// Peut être utilisé pour suspendre temporairement un compte.
    /// </summary>
    /// <param name="userId">ID de l'utilisateur à verrouiller</param>
    /// <param name="lockoutEnd">Date de fin du verrouillage (null = permanent)</param>
    /// <returns>Tuple (succès, message)</returns>
    public async Task<(bool Success, string Message)> LockUserAsync(string userId, DateTimeOffset? lockoutEnd = null)
    {
        try
        {
            _logger.LogInformation($"Verrouillage de l'utilisateur {userId}");

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return (false, "Utilisateur non trouvé");
            }

            // Si pas de date spécifiée, verrouille pour 100 ans (= permanent)
            lockoutEnd ??= DateTimeOffset.Now.AddYears(100);

            // Active le verrouillage
            user.LockoutEnabled = true;
            var result = await _userManager.SetLockoutEndDateAsync(user, lockoutEnd);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return (false, $"Échec : {errors}");
            }

            _logger.LogInformation($"Utilisateur {user.Email} verrouillé jusqu'au {lockoutEnd}");
            return (true, "Utilisateur verrouillé avec succès");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors du verrouillage de l'utilisateur");
            return (false, $"Erreur inattendue : {ex.Message}");
        }
    }

    /// <summary>
    /// Déverrouille un utilisateur (permet à nouveau la connexion).
    /// </summary>
    /// <param name="userId">ID de l'utilisateur à déverrouiller</param>
    /// <returns>Tuple (succès, message)</returns>
    public async Task<(bool Success, string Message)> UnlockUserAsync(string userId)
    {
        try
        {
            _logger.LogInformation($"Déverrouillage de l'utilisateur {userId}");

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return (false, "Utilisateur non trouvé");
            }

            // Supprime la date de verrouillage
            var result = await _userManager.SetLockoutEndDateAsync(user, null);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return (false, $"Échec : {errors}");
            }

            _logger.LogInformation($"Utilisateur {user.Email} déverrouillé avec succès");
            return (true, "Utilisateur déverrouillé avec succès");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors du déverrouillage de l'utilisateur");
            return (false, $"Erreur inattendue : {ex.Message}");
        }
    }
}

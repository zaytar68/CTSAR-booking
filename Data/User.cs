// ====================================================================
// User.cs : Modèle de données pour les utilisateurs (CUSTOM)
// ====================================================================
// Ce fichier remplace ApplicationUser qui héritait de IdentityUser.
// Nous gérons maintenant l'authentification de manière custom.

using System.ComponentModel.DataAnnotations;

namespace CTSAR.Booking.Data;

/// <summary>
/// Représente un utilisateur de l'application CTSAR Booking.
/// Modèle custom pour l'authentification (sans Identity).
/// </summary>
public class User
{
    // ================================================================
    // PROPRIÉTÉS DE BASE
    // ================================================================

    /// <summary>
    /// Identifiant unique de l'utilisateur.
    /// Clé primaire auto-incrémentée.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Adresse email de l'utilisateur (sert aussi de nom d'utilisateur).
    /// Unique dans la base de données.
    /// </summary>
    [Required(ErrorMessage = "L'email est obligatoire")]
    [EmailAddress(ErrorMessage = "Format d'email invalide")]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Hash du mot de passe (jamais le mot de passe en clair).
    /// Hashé avec BCrypt.
    /// </summary>
    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// Nom de famille de l'utilisateur.
    /// </summary>
    [Required(ErrorMessage = "Le nom est obligatoire")]
    [MaxLength(100, ErrorMessage = "Le nom ne peut pas dépasser 100 caractères")]
    public string Nom { get; set; } = string.Empty;

    /// <summary>
    /// Prénom de l'utilisateur.
    /// </summary>
    [Required(ErrorMessage = "Le prénom est obligatoire")]
    [MaxLength(100, ErrorMessage = "Le prénom ne peut pas dépasser 100 caractères")]
    public string Prenom { get; set; } = string.Empty;

    /// <summary>
    /// Langue préférée de l'utilisateur pour l'interface.
    /// Valeurs possibles : "fr" (français), "de" (allemand), "en" (anglais)
    /// </summary>
    [MaxLength(5)]
    public string PreferenceLangue { get; set; } = "fr";

    /// <summary>
    /// Indique si l'utilisateur est actif.
    /// Permet de désactiver un compte sans le supprimer.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Date et heure de création du compte.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // ================================================================
    // PROPRIÉTÉS DE NAVIGATION (relations avec d'autres tables)
    // ================================================================

    /// <summary>
    /// Rôles de l'utilisateur (many-to-many via UserRole).
    /// Un utilisateur peut avoir plusieurs rôles.
    /// </summary>
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

    // ================================================================
    // PROPRIÉTÉS CALCULÉES (pas stockées en base)
    // ================================================================

    /// <summary>
    /// Nom complet de l'utilisateur (Prénom + Nom).
    /// </summary>
    public string NomComplet => $"{Prenom} {Nom}";

    /// <summary>
    /// Initiales de l'utilisateur pour afficher un avatar.
    /// </summary>
    public string Initiales
    {
        get
        {
            var premiereLettrePrenom = Prenom.Length > 0 ? Prenom[0] : ' ';
            var premiereLettreNom = Nom.Length > 0 ? Nom[0] : ' ';
            return $"{premiereLettrePrenom}{premiereLettreNom}".Trim();
        }
    }
}

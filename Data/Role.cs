// ====================================================================
// Role.cs : Modèle de données pour les rôles utilisateur
// ====================================================================
// Représente un rôle dans l'application (Administrateur, Moniteur, Membre).

using System.ComponentModel.DataAnnotations;

namespace CTSAR.Booking.Data;

/// <summary>
/// Représente un rôle utilisateur dans l'application.
/// Les rôles définissent les permissions et autorisations.
/// </summary>
public class Role
{
    // ================================================================
    // PROPRIÉTÉS DE BASE
    // ================================================================

    /// <summary>
    /// Identifiant unique du rôle.
    /// Clé primaire auto-incrémentée.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Nom du rôle.
    /// Valeurs attendues : "Administrateur", "Moniteur", "Membre"
    /// Unique dans la base de données.
    /// </summary>
    [Required(ErrorMessage = "Le nom du rôle est obligatoire")]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description du rôle (optionnel).
    /// Explique les permissions associées à ce rôle.
    /// </summary>
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    // ================================================================
    // PROPRIÉTÉS DE NAVIGATION (relations avec d'autres tables)
    // ================================================================

    /// <summary>
    /// Utilisateurs ayant ce rôle (many-to-many via UserRole).
    /// </summary>
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}

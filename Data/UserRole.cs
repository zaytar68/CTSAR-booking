// ====================================================================
// UserRole.cs : Table de liaison Many-to-Many entre User et Role
// ====================================================================
// Permet de gérer la relation plusieurs-à-plusieurs :
// - Un utilisateur peut avoir plusieurs rôles
// - Un rôle peut être attribué à plusieurs utilisateurs

namespace CTSAR.Booking.Data;

/// <summary>
/// Représente la relation many-to-many entre User et Role.
/// Table de jonction avec clé composite (UserId, RoleId).
/// </summary>
public class UserRole
{
    // ================================================================
    // CLÉS ÉTRANGÈRES (constituent la clé primaire composite)
    // ================================================================

    /// <summary>
    /// Identifiant de l'utilisateur.
    /// Clé étrangère vers la table Users.
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Identifiant du rôle.
    /// Clé étrangère vers la table Roles.
    /// </summary>
    public int RoleId { get; set; }

    // ================================================================
    // PROPRIÉTÉS DE NAVIGATION
    // ================================================================

    /// <summary>
    /// Navigation vers l'utilisateur associé.
    /// </summary>
    public User User { get; set; } = null!;

    /// <summary>
    /// Navigation vers le rôle associé.
    /// </summary>
    public Role Role { get; set; } = null!;
}

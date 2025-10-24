// ====================================================================
// RoleNames.cs : Constantes pour les noms de rôles
// ====================================================================

namespace CTSAR.Booking.Data;

/// <summary>
/// Constantes pour les noms de rôles dans l'application.
/// </summary>
public static class RoleNames
{
    public const string Administrateur = "Administrateur";
    public const string Moniteur = "Moniteur";
    public const string Membre = "Membre";

    /// <summary>
    /// Liste de tous les rôles disponibles
    /// </summary>
    public static readonly List<string> All = new()
    {
        Administrateur,
        Moniteur,
        Membre
    };
}

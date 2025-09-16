namespace CTSAR.Booking.Models;

/// <summary>
/// Rôles disponibles dans le club de tir
/// </summary>
public enum Role
{
    /// <summary>
    /// Membre standard du club - peut réserver des créneaux
    /// </summary>
    Membre = 1,

    /// <summary>
    /// Moniteur - peut valider les réservations et encadrer les séances
    /// </summary>
    Moniteur = 2,

    /// <summary>
    /// Administrateur - accès complet à la gestion du club
    /// </summary>
    Administrateur = 3
}
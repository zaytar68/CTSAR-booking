// ====================================================================
// FermetureClub.cs : Modèle représentant une fermeture planifiée du club
// ====================================================================

using System.ComponentModel.DataAnnotations;

namespace CTSAR.Booking.Data;

/// <summary>
/// Représente une période de fermeture du club de tir.
/// Empêche toutes les réservations pendant la période (toutes alvéoles confondues).
/// </summary>
public class FermetureClub
{
    /// <summary>
    /// Identifiant unique de la fermeture
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Date et heure de début de la fermeture
    /// </summary>
    [Required]
    public DateTime DateDebut { get; set; }

    /// <summary>
    /// Date et heure de fin de la fermeture
    /// Doit être supérieure ou égale à DateDebut
    /// </summary>
    [Required]
    public DateTime DateFin { get; set; }

    /// <summary>
    /// Raison de la fermeture (optionnel)
    /// </summary>
    [MaxLength(200)]
    public string? Raison { get; set; }

    /// <summary>
    /// Type de fermeture (Travaux, JourFerie, etc.)
    /// </summary>
    public TypeFermeture TypeFermeture { get; set; }

    /// <summary>
    /// Date de création de cette fermeture
    /// </summary>
    public DateTime DateCreation { get; set; } = DateTime.UtcNow;
}

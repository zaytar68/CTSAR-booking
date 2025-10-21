// ====================================================================
// ReservationAlveole.cs : Table de liaison many-to-many
// ====================================================================
// Lie une réservation à une ou plusieurs alvéoles.
// Une réservation peut bloquer plusieurs alvéoles simultanément.

using System.ComponentModel.DataAnnotations;

namespace CTSAR.Booking.Data;

/// <summary>
/// Table de liaison entre Reservation et Alveole.
/// Permet à une réservation de bloquer plusieurs alvéoles.
/// </summary>
public class ReservationAlveole
{
    /// <summary>
    /// ID de la réservation
    /// </summary>
    [Required]
    public int ReservationId { get; set; }

    /// <summary>
    /// ID de l'alvéole
    /// </summary>
    [Required]
    public int AlveoleId { get; set; }

    // ================================================================
    // RELATIONS NAVIGATION (Entity Framework)
    // ================================================================

    /// <summary>
    /// Réservation associée
    /// </summary>
    public Reservation Reservation { get; set; } = null!;

    /// <summary>
    /// Alvéole associée
    /// </summary>
    public Alveole Alveole { get; set; } = null!;
}

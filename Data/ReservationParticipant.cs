// ====================================================================
// ReservationParticipant.cs : Table de liaison pour les participants
// ====================================================================
// Plusieurs membres peuvent s'inscrire sur une même réservation
// pour tirer ensemble. Les moniteurs sont marqués avec EstMoniteur=true.

using System.ComponentModel.DataAnnotations;

namespace CTSAR.Booking.Data;

/// <summary>
/// Table de liaison entre Reservation et User.
/// Représente l'inscription d'un membre ou moniteur à une session de tir.
/// </summary>
public class ReservationParticipant
{
    /// <summary>
    /// ID de la réservation
    /// </summary>
    [Required]
    public int ReservationId { get; set; }

    /// <summary>
    /// ID de l'utilisateur participant
    /// </summary>
    [Required]
    public int UserId { get; set; }

    /// <summary>
    /// Date d'inscription à la réservation
    /// </summary>
    public DateTime DateInscription { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Indique si ce participant est un moniteur.
    /// Si au moins un participant a EstMoniteur=true,
    /// la réservation passe au statut "Confirmée".
    /// </summary>
    public bool EstMoniteur { get; set; }

    // ================================================================
    // RELATIONS NAVIGATION (Entity Framework)
    // ================================================================

    /// <summary>
    /// Réservation associée
    /// </summary>
    public Reservation Reservation { get; set; } = null!;

    /// <summary>
    /// Utilisateur participant
    /// </summary>
    public User User { get; set; } = null!;
}

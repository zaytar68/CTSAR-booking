// ====================================================================
// Reservation.cs : Modèle représentant une session de tir
// ====================================================================

using System.ComponentModel.DataAnnotations;

namespace CTSAR.Booking.Data;

/// <summary>
/// Représente une session de tir réservée.
/// Plusieurs membres peuvent s'inscrire pour tirer ensemble.
/// Le statut est calculé selon la présence d'un moniteur.
/// </summary>
public class Reservation
{
    /// <summary>
    /// Identifiant unique de la réservation
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Date et heure de début de la session
    /// </summary>
    [Required]
    public DateTime DateDebut { get; set; }

    /// <summary>
    /// Date et heure de fin de la session
    /// Doit être supérieure à DateDebut
    /// </summary>
    [Required]
    public DateTime DateFin { get; set; }

    /// <summary>
    /// Statut de la réservation (EnAttente ou Confirmee)
    /// Calculé automatiquement selon présence d'un moniteur
    /// </summary>
    public StatutReservation StatutReservation { get; set; } = StatutReservation.EnAttente;

    /// <summary>
    /// Commentaire optionnel de la réservation
    /// </summary>
    [MaxLength(500)]
    public string? Commentaire { get; set; }

    /// <summary>
    /// ID de l'utilisateur qui a créé la réservation
    /// </summary>
    [Required]
    public string CreatedByUserId { get; set; } = string.Empty;

    /// <summary>
    /// Date de création de la réservation
    /// </summary>
    public DateTime DateCreation { get; set; } = DateTime.UtcNow;

    // ================================================================
    // RELATIONS NAVIGATION (Entity Framework)
    // ================================================================

    /// <summary>
    /// Utilisateur qui a créé la réservation
    /// </summary>
    public ApplicationUser? CreatedBy { get; set; }

    /// <summary>
    /// Liste des alvéoles réservées
    /// Relation many-to-many via ReservationAlveole
    /// </summary>
    public ICollection<ReservationAlveole> ReservationAlveoles { get; set; } = new List<ReservationAlveole>();

    /// <summary>
    /// Liste des participants inscrits (membres + moniteurs)
    /// Relation many-to-many via ReservationParticipant
    /// </summary>
    public ICollection<ReservationParticipant> Participants { get; set; } = new List<ReservationParticipant>();
}

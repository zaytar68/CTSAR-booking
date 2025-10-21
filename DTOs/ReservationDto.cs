// ====================================================================
// ReservationDto.cs : DTO complet pour une réservation
// ====================================================================

using CTSAR.Booking.Data;

namespace CTSAR.Booking.DTOs;

/// <summary>
/// DTO complet représentant une réservation avec tous ses détails
/// </summary>
public class ReservationDto
{
    /// <summary>
    /// ID de la réservation
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Date et heure de début
    /// </summary>
    public DateTime DateDebut { get; set; }

    /// <summary>
    /// Date et heure de fin
    /// </summary>
    public DateTime DateFin { get; set; }

    /// <summary>
    /// Statut de la réservation
    /// </summary>
    public StatutReservation StatutReservation { get; set; }

    /// <summary>
    /// Commentaire optionnel
    /// </summary>
    public string? Commentaire { get; set; }

    /// <summary>
    /// ID de l'utilisateur qui a créé la réservation
    /// </summary>
    public string CreatedByUserId { get; set; } = string.Empty;

    /// <summary>
    /// Nom complet du créateur
    /// </summary>
    public string CreatedByNom { get; set; } = string.Empty;

    /// <summary>
    /// Date de création
    /// </summary>
    public DateTime DateCreation { get; set; }

    /// <summary>
    /// Liste des alvéoles réservées
    /// </summary>
    public List<AlveoleDto> Alveoles { get; set; } = new();

    /// <summary>
    /// Liste des participants (membres + moniteurs)
    /// </summary>
    public List<ReservationParticipantDto> Participants { get; set; } = new();

    /// <summary>
    /// Nombre total de participants
    /// </summary>
    public int NombreParticipants => Participants.Count;

    /// <summary>
    /// Nombre de moniteurs inscrits
    /// </summary>
    public int NombreMoniteurs => Participants.Count(p => p.EstMoniteur);

    /// <summary>
    /// Noms des alvéoles concaténés (ex: "A1, A2, A3")
    /// </summary>
    public string NomsAlveoles => string.Join(", ", Alveoles.Select(a => a.Nom));
}

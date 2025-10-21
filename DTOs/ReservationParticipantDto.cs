// ====================================================================
// ReservationParticipantDto.cs : DTO pour un participant
// ====================================================================

namespace CTSAR.Booking.DTOs;

/// <summary>
/// DTO représentant un participant à une réservation
/// </summary>
public class ReservationParticipantDto
{
    /// <summary>
    /// ID de l'utilisateur
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Nom complet de l'utilisateur
    /// </summary>
    public string NomComplet { get; set; } = string.Empty;

    /// <summary>
    /// Email de l'utilisateur
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Initiales pour l'avatar
    /// </summary>
    public string Initiales { get; set; } = string.Empty;

    /// <summary>
    /// Indique si ce participant est un moniteur
    /// </summary>
    public bool EstMoniteur { get; set; }

    /// <summary>
    /// Date d'inscription à cette réservation
    /// </summary>
    public DateTime DateInscription { get; set; }
}

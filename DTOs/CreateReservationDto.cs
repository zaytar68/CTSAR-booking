// ====================================================================
// CreateReservationDto.cs : DTO pour créer une réservation
// ====================================================================

using System.ComponentModel.DataAnnotations;

namespace CTSAR.Booking.DTOs;

/// <summary>
/// DTO pour créer une nouvelle réservation
/// </summary>
public class CreateReservationDto
{
    /// <summary>
    /// Liste des IDs des alvéoles à réserver
    /// Au moins une alvéole doit être sélectionnée
    /// </summary>
    [Required(ErrorMessage = "Vous devez sélectionner au moins une alvéole")]
    [MinLength(1, ErrorMessage = "Vous devez sélectionner au moins une alvéole")]
    public List<int> AlveoleIds { get; set; } = new();

    /// <summary>
    /// Date et heure de début de la session
    /// </summary>
    [Required(ErrorMessage = "La date de début est obligatoire")]
    public DateTime DateDebut { get; set; }

    /// <summary>
    /// Date et heure de fin de la session
    /// Doit être supérieure à DateDebut
    /// </summary>
    [Required(ErrorMessage = "La date de fin est obligatoire")]
    public DateTime DateFin { get; set; }

    /// <summary>
    /// Commentaire optionnel
    /// </summary>
    [MaxLength(500, ErrorMessage = "Le commentaire ne peut pas dépasser 500 caractères")]
    public string? Commentaire { get; set; }
}

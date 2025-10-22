// ====================================================================
// FermetureDto.cs : DTO pour une fermeture d'alvéole
// ====================================================================

using CTSAR.Booking.Data;

namespace CTSAR.Booking.DTOs;

/// <summary>
/// DTO représentant une fermeture planifiée d'une alvéole
/// </summary>
public class FermetureDto
{
    /// <summary>
    /// ID de la fermeture
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// ID de l'alvéole concernée
    /// </summary>
    public int AlveoleId { get; set; }

    /// <summary>
    /// Nom de l'alvéole concernée
    /// </summary>
    public string AlveoleNom { get; set; } = string.Empty;

    /// <summary>
    /// Date et heure de début de la fermeture
    /// </summary>
    public DateTime DateDebut { get; set; }

    /// <summary>
    /// Date et heure de fin de la fermeture
    /// </summary>
    public DateTime DateFin { get; set; }

    /// <summary>
    /// Raison de la fermeture
    /// </summary>
    public string Raison { get; set; } = string.Empty;

    /// <summary>
    /// Type de fermeture
    /// </summary>
    public TypeFermeture TypeFermeture { get; set; }
}

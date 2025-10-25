// ====================================================================
// FermetureClubDto.cs : DTO pour une fermeture du club
// ====================================================================

using CTSAR.Booking.Data;

namespace CTSAR.Booking.DTOs;

/// <summary>
/// DTO représentant une fermeture planifiée du club de tir
/// </summary>
public class FermetureClubDto
{
    /// <summary>
    /// ID de la fermeture
    /// </summary>
    public int Id { get; set; }

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

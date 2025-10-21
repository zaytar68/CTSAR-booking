// ====================================================================
// AlveoleDto.cs : DTO pour représenter une alvéole
// ====================================================================

namespace CTSAR.Booking.DTOs;

/// <summary>
/// DTO pour transférer les données d'une alvéole
/// </summary>
public class AlveoleDto
{
    /// <summary>
    /// Identifiant unique de l'alvéole
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Nom de l'alvéole
    /// </summary>
    public string Nom { get; set; } = string.Empty;

    /// <summary>
    /// Ordre d'affichage
    /// </summary>
    public int Ordre { get; set; }

    /// <summary>
    /// Indique si l'alvéole est active
    /// </summary>
    public bool EstActive { get; set; }
}

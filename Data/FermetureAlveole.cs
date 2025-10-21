// ====================================================================
// FermetureAlveole.cs : Modèle représentant une fermeture planifiée
// ====================================================================

using System.ComponentModel.DataAnnotations;

namespace CTSAR.Booking.Data;

/// <summary>
/// Représente une période de fermeture d'une alvéole.
/// Empêche les réservations sur cette alvéole pendant la période.
/// </summary>
public class FermetureAlveole
{
    /// <summary>
    /// Identifiant unique de la fermeture
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// ID de l'alvéole concernée
    /// </summary>
    [Required]
    public int AlveoleId { get; set; }

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
    /// Raison de la fermeture
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Raison { get; set; } = string.Empty;

    /// <summary>
    /// Type de fermeture (Travaux, JourFerie, etc.)
    /// </summary>
    public TypeFermeture TypeFermeture { get; set; }

    /// <summary>
    /// Date de création de cette fermeture
    /// </summary>
    public DateTime DateCreation { get; set; } = DateTime.UtcNow;

    // ================================================================
    // RELATIONS NAVIGATION (Entity Framework)
    // ================================================================

    /// <summary>
    /// Alvéole concernée par cette fermeture
    /// </summary>
    public Alveole Alveole { get; set; } = null!;
}

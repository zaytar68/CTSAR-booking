// ====================================================================
// Alveole.cs : Modèle représentant un poste de tir
// ====================================================================

using System.ComponentModel.DataAnnotations;

namespace CTSAR.Booking.Data;

/// <summary>
/// Représente un poste de tir (alvéole) du club.
/// Maximum 20 alvéoles dans le système.
/// </summary>
public class Alveole
{
    /// <summary>
    /// Identifiant unique de l'alvéole
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Nom de l'alvéole (ex: "Alvéole 1", "Poste A", etc.)
    /// Obligatoire, unique, maximum 50 caractères
    /// </summary>
    [Required(ErrorMessage = "Le nom de l'alvéole est obligatoire")]
    [MaxLength(50, ErrorMessage = "Le nom ne peut pas dépasser 50 caractères")]
    public string Nom { get; set; } = string.Empty;

    /// <summary>
    /// Ordre d'affichage dans l'interface (tri croissant)
    /// Permet de réorganiser l'affichage des alvéoles
    /// </summary>
    public int Ordre { get; set; }

    /// <summary>
    /// Indique si l'alvéole est active (disponible pour réservation)
    /// false = désactivée (soft delete)
    /// </summary>
    public bool EstActive { get; set; } = true;

    /// <summary>
    /// Date de création de l'alvéole
    /// </summary>
    public DateTime DateCreation { get; set; } = DateTime.UtcNow;

    // ================================================================
    // RELATIONS NAVIGATION (Entity Framework)
    // ================================================================

    /// <summary>
    /// Liste des réservations liées à cette alvéole
    /// Relation many-to-many via ReservationAlveole
    /// </summary>
    public ICollection<ReservationAlveole> ReservationAlveoles { get; set; } = new List<ReservationAlveole>();

    /// <summary>
    /// Liste des fermetures planifiées pour cette alvéole
    /// </summary>
    public ICollection<FermetureAlveole> Fermetures { get; set; } = new List<FermetureAlveole>();
}

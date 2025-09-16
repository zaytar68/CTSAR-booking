using System.ComponentModel.DataAnnotations;

namespace CTSAR.Booking.Models;

/// <summary>
/// Représente une réservation d'alvéole pour une séance de tir
/// </summary>
public class Reservation
{
    public int Id { get; set; }

    /// <summary>
    /// Date de la séance de tir
    /// </summary>
    [Required]
    public DateTime DateSeance { get; set; }

    /// <summary>
    /// Heure de début de la séance
    /// </summary>
    [Required]
    public TimeSpan HeureDebut { get; set; }

    /// <summary>
    /// Heure de fin de la séance
    /// </summary>
    [Required]
    public TimeSpan HeureFin { get; set; }

    /// <summary>
    /// Alvéole réservée
    /// </summary>
    public int AlveoleId { get; set; }
    public virtual Alveole Alveole { get; set; } = null!;

    /// <summary>
    /// Membre qui a créé la réservation
    /// </summary>
    public int MembreCreateurId { get; set; }
    public virtual Membre MembreCreateur { get; set; } = null!;

    /// <summary>
    /// Moniteur qui valide la séance (obligatoire pour le tir)
    /// </summary>
    public int? MoniteurValidateurId { get; set; }
    public virtual Membre? MoniteurValidateur { get; set; }

    /// <summary>
    /// Date de création de la réservation
    /// </summary>
    public DateTime DateCreation { get; set; } = DateTime.Now;

    /// <summary>
    /// Date de validation par le moniteur
    /// </summary>
    public DateTime? DateValidation { get; set; }

    /// <summary>
    /// Commentaires additionnels
    /// </summary>
    [StringLength(500)]
    public string? Commentaires { get; set; }

    // Navigation properties
    public virtual ICollection<MembreReservation> MembresInscrits { get; set; } = new List<MembreReservation>();

    // Propriétés calculées
    /// <summary>
    /// Indique si la réservation est validée par un moniteur
    /// </summary>
    public bool EstValidee => MoniteurValidateurId.HasValue;

    /// <summary>
    /// Durée de la séance en heures
    /// </summary>
    public double DureeEnHeures => (HeureFin - HeureDebut).TotalHours;

    /// <summary>
    /// Nombre de membres inscrits
    /// </summary>
    public int NombreMembres => MembresInscrits.Count;

    /// <summary>
    /// Statut de la réservation pour l'affichage
    /// </summary>
    public string StatutAffichage =>
        EstValidee ? "Confirmée" : "En attente de validation";

    /// <summary>
    /// Couleur CSS pour l'affichage selon le statut
    /// </summary>
    public string CssClass =>
        EstValidee ? "reservation-confirmee" : "reservation-en-attente";
}

/// <summary>
/// Table de liaison entre membres et réservations
/// </summary>
public class MembreReservation
{
    public int MembreId { get; set; }
    public virtual Membre Membre { get; set; } = null!;

    public int ReservationId { get; set; }
    public virtual Reservation Reservation { get; set; } = null!;

    /// <summary>
    /// Date d'inscription à la réservation
    /// </summary>
    public DateTime DateInscription { get; set; } = DateTime.Now;

    /// <summary>
    /// Indique si le membre participera effectivement
    /// </summary>
    public bool EstConfirme { get; set; } = true;
}
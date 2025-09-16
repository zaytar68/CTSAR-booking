using System.ComponentModel.DataAnnotations;

namespace CTSAR.Booking.Models;

/// <summary>
/// Représente un membre du club de tir
/// </summary>
public class Membre
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Le nom est obligatoire")]
    [StringLength(100, ErrorMessage = "Le nom ne peut pas dépasser 100 caractères")]
    public string Nom { get; set; } = string.Empty;

    [Required(ErrorMessage = "Le prénom est obligatoire")]
    [StringLength(100, ErrorMessage = "Le prénom ne peut pas dépasser 100 caractères")]
    public string Prenom { get; set; } = string.Empty;

    [Required(ErrorMessage = "L'email est obligatoire")]
    [EmailAddress(ErrorMessage = "Format d'email invalide")]
    [StringLength(255, ErrorMessage = "L'email ne peut pas dépasser 255 caractères")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Rôle du membre dans le club
    /// </summary>
    public Role Role { get; set; } = Role.Membre;

    /// <summary>
    /// Langue préférée pour l'interface (fr-FR, de-DE, en-US)
    /// </summary>
    [StringLength(10)]
    public string LanguePreferee { get; set; } = "fr-FR";

    /// <summary>
    /// Préférences de notification
    /// </summary>
    public bool NotificationsEmail { get; set; } = true;
    public bool NotificationsWhatsApp { get; set; } = false;

    /// <summary>
    /// Date de création du compte
    /// </summary>
    public DateTime DateCreation { get; set; } = DateTime.Now;

    /// <summary>
    /// Indique si le membre est actif
    /// </summary>
    public bool EstActif { get; set; } = true;

    // Navigation properties
    public virtual ICollection<Reservation> ReservationsCreees { get; set; } = new List<Reservation>();
    public virtual ICollection<Reservation> ReservationsValidees { get; set; } = new List<Reservation>();

    // Méthodes utiles
    public string NomComplet => $"{Prenom} {Nom}";

    public bool EstMoniteur => Role == Role.Moniteur || Role == Role.Administrateur;

    public bool PeutGererMembres => Role == Role.Administrateur;
}
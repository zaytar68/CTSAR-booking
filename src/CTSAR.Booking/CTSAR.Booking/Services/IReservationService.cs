using CTSAR.Booking.Models;

namespace CTSAR.Booking.Services;

/// <summary>
/// Service de gestion des réservations d'alvéoles
/// </summary>
public interface IReservationService
{
    /// <summary>
    /// Crée une nouvelle réservation
    /// </summary>
    Task<ResultatReservation> CreerReservationAsync(CreerReservationDto reservationDto);

    /// <summary>
    /// Récupère une réservation par ID
    /// </summary>
    Task<ReservationDto?> GetReservationAsync(int id);

    /// <summary>
    /// Met à jour une réservation existante
    /// </summary>
    Task<ResultatReservation> ModifierReservationAsync(int id, ModifierReservationDto reservationDto);

    /// <summary>
    /// Supprime une réservation
    /// </summary>
    Task<bool> SupprimerReservationAsync(int id);

    /// <summary>
    /// Ajoute un membre à une réservation
    /// </summary>
    Task<bool> AjouterMembreReservationAsync(int reservationId, int membreId);

    /// <summary>
    /// Retire un membre d'une réservation
    /// </summary>
    Task<bool> RetirerMembreReservationAsync(int reservationId, int membreId);

    /// <summary>
    /// Valide une réservation par un moniteur
    /// </summary>
    Task<bool> ValiderReservationAsync(int reservationId, int moniteurId);

    /// <summary>
    /// Annule la validation d'une réservation
    /// </summary>
    Task<bool> AnnulerValidationAsync(int reservationId);
}

/// <summary>
/// DTO pour la création d'une réservation
/// </summary>
public class CreerReservationDto
{
    public DateTime DateSeance { get; set; }
    public TimeSpan HeureDebut { get; set; }
    public TimeSpan HeureFin { get; set; }
    public int AlveoleId { get; set; }
    public int MembreCreateurId { get; set; }
    public List<int> MembresInscritsIds { get; set; } = new();
    public string? Commentaires { get; set; }
}

/// <summary>
/// DTO pour la modification d'une réservation
/// </summary>
public class ModifierReservationDto
{
    public DateTime DateSeance { get; set; }
    public TimeSpan HeureDebut { get; set; }
    public TimeSpan HeureFin { get; set; }
    public int AlveoleId { get; set; }
    public List<int> MembresInscritsIds { get; set; } = new();
    public string? Commentaires { get; set; }
}

/// <summary>
/// Résultat d'une opération de réservation
/// </summary>
public class ResultatReservation
{
    public bool Succes { get; set; }
    public string? MessageErreur { get; set; }
    public List<string> Erreurs { get; set; } = new();
    public ReservationDto? Reservation { get; set; }

    public static ResultatReservation Success(ReservationDto reservation)
    {
        return new ResultatReservation
        {
            Succes = true,
            Reservation = reservation
        };
    }

    public static ResultatReservation Error(string message, List<string>? erreurs = null)
    {
        return new ResultatReservation
        {
            Succes = false,
            MessageErreur = message,
            Erreurs = erreurs ?? new List<string>()
        };
    }
}

/// <summary>
/// Énumération des erreurs de réservation
/// </summary>
public enum TypeErreurReservation
{
    AlveoleInexistante,
    AlveoleInactive,
    CreneauIndisponible,
    ConfligHoraire,
    HeuresOuverture,
    NombreMaxTireurs,
    MembreInexistant,
    MoniteurInexistant,
    ReservationInexistante,
    AutorisationInsuffisante
}
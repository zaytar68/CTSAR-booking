using CTSAR.Booking.Models;

namespace CTSAR.Booking.Services;

/// <summary>
/// Service de gestion du planning des réservations d'alvéoles
/// </summary>
public interface IPlanningService
{
    /// <summary>
    /// Récupère le planning mensuel avec toutes les réservations
    /// </summary>
    Task<PlanningMensuelDto> GetPlanningMensuelAsync(int annee, int mois);

    /// <summary>
    /// Récupère les réservations pour une date spécifique
    /// </summary>
    Task<List<ReservationDto>> GetReservationsJourAsync(DateTime date);

    /// <summary>
    /// Vérifie la disponibilité d'une alvéole pour un créneau
    /// </summary>
    Task<bool> EstDisponibleAsync(int alveoleId, DateTime date, TimeSpan heureDebut, TimeSpan heureFin);

    /// <summary>
    /// Récupère les créneaux disponibles pour une alvéole sur une date
    /// </summary>
    Task<List<CreneauDisponibleDto>> GetCreneauxDisponiblesAsync(int alveoleId, DateTime date);
}

/// <summary>
/// DTO représentant le planning mensuel complet
/// </summary>
public class PlanningMensuelDto
{
    public int Annee { get; set; }
    public int Mois { get; set; }
    public string NomMois { get; set; } = string.Empty;
    public List<JourPlanningDto> Jours { get; set; } = new();
    public List<AlveoleDto> Alveoles { get; set; } = new();
}

/// <summary>
/// DTO représentant un jour dans le planning
/// </summary>
public class JourPlanningDto
{
    public DateTime Date { get; set; }
    public DayOfWeek JourSemaine { get; set; }
    public bool EstAujourdhui { get; set; }
    public bool EstWeekend { get; set; }
    public List<ReservationDto> Reservations { get; set; } = new();
}

/// <summary>
/// DTO représentant une réservation dans le planning
/// </summary>
public class ReservationDto
{
    public int Id { get; set; }
    public DateTime DateSeance { get; set; }
    public TimeSpan HeureDebut { get; set; }
    public TimeSpan HeureFin { get; set; }
    public string AlveoleNom { get; set; } = string.Empty;
    public int AlveoleId { get; set; }
    public bool EstValidee { get; set; }
    public string? MoniteurValidateur { get; set; }
    public int NombreMembres { get; set; }
    public List<string> NomsMemebres { get; set; } = new();
    public string StatutCss => EstValidee ? "reservation-confirmee" : "reservation-en-attente";
    public string StatutTexte => EstValidee ? "Confirmée" : "En attente";
    public string CouleurBadge => EstValidee ? "success" : "warning";
}

/// <summary>
/// DTO représentant une alvéole
/// </summary>
public class AlveoleDto
{
    public int Id { get; set; }
    public string Nom { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool EstActive { get; set; }
    public int NombreMaxTireurs { get; set; }
}

/// <summary>
/// DTO représentant un créneau disponible
/// </summary>
public class CreneauDisponibleDto
{
    public TimeSpan HeureDebut { get; set; }
    public TimeSpan HeureFin { get; set; }
    public bool EstDisponible { get; set; }
    public int PlacesRestantes { get; set; }
    public string Description => $"{HeureDebut:hh\\:mm} - {HeureFin:hh\\:mm}";
}
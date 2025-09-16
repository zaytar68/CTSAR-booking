using CTSAR.Booking.Data;
using CTSAR.Booking.Models;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace CTSAR.Booking.Services;

/// <summary>
/// Service de gestion du planning des réservations d'alvéoles
/// </summary>
public class PlanningService : IPlanningService
{
    private readonly ApplicationDbContext _context;
    private static readonly CultureInfo FrenchCulture = new("fr-FR");

    // Créneaux horaires standards du club
    private static readonly List<(TimeSpan debut, TimeSpan fin)> CreneauxStandard = new()
    {
        (new TimeSpan(8, 0, 0), new TimeSpan(10, 0, 0)),   // 8h-10h
        (new TimeSpan(10, 0, 0), new TimeSpan(12, 0, 0)),  // 10h-12h
        (new TimeSpan(14, 0, 0), new TimeSpan(16, 0, 0)),  // 14h-16h
        (new TimeSpan(16, 0, 0), new TimeSpan(17, 0, 0))   // 16h-17h
    };

    public PlanningService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PlanningMensuelDto> GetPlanningMensuelAsync(int annee, int mois)
    {
        var premierJourMois = new DateTime(annee, mois, 1);
        var dernierJourMois = premierJourMois.AddMonths(1).AddDays(-1);

        // Récupérer toutes les réservations du mois
        var reservations = await _context.Reservations
            .Include(r => r.Alveole)
            .Include(r => r.MembreCreateur)
            .Include(r => r.MoniteurValidateur)
            .Include(r => r.MembresInscrits)
                .ThenInclude(mr => mr.Membre)
            .Where(r => r.DateSeance >= premierJourMois && r.DateSeance <= dernierJourMois)
            .ToListAsync();

        // Tri côté client pour éviter les problèmes SQLite avec TimeSpan
        reservations = reservations
            .OrderBy(r => r.DateSeance)
            .ThenBy(r => r.HeureDebut)
            .ToList();

        // Récupérer toutes les alvéoles actives
        var alveoles = await _context.Alveoles
            .Where(a => a.EstActive)
            .OrderBy(a => a.Nom)
            .ToListAsync();

        // Construire le planning
        var jours = new List<JourPlanningDto>();
        var dateActuelle = premierJourMois;

        while (dateActuelle <= dernierJourMois)
        {
            var reservationsJour = reservations
                .Where(r => r.DateSeance.Date == dateActuelle.Date)
                .Select(r => MapToReservationDto(r))
                .ToList();

            jours.Add(new JourPlanningDto
            {
                Date = dateActuelle,
                JourSemaine = dateActuelle.DayOfWeek,
                EstAujourdhui = dateActuelle.Date == DateTime.Today,
                EstWeekend = dateActuelle.DayOfWeek == DayOfWeek.Saturday || dateActuelle.DayOfWeek == DayOfWeek.Sunday,
                Reservations = reservationsJour
            });

            dateActuelle = dateActuelle.AddDays(1);
        }

        return new PlanningMensuelDto
        {
            Annee = annee,
            Mois = mois,
            NomMois = FrenchCulture.DateTimeFormat.GetMonthName(mois),
            Jours = jours,
            Alveoles = alveoles.Select(MapToAlveoleDto).ToList()
        };
    }

    public async Task<List<ReservationDto>> GetReservationsJourAsync(DateTime date)
    {
        var reservations = await _context.Reservations
            .Include(r => r.Alveole)
            .Include(r => r.MembreCreateur)
            .Include(r => r.MoniteurValidateur)
            .Include(r => r.MembresInscrits)
                .ThenInclude(mr => mr.Membre)
            .Where(r => r.DateSeance.Date == date.Date)
            .ToListAsync();

        // Tri côté client pour éviter les problèmes SQLite avec TimeSpan
        return reservations
            .OrderBy(r => r.HeureDebut)
            .ThenBy(r => r.AlveoleId)
            .Select(MapToReservationDto)
            .ToList();
    }

    public async Task<bool> EstDisponibleAsync(int alveoleId, DateTime date, TimeSpan heureDebut, TimeSpan heureFin)
    {
        // Vérifier les heures d'ouverture
        if (!EstDansHeuresOuverture(date, heureDebut, heureFin))
            return false;

        // Vérifier l'alvéole
        var alveole = await _context.Alveoles.FindAsync(alveoleId);
        if (alveole == null || !alveole.EstActive)
            return false;

        // Vérifier si l'alvéole est disponible (pas de fermeture programmée)
        if (!alveole.EstDisponible(date, heureDebut, heureFin))
            return false;

        // Vérifier les conflits de réservation
        var conflits = await _context.Reservations
            .Where(r => r.AlveoleId == alveoleId &&
                       r.DateSeance.Date == date.Date &&
                       r.HeureDebut < heureFin &&
                       r.HeureFin > heureDebut)
            .CountAsync();

        return conflits == 0;
    }

    public async Task<List<CreneauDisponibleDto>> GetCreneauxDisponiblesAsync(int alveoleId, DateTime date)
    {
        var alveole = await _context.Alveoles.FindAsync(alveoleId);
        if (alveole == null || !alveole.EstActive)
            return new List<CreneauDisponibleDto>();

        var creneaux = new List<CreneauDisponibleDto>();
        var reservationsJour = await _context.Reservations
            .Include(r => r.MembresInscrits)
            .Where(r => r.AlveoleId == alveoleId && r.DateSeance.Date == date.Date)
            .ToListAsync();

        // Vérifier chaque créneau standard
        foreach (var (debut, fin) in CreneauxStandard)
        {
            // Vérifier si c'est dans les heures d'ouverture
            if (!EstDansHeuresOuverture(date, debut, fin))
                continue;

            var reservationExistante = reservationsJour
                .FirstOrDefault(r => r.HeureDebut == debut && r.HeureFin == fin);

            var placesOccupees = reservationExistante?.MembresInscrits.Count ?? 0;
            var placesRestantes = alveole.NombreMaxTireurs - placesOccupees;

            creneaux.Add(new CreneauDisponibleDto
            {
                HeureDebut = debut,
                HeureFin = fin,
                EstDisponible = placesRestantes > 0,
                PlacesRestantes = Math.Max(0, placesRestantes)
            });
        }

        return creneaux;
    }

    private static bool EstDansHeuresOuverture(DateTime date, TimeSpan heureDebut, TimeSpan heureFin)
    {
        // Heures d'ouverture du club selon les spécifications
        // Lundi-Samedi: 8h-12h et 14h-17h
        // Dimanche: 8h-12h

        var jourSemaine = date.DayOfWeek;

        // Dimanche : seulement le matin
        if (jourSemaine == DayOfWeek.Sunday)
        {
            return heureDebut >= new TimeSpan(8, 0, 0) && heureFin <= new TimeSpan(12, 0, 0);
        }

        // Lundi-Samedi : matin et après-midi
        var matinOk = heureDebut >= new TimeSpan(8, 0, 0) && heureFin <= new TimeSpan(12, 0, 0);
        var apresmidiOk = heureDebut >= new TimeSpan(14, 0, 0) && heureFin <= new TimeSpan(17, 0, 0);

        return matinOk || apresmidiOk;
    }

    private static ReservationDto MapToReservationDto(Reservation reservation)
    {
        return new ReservationDto
        {
            Id = reservation.Id,
            DateSeance = reservation.DateSeance,
            HeureDebut = reservation.HeureDebut,
            HeureFin = reservation.HeureFin,
            AlveoleNom = reservation.Alveole.Nom,
            AlveoleId = reservation.AlveoleId,
            EstValidee = reservation.EstValidee,
            MoniteurValidateur = reservation.MoniteurValidateur?.NomComplet,
            NombreMembres = reservation.MembresInscrits.Count,
            NomsMemebres = reservation.MembresInscrits.Select(mr => mr.Membre.NomComplet).ToList()
        };
    }

    private static AlveoleDto MapToAlveoleDto(Alveole alveole)
    {
        return new AlveoleDto
        {
            Id = alveole.Id,
            Nom = alveole.Nom,
            Description = alveole.Description,
            EstActive = alveole.EstActive,
            NombreMaxTireurs = alveole.NombreMaxTireurs
        };
    }
}
using CTSAR.Booking.Data;
using CTSAR.Booking.Models;
using Microsoft.EntityFrameworkCore;

namespace CTSAR.Booking.Services;

/// <summary>
/// Service de gestion des réservations d'alvéoles
/// </summary>
public class ReservationService : IReservationService
{
    private readonly ApplicationDbContext _context;
    private readonly IPlanningService _planningService;

    public ReservationService(ApplicationDbContext context, IPlanningService planningService)
    {
        _context = context;
        _planningService = planningService;
    }

    public async Task<ResultatReservation> CreerReservationAsync(CreerReservationDto reservationDto)
    {
        // Validation de l'alvéole
        var alveole = await _context.Alveoles.FindAsync(reservationDto.AlveoleId);
        if (alveole == null)
            return ResultatReservation.Error("Alvéole introuvable", new List<string> { "L'alvéole spécifiée n'existe pas." });

        if (!alveole.EstActive)
            return ResultatReservation.Error("Alvéole inactive", new List<string> { "Cette alvéole n'est pas disponible." });

        // Validation du membre créateur
        var membreCreateur = await _context.Membres.FindAsync(reservationDto.MembreCreateurId);
        if (membreCreateur == null || !membreCreateur.EstActif)
            return ResultatReservation.Error("Membre créateur invalide", new List<string> { "Le membre créateur est introuvable ou inactif." });

        // Validation de la disponibilité
        var estDisponible = await _planningService.EstDisponibleAsync(
            reservationDto.AlveoleId,
            reservationDto.DateSeance,
            reservationDto.HeureDebut,
            reservationDto.HeureFin);

        if (!estDisponible)
            return ResultatReservation.Error("Créneau indisponible", new List<string> { "Ce créneau n'est pas disponible pour cette alvéole." });

        // Validation du nombre de participants
        var nombreTotalParticipants = reservationDto.MembresInscritsIds.Count + 1; // +1 pour le créateur
        if (nombreTotalParticipants > alveole.NombreMaxTireurs)
            return ResultatReservation.Error("Trop de participants",
                new List<string> { $"Maximum {alveole.NombreMaxTireurs} tireurs autorisés sur cette alvéole." });

        // Validation des membres inscrits
        var membresInscrits = new List<Membre>();
        if (reservationDto.MembresInscritsIds.Any())
        {
            membresInscrits = await _context.Membres
                .Where(m => reservationDto.MembresInscritsIds.Contains(m.Id) && m.EstActif)
                .ToListAsync();

            if (membresInscrits.Count != reservationDto.MembresInscritsIds.Count)
                return ResultatReservation.Error("Membres invalides",
                    new List<string> { "Certains membres sélectionnés sont introuvables ou inactifs." });
        }

        try
        {
            // Création de la réservation
            var reservation = new Reservation
            {
                DateSeance = reservationDto.DateSeance,
                HeureDebut = reservationDto.HeureDebut,
                HeureFin = reservationDto.HeureFin,
                AlveoleId = reservationDto.AlveoleId,
                MembreCreateurId = reservationDto.MembreCreateurId,
                Commentaires = reservationDto.Commentaires,
                DateCreation = DateTime.UtcNow
            };

            _context.Reservations.Add(reservation);
            await _context.SaveChangesAsync();

            // Ajout des membres inscrits
            var membresReservations = new List<MembreReservation>();

            // Toujours ajouter le créateur
            membresReservations.Add(new MembreReservation
            {
                MembreId = reservationDto.MembreCreateurId,
                ReservationId = reservation.Id,
                DateInscription = DateTime.UtcNow,
                EstConfirme = true
            });

            // Ajouter les autres membres
            foreach (var membre in membresInscrits.Where(m => m.Id != reservationDto.MembreCreateurId))
            {
                membresReservations.Add(new MembreReservation
                {
                    MembreId = membre.Id,
                    ReservationId = reservation.Id,
                    DateInscription = DateTime.UtcNow,
                    EstConfirme = false // En attente de confirmation
                });
            }

            _context.MembresReservations.AddRange(membresReservations);
            await _context.SaveChangesAsync();

            // Recharger la réservation avec les relations
            var reservationComplete = await _context.Reservations
                .Include(r => r.Alveole)
                .Include(r => r.MembreCreateur)
                .Include(r => r.MembresInscrits)
                    .ThenInclude(mr => mr.Membre)
                .FirstAsync(r => r.Id == reservation.Id);

            var reservationResult = MapToReservationDto(reservationComplete);
            return ResultatReservation.Success(reservationResult);
        }
        catch (Exception ex)
        {
            return ResultatReservation.Error("Erreur lors de la création",
                new List<string> { $"Une erreur inattendue s'est produite : {ex.Message}" });
        }
    }

    public async Task<ReservationDto?> GetReservationAsync(int id)
    {
        var reservation = await _context.Reservations
            .Include(r => r.Alveole)
            .Include(r => r.MembreCreateur)
            .Include(r => r.MoniteurValidateur)
            .Include(r => r.MembresInscrits)
                .ThenInclude(mr => mr.Membre)
            .FirstOrDefaultAsync(r => r.Id == id);

        return reservation != null ? MapToReservationDto(reservation) : null;
    }

    public async Task<ResultatReservation> ModifierReservationAsync(int id, ModifierReservationDto reservationDto)
    {
        var reservation = await _context.Reservations
            .Include(r => r.MembresInscrits)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (reservation == null)
            return ResultatReservation.Error("Réservation introuvable");

        // Validation de l'alvéole
        var alveole = await _context.Alveoles.FindAsync(reservationDto.AlveoleId);
        if (alveole == null || !alveole.EstActive)
            return ResultatReservation.Error("Alvéole invalide");

        // Validation de la disponibilité (exclure la réservation actuelle)
        var conflits = await _context.Reservations
            .Where(r => r.Id != id &&
                       r.AlveoleId == reservationDto.AlveoleId &&
                       r.DateSeance.Date == reservationDto.DateSeance.Date &&
                       r.HeureDebut < reservationDto.HeureFin &&
                       r.HeureFin > reservationDto.HeureDebut)
            .CountAsync();

        if (conflits > 0)
            return ResultatReservation.Error("Conflit horaire détecté");

        try
        {
            // Mise à jour de la réservation
            reservation.DateSeance = reservationDto.DateSeance;
            reservation.HeureDebut = reservationDto.HeureDebut;
            reservation.HeureFin = reservationDto.HeureFin;
            reservation.AlveoleId = reservationDto.AlveoleId;
            reservation.Commentaires = reservationDto.Commentaires;

            // Mise à jour des membres inscrits
            _context.MembresReservations.RemoveRange(reservation.MembresInscrits);

            var nouveauxMembres = await _context.Membres
                .Where(m => reservationDto.MembresInscritsIds.Contains(m.Id) && m.EstActif)
                .ToListAsync();

            var membresReservations = nouveauxMembres.Select(m => new MembreReservation
            {
                MembreId = m.Id,
                ReservationId = reservation.Id,
                DateInscription = DateTime.UtcNow,
                EstConfirme = m.Id == reservation.MembreCreateurId
            }).ToList();

            _context.MembresReservations.AddRange(membresReservations);
            await _context.SaveChangesAsync();

            var reservationMiseAJour = await GetReservationAsync(id);
            return ResultatReservation.Success(reservationMiseAJour!);
        }
        catch (Exception ex)
        {
            return ResultatReservation.Error($"Erreur lors de la modification : {ex.Message}");
        }
    }

    public async Task<bool> SupprimerReservationAsync(int id)
    {
        var reservation = await _context.Reservations.FindAsync(id);
        if (reservation == null)
            return false;

        try
        {
            _context.Reservations.Remove(reservation);
            await _context.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> AjouterMembreReservationAsync(int reservationId, int membreId)
    {
        var reservation = await _context.Reservations
            .Include(r => r.Alveole)
            .Include(r => r.MembresInscrits)
            .FirstOrDefaultAsync(r => r.Id == reservationId);

        if (reservation == null)
            return false;

        var membre = await _context.Membres.FindAsync(membreId);
        if (membre == null || !membre.EstActif)
            return false;

        // Vérifier si le membre n'est pas déjà inscrit
        if (reservation.MembresInscrits.Any(mr => mr.MembreId == membreId))
            return false;

        // Vérifier le nombre maximum de tireurs
        if (reservation.MembresInscrits.Count >= reservation.Alveole.NombreMaxTireurs)
            return false;

        try
        {
            var membreReservation = new MembreReservation
            {
                MembreId = membreId,
                ReservationId = reservationId,
                DateInscription = DateTime.UtcNow,
                EstConfirme = false
            };

            _context.MembresReservations.Add(membreReservation);
            await _context.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> RetirerMembreReservationAsync(int reservationId, int membreId)
    {
        var membreReservation = await _context.MembresReservations
            .FirstOrDefaultAsync(mr => mr.ReservationId == reservationId && mr.MembreId == membreId);

        if (membreReservation == null)
            return false;

        try
        {
            _context.MembresReservations.Remove(membreReservation);
            await _context.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> ValiderReservationAsync(int reservationId, int moniteurId)
    {
        var reservation = await _context.Reservations.FindAsync(reservationId);
        if (reservation == null)
            return false;

        var moniteur = await _context.Membres.FindAsync(moniteurId);
        if (moniteur == null || !moniteur.EstActif || moniteur.Role != Role.Moniteur)
            return false;

        try
        {
            reservation.MoniteurValidateurId = moniteurId;
            reservation.DateValidation = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> AnnulerValidationAsync(int reservationId)
    {
        var reservation = await _context.Reservations.FindAsync(reservationId);
        if (reservation == null)
            return false;

        try
        {
            reservation.MoniteurValidateurId = null;
            reservation.DateValidation = null;
            await _context.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
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
}
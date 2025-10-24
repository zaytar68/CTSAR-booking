// ====================================================================
// ReservationService.cs : Service de gestion des inscriptions (réservations)
// ====================================================================

using Microsoft.EntityFrameworkCore;
using CTSAR.Booking.Data;
using CTSAR.Booking.DTOs;

namespace CTSAR.Booking.Services;

/// <summary>
/// Service pour gérer les inscriptions aux séances de tir.
/// Gère la création, la modification, les participants et les statuts.
/// </summary>
public class ReservationService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ReservationService> _logger;

    public ReservationService(ApplicationDbContext context, ILogger<ReservationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // ================================================================
    // MÉTHODES DE LECTURE
    // ================================================================

    /// <summary>
    /// Récupère toutes les inscriptions d'un mois donné
    /// </summary>
    public async Task<List<ReservationDto>> GetReservationsForMonthAsync(int year, int month)
    {
        try
        {
            _logger.LogInformation($"Récupération des inscriptions pour {month}/{year}");

            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1);

            var reservations = await _context.Reservations
                .Include(r => r.ReservationAlveoles)
                    .ThenInclude(ra => ra.Alveole)
                .Include(r => r.Participants)
                    .ThenInclude(p => p.User)
                .Include(r => r.CreatedBy)
                .Where(r => r.DateDebut >= startDate && r.DateDebut < endDate)
                .OrderBy(r => r.DateDebut)
                .AsNoTracking()
                .ToListAsync();

            var dtos = reservations.Select(r => MapToReservationDto(r)).ToList();

            _logger.LogInformation($"{dtos.Count} inscriptions récupérées pour {month}/{year}");
            return dtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Erreur lors de la récupération des inscriptions pour {month}/{year}");
            throw;
        }
    }

    /// <summary>
    /// Récupère une inscription complète par son ID
    /// </summary>
    public async Task<ReservationDto?> GetReservationByIdAsync(int id)
    {
        try
        {
            _logger.LogInformation($"Récupération de l'inscription {id}");

            var reservation = await _context.Reservations
                .Include(r => r.ReservationAlveoles)
                    .ThenInclude(ra => ra.Alveole)
                .Include(r => r.Participants)
                    .ThenInclude(p => p.User)
                .Include(r => r.CreatedBy)
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == id);

            if (reservation == null)
            {
                _logger.LogWarning($"Inscription {id} non trouvée");
                return null;
            }

            return MapToReservationDto(reservation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Erreur lors de la récupération de l'inscription {id}");
            throw;
        }
    }

    // ================================================================
    // CRÉATION D'INSCRIPTION
    // ================================================================

    /// <summary>
    /// Crée une nouvelle inscription (séance de tir)
    /// Le créateur est automatiquement inscrit comme premier participant
    /// </summary>
    public async Task<(bool Success, string Message, ReservationDto? Reservation)> CreateReservationAsync(
        CreateReservationDto dto,
        string userId,
        bool isMoniteur)
    {
        try
        {
            _logger.LogInformation($"Création d'inscription par utilisateur {userId}");

            // 1. Valider les alvéoles
            var alveoles = await _context.Alveoles
                .Where(a => dto.AlveoleIds.Contains(a.Id) && a.EstActive)
                .ToListAsync();

            if (alveoles.Count != dto.AlveoleIds.Count)
            {
                _logger.LogWarning("Une ou plusieurs alvéoles invalides ou inactives");
                return (false, "Une ou plusieurs alvéoles sélectionnées sont invalides ou inactives", null);
            }

            // 2. Valider les dates
            if (dto.DateFin <= dto.DateDebut)
            {
                _logger.LogWarning("Date de fin doit être après date de début");
                return (false, "La date de fin doit être après la date de début", null);
            }

            // 3. Vérifier les fermetures d'alvéoles
            var alveolesFermees = await GetFermeturesAsync(dto.AlveoleIds, dto.DateDebut, dto.DateFin);
            if (alveolesFermees.Any())
            {
                var message = $"Les alvéoles suivantes sont fermées pendant cette période : {string.Join(", ", alveolesFermees)}";
                _logger.LogWarning(message);
                return (false, message, null);
            }

            // 4. Vérifier les chevauchements
            if (await HasOverlapAsync(dto.AlveoleIds, dto.DateDebut, dto.DateFin))
            {
                _logger.LogWarning("Chevauchement détecté avec une inscription existante");
                return (false, "Une ou plusieurs alvéoles sont déjà réservées sur ce créneau", null);
            }

            // 5. Créer l'inscription
            if (!int.TryParse(userId, out int userIdInt))
            {
                _logger.LogError("userId invalide : {userId}", userId);
                return (false, "Identifiant utilisateur invalide", null);
            }

            var reservation = new Reservation
            {
                DateDebut = dto.DateDebut,
                DateFin = dto.DateFin,
                Commentaire = dto.Commentaire,
                CreatedByUserId = userIdInt,
                DateCreation = DateTime.UtcNow,
                StatutReservation = isMoniteur ? StatutReservation.Confirmee : StatutReservation.EnAttente
            };

            _context.Reservations.Add(reservation);
            await _context.SaveChangesAsync();

            // 6. Lier les alvéoles
            foreach (var alveoleId in dto.AlveoleIds)
            {
                _context.ReservationAlveoles.Add(new ReservationAlveole
                {
                    ReservationId = reservation.Id,
                    AlveoleId = alveoleId
                });
            }

            // 7. Inscrire le créateur comme premier participant
            _context.ReservationParticipants.Add(new ReservationParticipant
            {
                ReservationId = reservation.Id,
                UserId = userIdInt,
                EstMoniteur = isMoniteur,
                DateInscription = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            // 8. Recharger avec toutes les relations pour le DTO
            var created = await GetReservationByIdAsync(reservation.Id);

            _logger.LogInformation($"Inscription {reservation.Id} créée avec succès");
            return (true, "Inscription créée avec succès", created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la création de l'inscription");
            return (false, $"Erreur : {ex.Message}", null);
        }
    }

    // ================================================================
    // GESTION DES PARTICIPANTS
    // ================================================================

    /// <summary>
    /// Ajoute un participant à une inscription existante
    /// </summary>
    public async Task<(bool Success, string Message)> AddParticipantAsync(
        int reservationId,
        string userId,
        bool isMoniteur)
    {
        try
        {
            _logger.LogInformation($"Ajout du participant {userId} à l'inscription {reservationId}");

            if (!int.TryParse(userId, out int userIdInt))
            {
                _logger.LogError("userId invalide : {userId}", userId);
                return (false, "Identifiant utilisateur invalide");
            }

            var reservation = await _context.Reservations
                .Include(r => r.Participants)
                .FirstOrDefaultAsync(r => r.Id == reservationId);

            if (reservation == null)
            {
                _logger.LogWarning($"Inscription {reservationId} non trouvée");
                return (false, "Inscription non trouvée");
            }

            // Vérifier si déjà inscrit
            if (reservation.Participants.Any(p => p.UserId == userIdInt))
            {
                _logger.LogWarning($"Utilisateur {userId} déjà inscrit");
                return (false, "Vous êtes déjà inscrit à cette séance");
            }

            // Ajouter le participant
            _context.ReservationParticipants.Add(new ReservationParticipant
            {
                ReservationId = reservationId,
                UserId = userIdInt,
                EstMoniteur = isMoniteur,
                DateInscription = DateTime.UtcNow
            });

            // Recalculer le statut si moniteur ajouté
            if (isMoniteur)
            {
                reservation.StatutReservation = StatutReservation.Confirmee;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Participant {userId} ajouté avec succès");
            return (true, "Inscription réussie");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Erreur lors de l'ajout du participant {userId}");
            return (false, $"Erreur : {ex.Message}");
        }
    }

    /// <summary>
    /// Retire un participant d'une inscription
    /// </summary>
    public async Task<(bool Success, string Message)> RemoveParticipantAsync(
        int reservationId,
        string userId)
    {
        try
        {
            _logger.LogInformation($"Retrait du participant {userId} de l'inscription {reservationId}");

            if (!int.TryParse(userId, out int userIdInt))
            {
                _logger.LogError("userId invalide : {userId}", userId);
                return (false, "Identifiant utilisateur invalide");
            }

            var reservation = await _context.Reservations
                .Include(r => r.Participants)
                .FirstOrDefaultAsync(r => r.Id == reservationId);

            if (reservation == null)
            {
                _logger.LogWarning($"Inscription {reservationId} non trouvée");
                return (false, "Inscription non trouvée");
            }

            var participant = reservation.Participants.FirstOrDefault(p => p.UserId == userIdInt);
            if (participant == null)
            {
                _logger.LogWarning($"Utilisateur {userId} non inscrit");
                return (false, "Vous n'êtes pas inscrit à cette séance");
            }

            var wasMoniteur = participant.EstMoniteur;

            // Retirer le participant
            _context.ReservationParticipants.Remove(participant);

            // Si plus aucun participant, supprimer l'inscription complète
            if (reservation.Participants.Count == 1) // Le seul participant restant est celui qu'on retire
            {
                _context.Reservations.Remove(reservation);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Inscription {reservationId} supprimée (plus aucun participant)");
                return (true, "Vous vous êtes désinscrit. L'inscription a été supprimée car vous étiez le dernier participant.");
            }

            // Recalculer le statut si c'était un moniteur
            if (wasMoniteur)
            {
                await RecalculateStatutAsync(reservation);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Participant {userId} retiré avec succès");
            return (true, "Vous vous êtes désinscrit avec succès");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Erreur lors du retrait du participant {userId}");
            return (false, $"Erreur : {ex.Message}");
        }
    }

    // ================================================================
    // MÉTHODES SPÉCIFIQUES MONITEURS
    // ================================================================

    /// <summary>
    /// Moniteur s'inscrit et confirme l'inscription (raccourci)
    /// </summary>
    public async Task<(bool Success, string Message)> ConfirmReservationAsMoniteurAsync(
        int reservationId,
        string moniteurId)
    {
        return await AddParticipantAsync(reservationId, moniteurId, isMoniteur: true);
    }

    /// <summary>
    /// Moniteur se désinscrit et recalcule le statut (raccourci)
    /// </summary>
    public async Task<(bool Success, string Message)> RemoveMoniteurPresenceAsync(
        int reservationId,
        string moniteurId)
    {
        return await RemoveParticipantAsync(reservationId, moniteurId);
    }

    // ================================================================
    // MODIFICATION DU COMMENTAIRE
    // ================================================================

    /// <summary>
    /// Met à jour le commentaire d'une inscription
    /// Tous les participants peuvent modifier le commentaire
    /// </summary>
    public async Task<(bool Success, string Message)> UpdateCommentaireAsync(
        int reservationId,
        string userId,
        string newCommentaire)
    {
        try
        {
            _logger.LogInformation($"Mise à jour du commentaire de l'inscription {reservationId}");

            if (!int.TryParse(userId, out int userIdInt))
            {
                _logger.LogError("userId invalide : {userId}", userId);
                return (false, "Identifiant utilisateur invalide");
            }

            var reservation = await _context.Reservations
                .Include(r => r.Participants)
                .FirstOrDefaultAsync(r => r.Id == reservationId);

            if (reservation == null)
            {
                _logger.LogWarning($"Inscription {reservationId} non trouvée");
                return (false, "Inscription non trouvée");
            }

            // Vérifier que l'utilisateur est inscrit
            if (!reservation.Participants.Any(p => p.UserId == userIdInt))
            {
                _logger.LogWarning($"Utilisateur {userId} non autorisé à modifier le commentaire");
                return (false, "Seuls les participants peuvent modifier le commentaire");
            }

            reservation.Commentaire = newCommentaire;
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Commentaire mis à jour pour l'inscription {reservationId}");
            return (true, "Commentaire mis à jour avec succès");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Erreur lors de la mise à jour du commentaire");
            return (false, $"Erreur : {ex.Message}");
        }
    }

    // ================================================================
    // SUPPRESSION
    // ================================================================

    /// <summary>
    /// Supprime une inscription ou retire un participant
    /// - Admin : supprime complètement l'inscription
    /// - Utilisateur normal : se retire des participants (comme RemoveParticipantAsync)
    /// </summary>
    public async Task<(bool Success, string Message)> DeleteReservationAsync(
        int reservationId,
        string userId,
        bool isAdmin)
    {
        try
        {
            _logger.LogInformation($"Suppression de l'inscription {reservationId} par {userId} (Admin: {isAdmin})");

            var reservation = await _context.Reservations
                .Include(r => r.Participants)
                .FirstOrDefaultAsync(r => r.Id == reservationId);

            if (reservation == null)
            {
                _logger.LogWarning($"Inscription {reservationId} non trouvée");
                return (false, "Inscription non trouvée");
            }

            if (isAdmin)
            {
                // Admin supprime complètement l'inscription
                _context.Reservations.Remove(reservation);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Inscription {reservationId} supprimée complètement par admin");
                return (true, "Inscription supprimée avec succès");
            }
            else
            {
                // Utilisateur normal : se retire des participants
                return await RemoveParticipantAsync(reservationId, userId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Erreur lors de la suppression de l'inscription {reservationId}");
            return (false, $"Erreur : {ex.Message}");
        }
    }

    // ================================================================
    // MÉTHODES PRIVÉES (HELPERS)
    // ================================================================

    /// <summary>
    /// Vérifie si des alvéoles ont des chevauchements temporels
    /// </summary>
    private async Task<bool> HasOverlapAsync(
        List<int> alveoleIds,
        DateTime dateDebut,
        DateTime dateFin,
        int? excludeReservationId = null)
    {
        var query = _context.Reservations
            .Include(r => r.ReservationAlveoles)
            .Where(r => r.ReservationAlveoles.Any(ra => alveoleIds.Contains(ra.AlveoleId)))
            .Where(r => r.DateDebut < dateFin && r.DateFin > dateDebut);

        if (excludeReservationId.HasValue)
        {
            query = query.Where(r => r.Id != excludeReservationId.Value);
        }

        return await query.AnyAsync();
    }

    /// <summary>
    /// Récupère les noms des alvéoles fermées pendant une période
    /// </summary>
    private async Task<List<string>> GetFermeturesAsync(
        List<int> alveoleIds,
        DateTime dateDebut,
        DateTime dateFin)
    {
        var fermetures = await _context.FermetureAlveoles
            .Include(f => f.Alveole)
            .Where(f => alveoleIds.Contains(f.AlveoleId))
            .Where(f => f.DateDebut < dateFin && f.DateFin > dateDebut)
            .ToListAsync();

        return fermetures.Select(f => f.Alveole.Nom).Distinct().ToList();
    }

    /// <summary>
    /// Recalcule le statut d'une inscription selon la présence de moniteurs
    /// </summary>
    private async Task RecalculateStatutAsync(Reservation reservation)
    {
        if (!reservation.Participants.Any())
        {
            await _context.Entry(reservation)
                .Collection(r => r.Participants)
                .LoadAsync();
        }

        var hasMoniteur = reservation.Participants.Any(p => p.EstMoniteur);
        reservation.StatutReservation = hasMoniteur
            ? StatutReservation.Confirmee
            : StatutReservation.EnAttente;
    }

    /// <summary>
    /// Convertit une entité Reservation en ReservationDto
    /// </summary>
    private ReservationDto MapToReservationDto(Reservation reservation)
    {
        var alveoles = reservation.ReservationAlveoles
            .Select(ra => new AlveoleDto
            {
                Id = ra.Alveole.Id,
                Nom = ra.Alveole.Nom,
                Ordre = ra.Alveole.Ordre,
                EstActive = ra.Alveole.EstActive
            })
            .OrderBy(a => a.Ordre)
            .ToList();

        var participants = reservation.Participants
            .Select(rp => new ReservationParticipantDto
            {
                UserId = rp.UserId.ToString(),
                NomComplet = rp.User.NomComplet,
                Email = rp.User.Email ?? "",
                Initiales = rp.User.Initiales,
                EstMoniteur = rp.EstMoniteur,
                DateInscription = rp.DateInscription
            })
            .OrderBy(p => p.DateInscription)
            .ToList();

        return new ReservationDto
        {
            Id = reservation.Id,
            DateDebut = reservation.DateDebut,
            DateFin = reservation.DateFin,
            StatutReservation = reservation.StatutReservation,
            Commentaire = reservation.Commentaire,
            CreatedByUserId = reservation.CreatedByUserId.ToString(),
            CreatedByNom = reservation.CreatedBy?.NomComplet ?? "",
            DateCreation = reservation.DateCreation,
            Alveoles = alveoles,
            Participants = participants
        };
    }
}

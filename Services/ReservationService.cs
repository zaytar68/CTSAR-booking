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
    private readonly INotificationService _notificationService;

    public ReservationService(
        ApplicationDbContext context,
        ILogger<ReservationService> logger,
        INotificationService notificationService)
    {
        _context = context;
        _logger = logger;
        _notificationService = notificationService;
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

            // 1. Valider les alvéoles (si spécifiées)
            // Les membres peuvent créer une session sans alvéoles (le moniteur les choisira)
            // Les moniteurs doivent obligatoirement sélectionner des alvéoles
            if (isMoniteur && !dto.AlveoleIds.Any())
            {
                _logger.LogWarning("Un moniteur doit sélectionner au moins une alvéole");
                return (false, "Vous devez sélectionner au moins une alvéole", null);
            }

            if (dto.AlveoleIds.Any())
            {
                var alveoles = await _context.Alveoles
                    .Where(a => dto.AlveoleIds.Contains(a.Id) && a.EstActive)
                    .ToListAsync();

                if (alveoles.Count != dto.AlveoleIds.Count)
                {
                    _logger.LogWarning("Une ou plusieurs alvéoles invalides ou inactives");
                    return (false, "Une ou plusieurs alvéoles sélectionnées sont invalides ou inactives", null);
                }
            }

            // 2. Valider les dates
            if (dto.DateFin <= dto.DateDebut)
            {
                _logger.LogWarning("Date de fin doit être après date de début");
                return (false, "La date de fin doit être après la date de début", null);
            }

            // 3. Vérifier si le club est fermé
            if (await IsClubFermeAsync(dto.DateDebut, dto.DateFin))
            {
                _logger.LogWarning("Le club est fermé pendant cette période");
                return (false, "Le club de tir est fermé pendant cette période", null);
            }

            // 4. Vérifier les chevauchements (seulement si des alvéoles sont spécifiées et que c'est un membre)
            // Les moniteurs peuvent créer des sessions concurrentes (plusieurs moniteurs dans le même créneau)
            // Les membres sans alvéoles ne peuvent pas créer de chevauchement
            if (!isMoniteur && dto.AlveoleIds.Any())
            {
                if (await HasOverlapAsync(dto.AlveoleIds, dto.DateDebut, dto.DateFin))
                {
                    _logger.LogWarning("Chevauchement détecté avec une inscription existante");
                    return (false, "Une ou plusieurs alvéoles sont déjà réservées sur ce créneau", null);
                }
            }

            // 5. Créer l'inscription
            if (!int.TryParse(userId, out int userIdInt))
            {
                _logger.LogError("userId invalide : {userId}", userId);
                return (false, "Identifiant utilisateur invalide", null);
            }

            // Récupérer l'utilisateur pour formater le commentaire
            var creator = await _context.Users.FindAsync(userIdInt);
            if (creator == null)
            {
                _logger.LogError("Utilisateur non trouvé : {userId}", userId);
                return (false, "Utilisateur non trouvé", null);
            }

            // Formater le commentaire avec date/heure et nom de l'utilisateur
            string? commentaireFormate = null;
            if (!string.IsNullOrWhiteSpace(dto.Commentaire))
            {
                var now = DateTime.Now;
                commentaireFormate = $"[{now:dd/MM HH:mm} - {creator.Prenom} {creator.Nom}] {dto.Commentaire}";
            }

            var reservation = new Reservation
            {
                DateDebut = dto.DateDebut,
                DateFin = dto.DateFin,
                Commentaire = commentaireFormate,
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

            // 7. Si c'est un moniteur, fusionner avec les sessions en attente sur le même créneau
            List<int> mergedUserIds = new List<int>();
            if (isMoniteur)
            {
                // Rechercher les réservations en attente qui chevauchent ce créneau
                var overlappingReservations = await _context.Reservations
                    .Include(r => r.Participants)
                    .Where(r => r.StatutReservation == StatutReservation.EnAttente
                           && r.DateDebut < dto.DateFin
                           && r.DateFin > dto.DateDebut)
                    .ToListAsync();

                if (overlappingReservations.Any())
                {
                    _logger.LogInformation($"Fusion de {overlappingReservations.Count} session(s) en attente");

                    foreach (var oldReservation in overlappingReservations)
                    {
                        // Transférer tous les participants (sauf ceux déjà inscrits)
                        foreach (var participant in oldReservation.Participants)
                        {
                            if (!mergedUserIds.Contains(participant.UserId) && participant.UserId != userIdInt)
                            {
                                _context.ReservationParticipants.Add(new ReservationParticipant
                                {
                                    ReservationId = reservation.Id,
                                    UserId = participant.UserId,
                                    EstMoniteur = participant.EstMoniteur,
                                    DateInscription = participant.DateInscription
                                });
                                mergedUserIds.Add(participant.UserId);
                            }
                        }

                        // Supprimer l'ancienne réservation
                        _context.Reservations.Remove(oldReservation);
                    }

                    _logger.LogInformation($"{mergedUserIds.Count} participant(s) transféré(s) depuis les sessions en attente");
                }
            }

            // 8. Inscrire le créateur comme participant
            _context.ReservationParticipants.Add(new ReservationParticipant
            {
                ReservationId = reservation.Id,
                UserId = userIdInt,
                EstMoniteur = isMoniteur,
                DateInscription = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            // 9. Si un moniteur a créé la session et a fusionné des participants, les notifier
            _logger.LogInformation($"[NOTIF DEBUG CreateReservationAsync] isMoniteur={isMoniteur}, mergedUserIds.Count={mergedUserIds.Count}");
            if (isMoniteur && mergedUserIds.Any())
            {
                // Utiliser la variable creator déjà récupérée plus haut
                await _notificationService.NotifyMultipleAsync(
                    mergedUserIds.Select(id => id.ToString()).ToList(),
                    "Moniteur disponible",
                    $"{creator.Prenom} {creator.Nom} a validé votre séance en s'inscrivant comme moniteur",
                    NotificationType.Success);

                _logger.LogInformation($"Notifications envoyées à {mergedUserIds.Count} participant(s) pour la validation par moniteur");
            }

            // 10. Recharger avec toutes les relations pour le DTO
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

            // Notifier les autres participants si un moniteur s'inscrit
            _logger.LogInformation($"[NOTIF DEBUG AddParticipantAsync] isMoniteur={isMoniteur}, reservation.Participants.Count={reservation.Participants.Count}");
            if (isMoniteur)
            {
                var user = await _context.Users.FindAsync(userIdInt);
                var membresInscrits = reservation.Participants
                    .Where(p => !p.EstMoniteur && p.UserId != userIdInt)
                    .Select(p => p.UserId.ToString())
                    .ToList();

                _logger.LogInformation($"[NOTIF DEBUG AddParticipantAsync] membresInscrits.Count={membresInscrits.Count}, user={user?.NomComplet ?? "null"}");
                if (membresInscrits.Any() && user != null)
                {
                    await _notificationService.NotifyMultipleAsync(
                        membresInscrits,
                        "Moniteur disponible",
                        $"{user.Prenom} {user.Nom} s'est inscrit comme moniteur pour votre séance",
                        NotificationType.Success);
                }
            }

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

            // Retirer le participant de la base de données ET de la collection en mémoire
            _context.ReservationParticipants.Remove(participant);
            reservation.Participants.Remove(participant);

            // Si plus aucun participant, supprimer l'inscription complète
            if (reservation.Participants.Count == 0)
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

            // Notifier les autres participants si un moniteur se désinscrit
            _logger.LogInformation($"[NOTIF DEBUG RemoveParticipantAsync] wasMoniteur={wasMoniteur}, reservation.Participants.Count={reservation.Participants.Count}");
            if (wasMoniteur)
            {
                var user = await _context.Users.FindAsync(userIdInt);
                var participantIds = reservation.Participants
                    .Where(p => p.UserId != userIdInt)
                    .Select(p => p.UserId.ToString())
                    .ToList();

                _logger.LogInformation($"[NOTIF DEBUG RemoveParticipantAsync] participantIds.Count={participantIds.Count}, user={user?.NomComplet ?? "null"}");
                if (participantIds.Any() && user != null)
                {
                    await _notificationService.NotifyMultipleAsync(
                        participantIds,
                        "Moniteur absent",
                        $"Le moniteur {user.Prenom} {user.Nom} s'est désinscrit de la séance",
                        NotificationType.Warning);
                }
            }

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
    // GESTION DES ALVÉOLES PAR LE MONITEUR
    // ================================================================

    /// <summary>
    /// Le moniteur définit les alvéoles utilisées pour la session
    /// Remplace les alvéoles existantes et notifie tous les participants
    /// </summary>
    public async Task<(bool Success, string Message)> UpdateSessionAlveolesAsync(
        int reservationId,
        List<int> alveoleIds,
        string moniteurId)
    {
        try
        {
            // 1. Vérifier que l'utilisateur est moniteur inscrit
            var reservation = await _context.Reservations
                .Include(r => r.Participants)
                .ThenInclude(rp => rp.User)
                .FirstOrDefaultAsync(r => r.Id == reservationId);

            if (reservation == null)
            {
                return (false, "Réservation non trouvée");
            }

            var isMoniteurInscrit = reservation.Participants
                .Any(p => p.UserId.ToString() == moniteurId && p.EstMoniteur);

            if (!isMoniteurInscrit)
            {
                _logger.LogWarning(
                    "L'utilisateur {UserId} a tenté de définir les alvéoles sans être moniteur inscrit",
                    moniteurId);
                return (false, "Seul un moniteur inscrit peut définir les alvéoles");
            }

            // 2. Vérifier que les alvéoles existent et sont actives
            var alveoles = await _context.Alveoles
                .Where(a => alveoleIds.Contains(a.Id) && a.EstActive)
                .ToListAsync();

            if (alveoles.Count != alveoleIds.Count)
            {
                return (false, "Certaines alvéoles sont inactives ou n'existent pas");
            }

            // 3. Mettre à jour les alvéoles (supprimer anciennes + ajouter nouvelles)
            var existingLinks = await _context.Set<ReservationAlveole>()
                .Where(ra => ra.ReservationId == reservationId)
                .ToListAsync();

            _context.RemoveRange(existingLinks);

            foreach (var alveoleId in alveoleIds)
            {
                _context.Add(new ReservationAlveole
                {
                    ReservationId = reservationId,
                    AlveoleId = alveoleId
                });
            }

            await _context.SaveChangesAsync();

            // 4. Notifier tous les participants (sauf le moniteur qui fait l'action)
            var participantIds = reservation.Participants
                .Where(p => p.UserId.ToString() != moniteurId)
                .Select(p => p.UserId.ToString())
                .ToList();

            if (participantIds.Any())
            {
                var alveoleNames = alveoles.Select(a => a.Nom).ToList();
                var alveolesText = string.Join(", ", alveoleNames);

                await _notificationService.NotifyMultipleAsync(
                    participantIds,
                    "Alvéoles définies",
                    $"Le moniteur a défini les alvéoles pour la séance : {alveolesText}",
                    NotificationType.Info);
            }

            _logger.LogInformation(
                "Alvéoles mises à jour pour la réservation {ReservationId} par le moniteur {MoniteurId}",
                reservationId,
                moniteurId);

            return (true, "Alvéoles mises à jour avec succès. Les participants ont été notifiés.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Erreur lors de la mise à jour des alvéoles pour la réservation {ReservationId}",
                reservationId);
            return (false, "Une erreur est survenue lors de la mise à jour des alvéoles");
        }
    }

    // ================================================================
    // GESTION DES COMMENTAIRES
    // ================================================================

    /// <summary>
    /// Ajoute une entrée au commentaire (moniteur ou membre)
    /// Format : [DD/MM HH:mm - Prénom NOM] Texte
    /// </summary>
    public async Task<(bool Success, string Message)> AddCommentaireEntryAsync(
        int reservationId,
        string userId,
        string content)
    {
        try
        {
            var reservation = await _context.Reservations
                .Include(r => r.Participants)
                .ThenInclude(rp => rp.User)
                .FirstOrDefaultAsync(r => r.Id == reservationId);

            if (reservation == null)
            {
                return (false, "Réservation non trouvée");
            }

            // Vérifier que l'utilisateur est inscrit
            var participant = reservation.Participants
                .FirstOrDefault(p => p.UserId.ToString() == userId);

            if (participant == null)
            {
                _logger.LogWarning(
                    "L'utilisateur {UserId} a tenté d'ajouter un commentaire sans être inscrit",
                    userId);
                return (false, "Vous devez être inscrit pour ajouter un commentaire");
            }

            // Récupérer les infos utilisateur
            var user = participant.User;
            var userName = $"{user.Prenom} {user.Nom}";
            var timestamp = DateTime.Now.ToString("dd/MM HH:mm");

            // Formater la nouvelle entrée
            var newEntry = $"[{timestamp} - {userName}] {content}";

            // Ajouter au commentaire existant
            if (string.IsNullOrWhiteSpace(reservation.Commentaire))
            {
                reservation.Commentaire = newEntry;
            }
            else
            {
                reservation.Commentaire += $"\n\n{newEntry}";
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Commentaire ajouté à la réservation {ReservationId} par {UserId} ({UserName})",
                reservationId,
                userId,
                userName);

            return (true, "Commentaire ajouté avec succès");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Erreur lors de l'ajout du commentaire pour la réservation {ReservationId}",
                reservationId);
            return (false, "Une erreur est survenue lors de l'ajout du commentaire");
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
    /// Vérifie si le club est fermé pendant une période donnée
    /// </summary>
    private async Task<bool> IsClubFermeAsync(
        DateTime dateDebut,
        DateTime dateFin)
    {
        return await _context.FermeturesClub
            .AnyAsync(f => f.DateDebut < dateFin && f.DateFin > dateDebut);
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

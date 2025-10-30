// ====================================================================
// FermetureClubService.cs : Service de gestion des fermetures du club
// ====================================================================

using Microsoft.EntityFrameworkCore;
using CTSAR.Booking.Data;
using CTSAR.Booking.DTOs;

namespace CTSAR.Booking.Services;

/// <summary>
/// Service pour gérer les fermetures planifiées du club de tir
/// (travaux, jours fériés, réservations extérieures, etc.)
/// </summary>
public class FermetureClubService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<FermetureClubService> _logger;
    private readonly INotificationService _notificationService;

    public FermetureClubService(
        ApplicationDbContext context,
        ILogger<FermetureClubService> logger,
        INotificationService notificationService)
    {
        _context = context;
        _logger = logger;
        _notificationService = notificationService;
    }

    /// <summary>
    /// Récupère toutes les fermetures planifiées dans une période donnée
    /// </summary>
    /// <param name="debut">Date de début de la période</param>
    /// <param name="fin">Date de fin de la période</param>
    /// <returns>Liste des fermetures dans la période</returns>
    public async Task<List<FermetureClubDto>> GetFermeturesForPeriodAsync(DateTime debut, DateTime fin)
    {
        try
        {
            _logger.LogInformation($"Récupération des fermetures entre {debut:yyyy-MM-dd} et {fin:yyyy-MM-dd}");

            var fermetures = await _context.FermeturesClub
                .Where(f => f.DateDebut < fin && f.DateFin > debut)
                .OrderBy(f => f.DateDebut)
                .AsNoTracking()
                .ToListAsync();

            var dtos = fermetures.Select(f => new FermetureClubDto
            {
                Id = f.Id,
                DateDebut = f.DateDebut,
                DateFin = f.DateFin,
                TypeFermeture = f.TypeFermeture,
                Raison = f.Raison ?? string.Empty
            }).ToList();

            _logger.LogInformation($"{dtos.Count} fermetures récupérées");
            return dtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération des fermetures");
            throw;
        }
    }

    /// <summary>
    /// Récupère toutes les fermetures pour un mois donné
    /// </summary>
    /// <param name="year">Année</param>
    /// <param name="month">Mois (1-12)</param>
    /// <returns>Liste des fermetures du mois</returns>
    public async Task<List<FermetureClubDto>> GetFermeturesForMonthAsync(int year, int month)
    {
        var debut = new DateTime(year, month, 1);
        var fin = debut.AddMonths(1);
        return await GetFermeturesForPeriodAsync(debut, fin);
    }

    /// <summary>
    /// Récupère une fermeture par son ID
    /// </summary>
    public async Task<FermetureClubDto?> GetFermetureByIdAsync(int id)
    {
        try
        {
            var fermeture = await _context.FermeturesClub
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.Id == id);

            if (fermeture == null)
                return null;

            return new FermetureClubDto
            {
                Id = fermeture.Id,
                DateDebut = fermeture.DateDebut,
                DateFin = fermeture.DateFin,
                TypeFermeture = fermeture.TypeFermeture,
                Raison = fermeture.Raison ?? string.Empty
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Erreur lors de la récupération de la fermeture {id}");
            throw;
        }
    }

    /// <summary>
    /// Crée une nouvelle fermeture planifiée du club
    /// </summary>
    /// <param name="shouldNotifyUsers">Indique si les utilisateurs impactés doivent être notifiés</param>
    public async Task<(bool Success, string Message, FermetureClubDto? Fermeture)> CreateFermetureAsync(
        DateTime dateDebut,
        DateTime dateFin,
        TypeFermeture typeFermeture,
        string? raison,
        bool shouldNotifyUsers = false)
    {
        try
        {
            _logger.LogInformation("Création d'une fermeture du club");

            // Valider les dates
            if (dateFin <= dateDebut)
            {
                _logger.LogWarning("Date de fin doit être après date de début");
                return (false, "La date de fin doit être après la date de début", null);
            }

            // Vérifier les chevauchements avec d'autres fermetures
            var hasOverlap = await _context.FermeturesClub
                .AnyAsync(f => f.DateDebut < dateFin && f.DateFin > dateDebut);

            if (hasOverlap)
            {
                _logger.LogWarning("Fermeture chevauche une fermeture existante");
                return (false, "Cette période chevauche une fermeture existante", null);
            }

            // Récupérer les utilisateurs impactés AVANT de créer la fermeture
            List<AffectedUserInfo> affectedUsers = new();
            if (shouldNotifyUsers)
            {
                affectedUsers = await _notificationService.GetUsersAffectedByClubClosureAsync(dateDebut, dateFin);
            }

            // Créer la fermeture
            var fermeture = new FermetureClub
            {
                DateDebut = dateDebut,
                DateFin = dateFin,
                TypeFermeture = typeFermeture,
                Raison = raison,
                DateCreation = DateTime.UtcNow
            };

            _context.FermeturesClub.Add(fermeture);
            await _context.SaveChangesAsync();

            // Notifier les utilisateurs impactés
            if (shouldNotifyUsers && affectedUsers.Any())
            {
                var userIds = affectedUsers.Select(u => u.UserId).Distinct().ToList();
                var typeLabel = typeFermeture switch
                {
                    TypeFermeture.Travaux => "travaux",
                    TypeFermeture.JourFerie => "jour férié",
                    TypeFermeture.ReservationExterne => "réservation externe",
                    _ => "fermeture"
                };

                var message = string.IsNullOrWhiteSpace(raison)
                    ? $"Le club sera fermé du {dateDebut:dd/MM/yyyy HH:mm} au {dateFin:dd/MM/yyyy HH:mm} ({typeLabel}). Vos réservations pendant cette période sont annulées."
                    : $"Le club sera fermé du {dateDebut:dd/MM/yyyy HH:mm} au {dateFin:dd/MM/yyyy HH:mm} ({typeLabel} : {raison}). Vos réservations pendant cette période sont annulées.";

                await _notificationService.NotifyMultipleAsync(
                    userIds,
                    "Club fermé",
                    message,
                    NotificationType.Warning);

                _logger.LogInformation(
                    "Notifications envoyées à {Count} utilisateur(s) pour la fermeture du club",
                    userIds.Count);
            }

            var dto = new FermetureClubDto
            {
                Id = fermeture.Id,
                DateDebut = fermeture.DateDebut,
                DateFin = fermeture.DateFin,
                TypeFermeture = fermeture.TypeFermeture,
                Raison = fermeture.Raison ?? string.Empty
            };

            _logger.LogInformation($"Fermeture du club créée avec succès (ID: {fermeture.Id})");
            return (true, "Fermeture créée avec succès", dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la création de la fermeture");
            return (false, $"Erreur : {ex.Message}", null);
        }
    }

    /// <summary>
    /// Met à jour une fermeture existante
    /// </summary>
    public async Task<(bool Success, string Message)> UpdateFermetureAsync(
        int id,
        DateTime dateDebut,
        DateTime dateFin,
        TypeFermeture typeFermeture,
        string? raison)
    {
        try
        {
            _logger.LogInformation($"Mise à jour de la fermeture {id}");

            var fermeture = await _context.FermeturesClub.FindAsync(id);
            if (fermeture == null)
            {
                _logger.LogWarning($"Fermeture {id} non trouvée");
                return (false, "Fermeture non trouvée");
            }

            // Valider les dates
            if (dateFin <= dateDebut)
            {
                _logger.LogWarning("Date de fin doit être après date de début");
                return (false, "La date de fin doit être après la date de début");
            }

            // Vérifier les chevauchements avec d'autres fermetures (en excluant celle-ci)
            var hasOverlap = await _context.FermeturesClub
                .AnyAsync(f => f.Id != id
                    && f.DateDebut < dateFin
                    && f.DateFin > dateDebut);

            if (hasOverlap)
            {
                _logger.LogWarning("Fermeture chevauche une fermeture existante");
                return (false, "Cette période chevauche une fermeture existante");
            }

            // Mettre à jour
            fermeture.DateDebut = dateDebut;
            fermeture.DateFin = dateFin;
            fermeture.TypeFermeture = typeFermeture;
            fermeture.Raison = raison;

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Fermeture {id} mise à jour avec succès");
            return (true, "Fermeture mise à jour avec succès");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Erreur lors de la mise à jour de la fermeture {id}");
            return (false, $"Erreur : {ex.Message}");
        }
    }

    /// <summary>
    /// Supprime une fermeture planifiée
    /// </summary>
    public async Task<(bool Success, string Message)> DeleteFermetureAsync(int id)
    {
        try
        {
            _logger.LogInformation($"Suppression de la fermeture {id}");

            var fermeture = await _context.FermeturesClub.FindAsync(id);
            if (fermeture == null)
            {
                _logger.LogWarning($"Fermeture {id} non trouvée");
                return (false, "Fermeture non trouvée");
            }

            _context.FermeturesClub.Remove(fermeture);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Fermeture {id} supprimée avec succès");
            return (true, "Fermeture supprimée avec succès");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Erreur lors de la suppression de la fermeture {id}");
            return (false, $"Erreur : {ex.Message}");
        }
    }

    /// <summary>
    /// Vérifie si le club est fermé pendant une période donnée
    /// </summary>
    public async Task<bool> IsClubFermePendantPeriodeAsync(
        DateTime dateDebut,
        DateTime dateFin)
    {
        try
        {
            return await _context.FermeturesClub
                .AnyAsync(f => f.DateDebut < dateFin && f.DateFin > dateDebut);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la vérification de fermeture du club");
            throw;
        }
    }
}

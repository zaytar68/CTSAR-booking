// ====================================================================
// FermetureAlveoleService.cs : Service de gestion des fermetures d'alvéoles
// ====================================================================

using Microsoft.EntityFrameworkCore;
using CTSAR.Booking.Data;
using CTSAR.Booking.DTOs;

namespace CTSAR.Booking.Services;

/// <summary>
/// Service pour gérer les fermetures planifiées des alvéoles
/// (travaux, jours fériés, réservations extérieures, etc.)
/// </summary>
public class FermetureAlveoleService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<FermetureAlveoleService> _logger;

    public FermetureAlveoleService(ApplicationDbContext context, ILogger<FermetureAlveoleService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Récupère toutes les fermetures planifiées dans une période donnée
    /// </summary>
    /// <param name="debut">Date de début de la période</param>
    /// <param name="fin">Date de fin de la période</param>
    /// <returns>Liste des fermetures dans la période</returns>
    public async Task<List<FermetureDto>> GetFermeturesForPeriodAsync(DateTime debut, DateTime fin)
    {
        try
        {
            _logger.LogInformation($"Récupération des fermetures entre {debut:yyyy-MM-dd} et {fin:yyyy-MM-dd}");

            var fermetures = await _context.FermetureAlveoles
                .Include(f => f.Alveole)
                .Where(f => f.DateDebut < fin && f.DateFin > debut)
                .OrderBy(f => f.DateDebut)
                .ThenBy(f => f.Alveole.Ordre)
                .AsNoTracking()
                .ToListAsync();

            var dtos = fermetures.Select(f => new FermetureDto
            {
                Id = f.Id,
                AlveoleId = f.AlveoleId,
                AlveoleNom = f.Alveole.Nom,
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
    public async Task<List<FermetureDto>> GetFermeturesForMonthAsync(int year, int month)
    {
        var debut = new DateTime(year, month, 1);
        var fin = debut.AddMonths(1);
        return await GetFermeturesForPeriodAsync(debut, fin);
    }

    /// <summary>
    /// Récupère une fermeture par son ID
    /// </summary>
    public async Task<FermetureDto?> GetFermetureByIdAsync(int id)
    {
        try
        {
            var fermeture = await _context.FermetureAlveoles
                .Include(f => f.Alveole)
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.Id == id);

            if (fermeture == null)
                return null;

            return new FermetureDto
            {
                Id = fermeture.Id,
                AlveoleId = fermeture.AlveoleId,
                AlveoleNom = fermeture.Alveole.Nom,
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
    /// Récupère toutes les fermetures pour une alvéole donnée
    /// </summary>
    public async Task<List<FermetureDto>> GetFermeturesByAlveoleAsync(int alveoleId)
    {
        try
        {
            _logger.LogInformation($"Récupération des fermetures pour l'alvéole {alveoleId}");

            var fermetures = await _context.FermetureAlveoles
                .Include(f => f.Alveole)
                .Where(f => f.AlveoleId == alveoleId)
                .OrderBy(f => f.DateDebut)
                .AsNoTracking()
                .ToListAsync();

            return fermetures.Select(f => new FermetureDto
            {
                Id = f.Id,
                AlveoleId = f.AlveoleId,
                AlveoleNom = f.Alveole.Nom,
                DateDebut = f.DateDebut,
                DateFin = f.DateFin,
                TypeFermeture = f.TypeFermeture,
                Raison = f.Raison ?? string.Empty
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Erreur lors de la récupération des fermetures pour l'alvéole {alveoleId}");
            throw;
        }
    }

    /// <summary>
    /// Crée une nouvelle fermeture planifiée
    /// </summary>
    public async Task<(bool Success, string Message, FermetureDto? Fermeture)> CreateFermetureAsync(
        int alveoleId,
        DateTime dateDebut,
        DateTime dateFin,
        TypeFermeture typeFermeture,
        string? raison)
    {
        try
        {
            _logger.LogInformation($"Création d'une fermeture pour l'alvéole {alveoleId}");

            // Vérifier que l'alvéole existe et est active
            var alveole = await _context.Alveoles.FindAsync(alveoleId);
            if (alveole == null || !alveole.EstActive)
            {
                _logger.LogWarning($"Alvéole {alveoleId} non trouvée ou inactive");
                return (false, "Alvéole non trouvée ou inactive", null);
            }

            // Valider les dates
            if (dateFin <= dateDebut)
            {
                _logger.LogWarning("Date de fin doit être après date de début");
                return (false, "La date de fin doit être après la date de début", null);
            }

            // Vérifier les chevauchements avec d'autres fermetures
            var hasOverlap = await _context.FermetureAlveoles
                .AnyAsync(f => f.AlveoleId == alveoleId
                    && f.DateDebut < dateFin
                    && f.DateFin > dateDebut);

            if (hasOverlap)
            {
                _logger.LogWarning($"Fermeture chevauche une fermeture existante pour l'alvéole {alveoleId}");
                return (false, "Cette période chevauche une fermeture existante pour cette alvéole", null);
            }

            // Créer la fermeture
            var fermeture = new FermetureAlveole
            {
                AlveoleId = alveoleId,
                DateDebut = dateDebut,
                DateFin = dateFin,
                TypeFermeture = typeFermeture,
                Raison = raison,
                DateCreation = DateTime.UtcNow
            };

            _context.FermetureAlveoles.Add(fermeture);
            await _context.SaveChangesAsync();

            // Recharger avec l'alvéole pour le DTO
            await _context.Entry(fermeture)
                .Reference(f => f.Alveole)
                .LoadAsync();

            var dto = new FermetureDto
            {
                Id = fermeture.Id,
                AlveoleId = fermeture.AlveoleId,
                AlveoleNom = fermeture.Alveole.Nom,
                DateDebut = fermeture.DateDebut,
                DateFin = fermeture.DateFin,
                TypeFermeture = fermeture.TypeFermeture,
                Raison = fermeture.Raison ?? string.Empty
            };

            _logger.LogInformation($"Fermeture créée avec succès (ID: {fermeture.Id})");
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

            var fermeture = await _context.FermetureAlveoles.FindAsync(id);
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
            var hasOverlap = await _context.FermetureAlveoles
                .AnyAsync(f => f.AlveoleId == fermeture.AlveoleId
                    && f.Id != id
                    && f.DateDebut < dateFin
                    && f.DateFin > dateDebut);

            if (hasOverlap)
            {
                _logger.LogWarning($"Fermeture chevauche une fermeture existante pour l'alvéole {fermeture.AlveoleId}");
                return (false, "Cette période chevauche une fermeture existante pour cette alvéole");
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

            var fermeture = await _context.FermetureAlveoles.FindAsync(id);
            if (fermeture == null)
            {
                _logger.LogWarning($"Fermeture {id} non trouvée");
                return (false, "Fermeture non trouvée");
            }

            _context.FermetureAlveoles.Remove(fermeture);
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
    /// Vérifie si une alvéole est fermée pendant une période donnée
    /// </summary>
    public async Task<bool> IsAlveoleFermeePendantPeriodeAsync(
        int alveoleId,
        DateTime dateDebut,
        DateTime dateFin)
    {
        try
        {
            return await _context.FermetureAlveoles
                .AnyAsync(f => f.AlveoleId == alveoleId
                    && f.DateDebut < dateFin
                    && f.DateFin > dateDebut);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Erreur lors de la vérification de fermeture pour l'alvéole {alveoleId}");
            throw;
        }
    }

    /// <summary>
    /// Récupère les alvéoles fermées pendant une période donnée
    /// </summary>
    /// <returns>Liste des IDs d'alvéoles fermées</returns>
    public async Task<List<int>> GetAlveolesFermeesAsync(DateTime dateDebut, DateTime dateFin)
    {
        try
        {
            var fermetures = await _context.FermetureAlveoles
                .Where(f => f.DateDebut < dateFin && f.DateFin > dateDebut)
                .Select(f => f.AlveoleId)
                .Distinct()
                .ToListAsync();

            return fermetures;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération des alvéoles fermées");
            throw;
        }
    }
}

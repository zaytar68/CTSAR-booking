// ====================================================================
// AlveoleService.cs : Service de gestion des alvéoles
// ====================================================================

using Microsoft.EntityFrameworkCore;
using CTSAR.Booking.Data;
using CTSAR.Booking.DTOs;

namespace CTSAR.Booking.Services;

/// <summary>
/// Service pour gérer les alvéoles (postes de tir).
/// Maximum 20 alvéoles dans le système.
/// </summary>
public class AlveoleService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AlveoleService> _logger;

    public AlveoleService(ApplicationDbContext context, ILogger<AlveoleService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Récupère toutes les alvéoles actives, triées par ordre d'affichage
    /// </summary>
    public async Task<List<AlveoleDto>> GetAllAlveolesAsync()
    {
        try
        {
            _logger.LogInformation("Récupération de toutes les alvéoles actives");

            var alveoles = await _context.Alveoles
                .Where(a => a.EstActive)
                .OrderBy(a => a.Ordre)
                .AsNoTracking()
                .ToListAsync();

            var dtos = alveoles.Select(a => new AlveoleDto
            {
                Id = a.Id,
                Nom = a.Nom,
                Ordre = a.Ordre,
                EstActive = a.EstActive
            }).ToList();

            _logger.LogInformation($"{dtos.Count} alvéoles récupérées");
            return dtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération des alvéoles");
            throw;
        }
    }

    /// <summary>
    /// Récupère une alvéole par son ID
    /// </summary>
    public async Task<AlveoleDto?> GetAlveoleByIdAsync(int id)
    {
        try
        {
            var alveole = await _context.Alveoles
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == id);

            if (alveole == null)
                return null;

            return new AlveoleDto
            {
                Id = alveole.Id,
                Nom = alveole.Nom,
                Ordre = alveole.Ordre,
                EstActive = alveole.EstActive
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Erreur lors de la récupération de l'alvéole {id}");
            throw;
        }
    }

    /// <summary>
    /// Crée une nouvelle alvéole
    /// </summary>
    public async Task<(bool Success, string Message, AlveoleDto? Alveole)> CreateAlveoleAsync(string nom)
    {
        try
        {
            _logger.LogInformation($"Création de l'alvéole '{nom}'");

            // Vérifier que le nom n'existe pas déjà
            var existe = await _context.Alveoles.AnyAsync(a => a.Nom == nom);
            if (existe)
            {
                _logger.LogWarning($"Une alvéole avec le nom '{nom}' existe déjà");
                return (false, "Une alvéole avec ce nom existe déjà", null);
            }

            // Récupérer le prochain ordre d'affichage
            var maxOrdre = await _context.Alveoles.MaxAsync(a => (int?)a.Ordre) ?? 0;

            var alveole = new Alveole
            {
                Nom = nom,
                Ordre = maxOrdre + 1,
                EstActive = true,
                DateCreation = DateTime.UtcNow
            };

            _context.Alveoles.Add(alveole);
            await _context.SaveChangesAsync();

            var dto = new AlveoleDto
            {
                Id = alveole.Id,
                Nom = alveole.Nom,
                Ordre = alveole.Ordre,
                EstActive = alveole.EstActive
            };

            _logger.LogInformation($"Alvéole '{nom}' créée avec succès (ID: {alveole.Id})");
            return (true, "Alvéole créée avec succès", dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Erreur lors de la création de l'alvéole '{nom}'");
            return (false, $"Erreur : {ex.Message}", null);
        }
    }

    /// <summary>
    /// Met à jour une alvéole existante
    /// </summary>
    public async Task<(bool Success, string Message)> UpdateAlveoleAsync(int id, string nom)
    {
        try
        {
            _logger.LogInformation($"Mise à jour de l'alvéole {id}");

            var alveole = await _context.Alveoles.FindAsync(id);
            if (alveole == null)
            {
                _logger.LogWarning($"Alvéole {id} non trouvée");
                return (false, "Alvéole non trouvée");
            }

            // Vérifier que le nouveau nom n'existe pas déjà (sauf pour cette alvéole)
            var existe = await _context.Alveoles
                .AnyAsync(a => a.Nom == nom && a.Id != id);

            if (existe)
            {
                _logger.LogWarning($"Une autre alvéole avec le nom '{nom}' existe déjà");
                return (false, "Une autre alvéole avec ce nom existe déjà");
            }

            alveole.Nom = nom;
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Alvéole {id} mise à jour avec succès");
            return (true, "Alvéole mise à jour avec succès");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Erreur lors de la mise à jour de l'alvéole {id}");
            return (false, $"Erreur : {ex.Message}");
        }
    }

    /// <summary>
    /// Désactive une alvéole (soft delete)
    /// </summary>
    public async Task<(bool Success, string Message)> DeleteAlveoleAsync(int id)
    {
        try
        {
            _logger.LogInformation($"Désactivation de l'alvéole {id}");

            var alveole = await _context.Alveoles.FindAsync(id);
            if (alveole == null)
            {
                _logger.LogWarning($"Alvéole {id} non trouvée");
                return (false, "Alvéole non trouvée");
            }

            // Soft delete : on marque comme inactive au lieu de supprimer
            alveole.EstActive = false;
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Alvéole {id} désactivée avec succès");
            return (true, "Alvéole désactivée avec succès");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Erreur lors de la désactivation de l'alvéole {id}");
            return (false, $"Erreur : {ex.Message}");
        }
    }

    /// <summary>
    /// Réorganise l'ordre d'affichage des alvéoles
    /// </summary>
    public async Task<(bool Success, string Message)> ReorderAlveolesAsync(List<int> orderedIds)
    {
        try
        {
            _logger.LogInformation("Réorganisation des alvéoles");

            var alveoles = await _context.Alveoles
                .Where(a => orderedIds.Contains(a.Id))
                .ToListAsync();

            for (int i = 0; i < orderedIds.Count; i++)
            {
                var alveole = alveoles.FirstOrDefault(a => a.Id == orderedIds[i]);
                if (alveole != null)
                {
                    alveole.Ordre = i + 1;
                }
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Alvéoles réorganisées avec succès");
            return (true, "Ordre mis à jour avec succès");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la réorganisation des alvéoles");
            return (false, $"Erreur : {ex.Message}");
        }
    }
}

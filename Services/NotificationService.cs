using CTSAR.Booking.Data;
using Microsoft.EntityFrameworkCore;

namespace CTSAR.Booking.Services;

/// <summary>
/// Implémentation du service de notifications
/// Pour l'instant : logs uniquement
/// Architecture extensible pour ajouter email/WhatsApp plus tard
/// </summary>
public class NotificationService : INotificationService
{
    private readonly ILogger<NotificationService> _logger;
    private readonly ApplicationDbContext _context;

    public NotificationService(
        ILogger<NotificationService> logger,
        ApplicationDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    /// <summary>
    /// Envoie une notification à un utilisateur
    /// </summary>
    public async Task NotifyAsync(string userId, string title, string message, NotificationType type)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                _logger.LogWarning("Tentative de notification vers un utilisateur inexistant : {UserId}", userId);
                return;
            }

            _logger.LogInformation(
                "[NOTIFICATION] {Type} | To: {UserEmail} ({UserName}) | Title: {Title} | Message: {Message}",
                type,
                user.Email,
                $"{user.Prenom} {user.Nom}",
                title,
                message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de l'envoi de la notification à {UserId}", userId);
        }
    }

    /// <summary>
    /// Envoie une notification à plusieurs utilisateurs
    /// </summary>
    public async Task NotifyMultipleAsync(List<string> userIds, string title, string message, NotificationType type)
    {
        foreach (var userId in userIds)
        {
            await NotifyAsync(userId, title, message, type);
        }
    }

    /// <summary>
    /// Récupère la liste des utilisateurs impactés par la fermeture d'une alvéole
    /// Retourne tous les participants (membres + moniteurs) des réservations futures sur cette alvéole
    /// </summary>
    public async Task<List<AffectedUserInfo>> GetUsersAffectedByAlveoleClosureAsync(int alveoleId)
    {
        try
        {
            var now = DateTime.UtcNow;

            // Récupérer toutes les réservations futures sur cette alvéole
            var reservations = await _context.Reservations
                .Include(r => r.ReservationAlveoles)
                .Include(r => r.Participants)
                    .ThenInclude(p => p.User)
                .Where(r => r.DateDebut > now
                       && r.ReservationAlveoles.Any(ra => ra.AlveoleId == alveoleId))
                .ToListAsync();

            var affectedUsers = new List<AffectedUserInfo>();

            foreach (var reservation in reservations)
            {
                foreach (var participant in reservation.Participants)
                {
                    affectedUsers.Add(new AffectedUserInfo
                    {
                        UserId = participant.UserId.ToString(),
                        NomComplet = participant.User.NomComplet,
                        Email = participant.User.Email ?? string.Empty,
                        IsMoniteur = participant.EstMoniteur,
                        ReservationId = reservation.Id,
                        ReservationDateDebut = reservation.DateDebut,
                        ReservationDateFin = reservation.DateFin
                    });
                }
            }

            _logger.LogInformation(
                "Alvéole {AlveoleId} : {Count} utilisateur(s) impacté(s) dans {ReservationCount} réservation(s)",
                alveoleId,
                affectedUsers.Count,
                reservations.Count);

            return affectedUsers;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération des utilisateurs impactés par la fermeture de l'alvéole {AlveoleId}", alveoleId);
            return new List<AffectedUserInfo>();
        }
    }

    /// <summary>
    /// Récupère la liste des utilisateurs impactés par une fermeture du club
    /// Retourne tous les participants des réservations dans la période de fermeture
    /// </summary>
    public async Task<List<AffectedUserInfo>> GetUsersAffectedByClubClosureAsync(DateTime dateDebut, DateTime dateFin)
    {
        try
        {
            // Récupérer toutes les réservations chevauchant la période de fermeture
            var reservations = await _context.Reservations
                .Include(r => r.Participants)
                    .ThenInclude(p => p.User)
                .Where(r => r.DateDebut < dateFin && r.DateFin > dateDebut)
                .ToListAsync();

            var affectedUsers = new List<AffectedUserInfo>();

            foreach (var reservation in reservations)
            {
                foreach (var participant in reservation.Participants)
                {
                    affectedUsers.Add(new AffectedUserInfo
                    {
                        UserId = participant.UserId.ToString(),
                        NomComplet = participant.User.NomComplet,
                        Email = participant.User.Email ?? string.Empty,
                        IsMoniteur = participant.EstMoniteur,
                        ReservationId = reservation.Id,
                        ReservationDateDebut = reservation.DateDebut,
                        ReservationDateFin = reservation.DateFin
                    });
                }
            }

            _logger.LogInformation(
                "Fermeture club du {DateDebut:yyyy-MM-dd} au {DateFin:yyyy-MM-dd} : {Count} utilisateur(s) impacté(s) dans {ReservationCount} réservation(s)",
                dateDebut,
                dateFin,
                affectedUsers.Count,
                reservations.Count);

            return affectedUsers;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération des utilisateurs impactés par la fermeture du club");
            return new List<AffectedUserInfo>();
        }
    }
}

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
}

namespace CTSAR.Booking.Services;

/// <summary>
/// Service de notifications pour informer les utilisateurs des événements
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Envoie une notification à un utilisateur
    /// </summary>
    Task NotifyAsync(string userId, string title, string message, NotificationType type);

    /// <summary>
    /// Envoie une notification à plusieurs utilisateurs
    /// </summary>
    Task NotifyMultipleAsync(List<string> userIds, string title, string message, NotificationType type);
}

/// <summary>
/// Types de notifications
/// </summary>
public enum NotificationType
{
    Info,
    Success,
    Warning,
    Error
}

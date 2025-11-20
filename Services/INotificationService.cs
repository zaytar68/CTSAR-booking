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

    /// <summary>
    /// Envoie une notification de confirmation de réservation à un membre
    /// </summary>
    Task NotifyReservationConfirmeeAsync(string userId, DateTime dateReservation, TimeSpan heureDebut, TimeSpan heureFin, string nomAlveole, string nomMoniteur);

    /// <summary>
    /// Envoie une notification d'annulation de réservation à un membre
    /// </summary>
    Task NotifyReservationAnnuleeAsync(string userId, DateTime dateReservation, TimeSpan heureDebut, TimeSpan heureFin, string nomAlveole, string raisonAnnulation);

    /// <summary>
    /// Envoie une notification à plusieurs membres lorsqu'un moniteur valide sa présence
    /// </summary>
    Task NotifyMoniteurValideAsync(List<string> userIds, DateTime dateReservation, TimeSpan heureDebut, TimeSpan heureFin, string nomMoniteur);

    /// <summary>
    /// Envoie une notification à plusieurs membres lorsqu'un moniteur annule sa présence
    /// </summary>
    Task NotifyMoniteurAnnuleAsync(List<string> userIds, DateTime dateReservation, TimeSpan heureDebut, TimeSpan heureFin);

    /// <summary>
    /// Récupère la liste des utilisateurs impactés par la fermeture d'une alvéole
    /// </summary>
    Task<List<AffectedUserInfo>> GetUsersAffectedByAlveoleClosureAsync(int alveoleId);

    /// <summary>
    /// Récupère la liste des utilisateurs impactés par une fermeture du club
    /// </summary>
    Task<List<AffectedUserInfo>> GetUsersAffectedByClubClosureAsync(DateTime dateDebut, DateTime dateFin);
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

/// <summary>
/// Informations sur un utilisateur impacté par une action
/// </summary>
public class AffectedUserInfo
{
    public string UserId { get; set; } = string.Empty;
    public string NomComplet { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsMoniteur { get; set; }
    public int ReservationId { get; set; }
    public DateTime ReservationDateDebut { get; set; }
    public DateTime ReservationDateFin { get; set; }
}

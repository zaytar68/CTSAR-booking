namespace CTSAR.Booking.Services;

/// <summary>
/// Service de gestion des notifications push Web
/// </summary>
public interface IPushNotificationService
{
    /// <summary>
    /// Crée ou met à jour une souscription push pour un utilisateur
    /// </summary>
    /// <param name="userId">Identifiant de l'utilisateur</param>
    /// <param name="subscription">Données de souscription provenant du navigateur</param>
    /// <param name="userAgent">User-Agent du navigateur (optionnel)</param>
    /// <returns>True si la souscription a été créée/mise à jour</returns>
    Task<bool> SubscribeAsync(int userId, PushSubscriptionDto subscription, string? userAgent = null);

    /// <summary>
    /// Supprime une souscription push pour un utilisateur
    /// </summary>
    /// <param name="userId">Identifiant de l'utilisateur</param>
    /// <param name="endpoint">Endpoint de la souscription à supprimer</param>
    /// <returns>True si la souscription a été supprimée</returns>
    Task<bool> UnsubscribeAsync(int userId, string endpoint);

    /// <summary>
    /// Envoie une notification push à un utilisateur
    /// </summary>
    /// <param name="userId">Identifiant de l'utilisateur</param>
    /// <param name="notification">Données de la notification</param>
    /// <returns>True si au moins une notification a été envoyée avec succès</returns>
    Task<bool> SendAsync(int userId, PushNotificationDto notification);

    /// <summary>
    /// Envoie une notification push à plusieurs utilisateurs
    /// </summary>
    /// <param name="userIds">Liste des identifiants utilisateurs</param>
    /// <param name="notification">Données de la notification</param>
    /// <returns>Nombre d'utilisateurs ayant reçu la notification avec succès</returns>
    Task<int> SendToMultipleAsync(List<int> userIds, PushNotificationDto notification);

    /// <summary>
    /// Récupère toutes les souscriptions actives d'un utilisateur
    /// </summary>
    /// <param name="userId">Identifiant de l'utilisateur</param>
    /// <returns>Liste des souscriptions</returns>
    Task<List<Data.PushSubscription>> GetUserSubscriptionsAsync(int userId);

    /// <summary>
    /// Vérifie si un utilisateur a au moins une souscription active
    /// </summary>
    /// <param name="userId">Identifiant de l'utilisateur</param>
    /// <returns>True si au moins une souscription existe</returns>
    Task<bool> HasActiveSubscriptionAsync(int userId);
}

/// <summary>
/// DTO pour la souscription push provenant du client JavaScript
/// </summary>
public class PushSubscriptionDto
{
    /// <summary>
    /// URL endpoint fournie par le service push du navigateur
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// Clé publique P256DH encodée en Base64
    /// </summary>
    public string P256dh { get; set; } = string.Empty;

    /// <summary>
    /// Token d'authentification Auth encodé en Base64
    /// </summary>
    public string Auth { get; set; } = string.Empty;
}

/// <summary>
/// DTO pour l'envoi de notifications push
/// </summary>
public class PushNotificationDto
{
    /// <summary>
    /// Titre de la notification
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Corps de la notification
    /// </summary>
    public string Body { get; set; } = string.Empty;

    /// <summary>
    /// URL de destination au clic (optionnel)
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// URL de l'icône (optionnel)
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// Tag pour grouper les notifications (optionnel)
    /// </summary>
    public string? Tag { get; set; }

    /// <summary>
    /// Nécessite une interaction utilisateur pour fermer (optionnel)
    /// </summary>
    public bool RequireInteraction { get; set; } = false;
}

using System.ComponentModel.DataAnnotations;

namespace CTSAR.Booking.Data;

/// <summary>
/// Représente une souscription push notification pour un utilisateur sur un appareil spécifique.
/// Un utilisateur peut avoir plusieurs souscriptions (différents navigateurs/appareils).
/// </summary>
public class PushSubscription
{
    /// <summary>
    /// Identifiant unique de la souscription
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Identifiant de l'utilisateur propriétaire de cette souscription
    /// </summary>
    [Required]
    public int UserId { get; set; }

    /// <summary>
    /// URL endpoint fournie par le service push du navigateur
    /// Ex: https://fcm.googleapis.com/fcm/send/...
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// Clé publique P256DH (ECDH public key) encodée en Base64
    /// Utilisée pour chiffrer les données de notification
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string P256dh { get; set; } = string.Empty;

    /// <summary>
    /// Token d'authentification Auth encodé en Base64
    /// Utilisé pour authentifier les notifications
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Auth { get; set; } = string.Empty;

    /// <summary>
    /// User-Agent du navigateur/appareil (optionnel)
    /// Permet d'identifier l'appareil dans l'interface utilisateur
    /// </summary>
    [MaxLength(500)]
    public string? UserAgent { get; set; }

    /// <summary>
    /// Date de création de la souscription
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Date de dernière utilisation réussie (dernière notification envoyée)
    /// Null si aucune notification n'a encore été envoyée
    /// </summary>
    public DateTime? LastUsedAt { get; set; }

    // ====================================================================
    // NAVIGATION PROPERTIES
    // ====================================================================

    /// <summary>
    /// Utilisateur propriétaire de cette souscription
    /// </summary>
    public User User { get; set; } = null!;
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using CTSAR.Booking.Configuration;
using CTSAR.Booking.Services;
using System.Security.Claims;

namespace CTSAR.Booking.Controllers;

/// <summary>
/// Contrôleur API pour la gestion des notifications push Web
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize] // Toutes les méthodes nécessitent une authentification
public class PushNotificationsController : ControllerBase
{
    private readonly IPushNotificationService _pushService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PushNotificationsController> _logger;

    public PushNotificationsController(
        IPushNotificationService pushService,
        IConfiguration configuration,
        ILogger<PushNotificationsController> logger)
    {
        _pushService = pushService;
        _configuration = configuration;
        _logger = logger;
    }

    // ====================================================================
    // RÉCUPÉRATION DE LA CLÉ PUBLIQUE VAPID
    // ====================================================================

    /// <summary>
    /// Récupère la clé publique VAPID nécessaire pour s'abonner aux notifications
    /// </summary>
    /// <returns>Clé publique VAPID en Base64</returns>
    [HttpGet("vapid-public-key")]
    [AllowAnonymous] // Accessible sans authentification (nécessaire au chargement initial)
    public IActionResult GetVapidPublicKey()
    {
        var publicKey = _configuration["VapidSettings:PublicKey"];

        if (string.IsNullOrEmpty(publicKey))
        {
            _logger.LogError("[PUSH API] Clé publique VAPID non configurée");
            return Problem(
                title: "Configuration manquante",
                detail: "La clé publique VAPID n'est pas configurée sur le serveur",
                statusCode: 500
            );
        }

        return Ok(new { publicKey });
    }

    // ====================================================================
    // GESTION DES SOUSCRIPTIONS
    // ====================================================================

    /// <summary>
    /// Crée ou met à jour une souscription push pour l'utilisateur connecté
    /// </summary>
    /// <param name="subscription">Données de souscription provenant du navigateur</param>
    [HttpPost("subscribe")]
    public async Task<IActionResult> Subscribe([FromBody] PushSubscriptionDto subscription)
    {
        // Validation du modèle
        if (string.IsNullOrEmpty(subscription.Endpoint) ||
            string.IsNullOrEmpty(subscription.P256dh) ||
            string.IsNullOrEmpty(subscription.Auth))
        {
            return BadRequest(new
            {
                error = "Données de souscription invalides",
                details = "Endpoint, P256dh et Auth sont requis"
            });
        }

        // Récupérer l'ID de l'utilisateur connecté
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { error = "Utilisateur non authentifié" });
        }

        // Récupérer le User-Agent du navigateur
        var userAgent = Request.Headers["User-Agent"].ToString();

        // Créer la souscription
        var success = await _pushService.SubscribeAsync(userId.Value, subscription, userAgent);

        if (success)
        {
            _logger.LogInformation(
                "[PUSH API] Souscription créée pour userId: {UserId}",
                userId);

            return Ok(new
            {
                message = "Souscription aux notifications push créée avec succès"
            });
        }

        _logger.LogError(
            "[PUSH API] Échec de création de souscription pour userId: {UserId}",
            userId);

        return Problem(
            title: "Erreur lors de la souscription",
            detail: "Impossible de créer la souscription aux notifications push",
            statusCode: 500
        );
    }

    /// <summary>
    /// Supprime une souscription push pour l'utilisateur connecté
    /// </summary>
    /// <param name="request">Endpoint de la souscription à supprimer</param>
    [HttpPost("unsubscribe")]
    public async Task<IActionResult> Unsubscribe([FromBody] UnsubscribeRequest request)
    {
        if (string.IsNullOrEmpty(request.Endpoint))
        {
            return BadRequest(new { error = "Endpoint requis" });
        }

        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { error = "Utilisateur non authentifié" });
        }

        var success = await _pushService.UnsubscribeAsync(userId.Value, request.Endpoint);

        if (success)
        {
            _logger.LogInformation(
                "[PUSH API] Désinscription réussie pour userId: {UserId}",
                userId);

            return Ok(new { message = "Désinscription réussie" });
        }

        _logger.LogWarning(
            "[PUSH API] Souscription non trouvée pour userId: {UserId}",
            userId);

        return NotFound(new { error = "Souscription non trouvée" });
    }

    /// <summary>
    /// Récupère les souscriptions actives de l'utilisateur connecté
    /// </summary>
    [HttpGet("subscriptions")]
    public async Task<IActionResult> GetSubscriptions()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { error = "Utilisateur non authentifié" });
        }

        var subscriptions = await _pushService.GetUserSubscriptionsAsync(userId.Value);

        var result = subscriptions.Select(s => new
        {
            id = s.Id,
            endpoint = s.Endpoint.Substring(0, Math.Min(50, s.Endpoint.Length)) + "...",
            userAgent = s.UserAgent ?? "Inconnu",
            createdAt = s.CreatedAt,
            lastUsedAt = s.LastUsedAt
        });

        return Ok(result);
    }

    /// <summary>
    /// Vérifie si l'utilisateur a une souscription active
    /// </summary>
    [HttpGet("has-subscription")]
    public async Task<IActionResult> HasSubscription()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { error = "Utilisateur non authentifié" });
        }

        var hasSubscription = await _pushService.HasActiveSubscriptionAsync(userId.Value);

        return Ok(new { hasSubscription });
    }

    // ====================================================================
    // ENVOI DE NOTIFICATION DE TEST (Développement uniquement)
    // ====================================================================

    /// <summary>
    /// Envoie une notification de test à l'utilisateur connecté
    /// </summary>
    [HttpPost("test")]
    public async Task<IActionResult> SendTestNotification()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { error = "Utilisateur non authentifié" });
        }

        var notification = new PushNotificationDto
        {
            Title = "Test - CTSAR Booking",
            Body = "Ceci est une notification de test. Vos notifications fonctionnent correctement !",
            Url = "/",
            Tag = "test"
        };

        var success = await _pushService.SendAsync(userId.Value, notification);

        if (success)
        {
            return Ok(new { message = "Notification de test envoyée avec succès" });
        }

        return Problem(
            title: "Erreur lors de l'envoi",
            detail: "Impossible d'envoyer la notification de test",
            statusCode: 500
        );
    }

    // ====================================================================
    // MÉTHODES UTILITAIRES
    // ====================================================================

    /// <summary>
    /// Récupère l'ID de l'utilisateur connecté depuis les claims
    /// </summary>
    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdClaim))
        {
            return null;
        }

        if (int.TryParse(userIdClaim, out int userId))
        {
            return userId;
        }

        return null;
    }
}

/// <summary>
/// Requête de désinscription
/// </summary>
public class UnsubscribeRequest
{
    /// <summary>
    /// Endpoint de la souscription à supprimer
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;
}

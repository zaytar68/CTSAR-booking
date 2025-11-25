using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using CTSAR.Booking.Configuration;
using CTSAR.Booking.Data;
using WebPush;
using System.Text.Json;

namespace CTSAR.Booking.Services;

/// <summary>
/// Service de gestion des notifications push Web utilisant VAPID
/// </summary>
public class PushNotificationService : IPushNotificationService
{
    private readonly ApplicationDbContext _context;
    private readonly VapidSettings _vapidSettings;
    private readonly ILogger<PushNotificationService> _logger;
    private readonly WebPushClient _webPushClient;

    public PushNotificationService(
        ApplicationDbContext context,
        IOptions<VapidSettings> vapidSettings,
        ILogger<PushNotificationService> logger)
    {
        _context = context;
        _vapidSettings = vapidSettings.Value;
        _logger = logger;
        _webPushClient = new WebPushClient();
    }

    // ====================================================================
    // GESTION DES SOUSCRIPTIONS
    // ====================================================================

    public async Task<bool> SubscribeAsync(int userId, PushSubscriptionDto dto, string? userAgent = null)
    {
        try
        {
            // Vérifier si une souscription existe déjà avec cet endpoint
            var existing = await _context.PushSubscriptions
                .FirstOrDefaultAsync(ps => ps.UserId == userId && ps.Endpoint == dto.Endpoint);

            if (existing != null)
            {
                // Mettre à jour les clés (elles peuvent changer)
                existing.P256dh = dto.P256dh;
                existing.Auth = dto.Auth;
                existing.UserAgent = userAgent;
                existing.LastUsedAt = DateTime.UtcNow;

                _logger.LogInformation(
                    "[PUSH] Souscription mise à jour pour userId: {UserId}, endpoint: {Endpoint}",
                    userId, dto.Endpoint.Substring(0, Math.Min(50, dto.Endpoint.Length)) + "...");
            }
            else
            {
                // Créer une nouvelle souscription
                var subscription = new Data.PushSubscription
                {
                    UserId = userId,
                    Endpoint = dto.Endpoint,
                    P256dh = dto.P256dh,
                    Auth = dto.Auth,
                    UserAgent = userAgent,
                    CreatedAt = DateTime.UtcNow
                };

                _context.PushSubscriptions.Add(subscription);

                _logger.LogInformation(
                    "[PUSH] Nouvelle souscription créée pour userId: {UserId}",
                    userId);
            }

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[PUSH] Erreur lors de la création/mise à jour de souscription pour userId: {UserId}",
                userId);
            return false;
        }
    }

    public async Task<bool> UnsubscribeAsync(int userId, string endpoint)
    {
        try
        {
            var subscription = await _context.PushSubscriptions
                .FirstOrDefaultAsync(ps => ps.UserId == userId && ps.Endpoint == endpoint);

            if (subscription != null)
            {
                _context.PushSubscriptions.Remove(subscription);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "[PUSH] Souscription supprimée pour userId: {UserId}",
                    userId);
                return true;
            }

            _logger.LogWarning(
                "[PUSH] Aucune souscription trouvée pour userId: {UserId}, endpoint: {Endpoint}",
                userId, endpoint.Substring(0, Math.Min(50, endpoint.Length)));
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[PUSH] Erreur lors de la suppression de souscription pour userId: {UserId}",
                userId);
            return false;
        }
    }

    public async Task<List<Data.PushSubscription>> GetUserSubscriptionsAsync(int userId)
    {
        return await _context.PushSubscriptions
            .Where(ps => ps.UserId == userId)
            .OrderByDescending(ps => ps.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> HasActiveSubscriptionAsync(int userId)
    {
        return await _context.PushSubscriptions
            .AnyAsync(ps => ps.UserId == userId);
    }

    // ====================================================================
    // ENVOI DE NOTIFICATIONS
    // ====================================================================

    public async Task<bool> SendAsync(int userId, PushNotificationDto notification)
    {
        try
        {
            // Récupérer toutes les souscriptions de l'utilisateur
            var subscriptions = await _context.PushSubscriptions
                .Where(ps => ps.UserId == userId)
                .ToListAsync();

            if (!subscriptions.Any())
            {
                _logger.LogWarning(
                    "[PUSH] Aucune souscription push pour userId: {UserId}",
                    userId);
                return false;
            }

            // Préparer les détails VAPID
            var vapidDetails = new VapidDetails(
                _vapidSettings.Subject,
                _vapidSettings.PublicKey,
                _vapidSettings.PrivateKey
            );

            // Préparer le payload JSON
            var payload = JsonSerializer.Serialize(new
            {
                title = notification.Title,
                body = notification.Body,
                url = notification.Url ?? "/",
                icon = notification.Icon ?? "/icons/icon-192x192.png",
                badge = "/icons/icon-192x192.png",
                tag = notification.Tag ?? "default",
                requireInteraction = notification.RequireInteraction
            });

            bool atLeastOneSuccess = false;

            // Envoyer à toutes les souscriptions de l'utilisateur
            foreach (var subscription in subscriptions)
            {
                try
                {
                    var pushSubscription = new WebPush.PushSubscription(
                        subscription.Endpoint,
                        subscription.P256dh,
                        subscription.Auth
                    );

                    await _webPushClient.SendNotificationAsync(
                        pushSubscription,
                        payload,
                        vapidDetails
                    );

                    // Mettre à jour la date de dernière utilisation
                    subscription.LastUsedAt = DateTime.UtcNow;
                    atLeastOneSuccess = true;

                    _logger.LogInformation(
                        "[PUSH] Notification envoyée avec succès pour userId: {UserId}",
                        userId);
                }
                catch (WebPushException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Gone)
                {
                    // Souscription expirée (410 Gone) : la supprimer
                    _logger.LogWarning(
                        "[PUSH] Souscription expirée supprimée pour userId: {UserId}, endpoint: {Endpoint}",
                        userId, subscription.Endpoint.Substring(0, Math.Min(50, subscription.Endpoint.Length)));

                    _context.PushSubscriptions.Remove(subscription);
                }
                catch (WebPushException ex)
                {
                    _logger.LogError(ex,
                        "[PUSH] Erreur WebPush (Status: {StatusCode}) pour userId: {UserId}",
                        ex.StatusCode, userId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "[PUSH] Erreur lors de l'envoi de notification pour userId: {UserId}",
                        userId);
                }
            }

            // Sauvegarder les changements (LastUsedAt ou suppressions)
            await _context.SaveChangesAsync();

            return atLeastOneSuccess;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[PUSH] Erreur critique lors de l'envoi de notification pour userId: {UserId}",
                userId);
            return false;
        }
    }

    public async Task<int> SendToMultipleAsync(List<int> userIds, PushNotificationDto notification)
    {
        int successCount = 0;

        foreach (var userId in userIds)
        {
            if (await SendAsync(userId, notification))
            {
                successCount++;
            }
        }

        _logger.LogInformation(
            "[PUSH] Notifications envoyées à {SuccessCount}/{TotalCount} utilisateurs",
            successCount, userIds.Count);

        return successCount;
    }
}

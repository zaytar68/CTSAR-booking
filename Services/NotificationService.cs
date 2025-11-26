using System.Globalization;
using CTSAR.Booking.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace CTSAR.Booking.Services;

/// <summary>
/// Implémentation du service de notifications multi-canaux.
/// Supporte l'envoi de notifications par :
/// - Email (si NotifMail = true)
/// - Push Web (si Notif2 = true)
/// - Logs console (toujours actifs)
/// Architecture extensible pour ajouter d'autres canaux (SMS, Notif3, etc.)
/// </summary>
public class NotificationService : INotificationService
{
    private readonly ILogger<NotificationService> _logger;
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly IEmailTemplateService _emailTemplateService;
    private readonly IConfiguration _configuration;
    private readonly IPushNotificationService _pushService;

    public NotificationService(
        ILogger<NotificationService> logger,
        ApplicationDbContext context,
        IEmailService emailService,
        IEmailTemplateService emailTemplateService,
        IConfiguration configuration,
        IPushNotificationService pushService)
    {
        _logger = logger;
        _context = context;
        _emailService = emailService;
        _emailTemplateService = emailTemplateService;
        _configuration = configuration;
        _pushService = pushService;
    }

    /// <summary>
    /// Envoie une notification à un utilisateur
    /// </summary>
    public async Task NotifyAsync(string userId, string title, string message, NotificationType type)
    {
        _logger.LogInformation($"[NOTIF DEBUG NotifyAsync] DÉBUT - userId={userId}, title={title}");
        try
        {
            if (!int.TryParse(userId, out int userIdInt))
            {
                _logger.LogWarning("UserId invalide (pas un entier) : {UserId}", userId);
                return;
            }

            var user = await _context.Users.FindAsync(userIdInt);

            if (user == null)
            {
                _logger.LogWarning("Tentative de notification vers un utilisateur inexistant : {UserId}", userId);
                return;
            }

            _logger.LogInformation($"[NOTIF DEBUG NotifyAsync] User trouvé : {user.NomComplet} ({user.Email})");

            // Vérifier les préférences de notification
            bool shouldSendNotification = false;
            List<string> channels = new List<string>();

            if (user.NotifMail)
            {
                shouldSendNotification = true;
                channels.Add("Email");
            }
            if (user.Notif2)
            {
                shouldSendNotification = true;
                channels.Add("Canal2");
            }
            if (user.Notif3)
            {
                shouldSendNotification = true;
                channels.Add("Canal3");
            }

            if (!shouldSendNotification)
            {
                _logger.LogInformation(
                    "[NOTIFICATION IGNORÉE] L'utilisateur {UserEmail} a désactivé toutes les notifications",
                    user.Email);
                return;
            }

            // ================================================================
            // ENVOI DES NOTIFICATIONS PAR LES CANAUX ACTIVÉS
            // ================================================================

            // Canal 1 : Envoi par email
            if (user.NotifMail)
            {
                try
                {
                    // Utiliser le template multilingue
                    var baseUrl = _configuration["Application:BaseUrl"] ?? "http://localhost:5127";
                    var emailSubject = _emailTemplateService.GetSubject("NotificationGenerique");
                    var emailBody = await _emailTemplateService.RenderTemplateAsync("NotificationGenerique", new Dictionary<string, string>
                    {
                        { "Titre", title },
                        { "NomMembre", user.NomComplet },
                        { "Message", message },
                        { "UrlApplication", baseUrl },
                        { "Year", DateTime.Now.Year.ToString() },
                        { "Lang", CultureInfo.CurrentUICulture.TwoLetterISOLanguageName }
                    });

                    // Envoyer l'email de manière asynchrone (ne pas bloquer si ça échoue)
                    var emailSent = await _emailService.SendEmailAsync(
                        user.Email!,
                        emailSubject,
                        emailBody,
                        isHtml: true);

                    if (!emailSent)
                    {
                        _logger.LogWarning(
                            "[NOTIFICATION] Échec de l'envoi de l'email à {UserEmail}",
                            user.Email);
                    }
                }
                catch (Exception ex)
                {
                    // Ne pas bloquer toute la notification si l'email échoue
                    _logger.LogError(ex,
                        "[NOTIFICATION] Erreur lors de l'envoi de l'email à {UserEmail}",
                        user.Email);
                }
            }

            // Canal 2 : Notifications push Web
            if (user.Notif2)
            {
                try
                {
                    // Vérifier si l'utilisateur a une souscription push active
                    var hasSubscription = await _pushService.HasActiveSubscriptionAsync(userIdInt);

                    if (hasSubscription)
                    {
                        // Construire la notification push
                        var pushNotification = new PushNotificationDto
                        {
                            Title = title,
                            Body = message,
                            Url = "/planning",
                            Tag = type.ToString().ToLower(),
                            RequireInteraction = type == NotificationType.Warning || type == NotificationType.Error
                        };

                        var success = await _pushService.SendAsync(userIdInt, pushNotification);

                        if (success)
                        {
                            _logger.LogInformation(
                                "[NOTIFICATION] Notification push envoyée à {UserEmail}",
                                user.Email);
                        }
                        else
                        {
                            _logger.LogWarning(
                                "[NOTIFICATION] Échec de l'envoi push à {UserEmail}",
                                user.Email);
                        }
                    }
                    else
                    {
                        // L'utilisateur a activé Notif2 mais n'a pas de souscription active
                        // Ce n'est pas une erreur (changement de navigateur/appareil)
                        _logger.LogInformation(
                            "[NOTIFICATION] Notif2 activée pour {UserEmail} mais pas de souscription push active",
                            user.Email);
                    }
                }
                catch (Exception ex)
                {
                    // Ne pas bloquer toute la notification si le push échoue
                    _logger.LogError(ex,
                        "[NOTIFICATION] Erreur lors de l'envoi push à {UserEmail}",
                        user.Email);
                }
            }

            // Canal 3 : Réservé (usage futur)
            if (user.Notif3)
            {
                // TODO : Implémenter l'envoi via le canal 3
                _logger.LogInformation("[NOTIFICATION] Canal 3 activé mais pas encore implémenté");
            }

            // ================================================================
            // LOGS CONSOLE (TOUJOURS ACTIFS POUR DEBUG)
            // ================================================================

            var separator = new string('=', 80);
            Console.WriteLine();
            Console.WriteLine(separator);
            Console.WriteLine("📧 NOTIFICATION ENVOYÉE");
            Console.WriteLine(separator);
            Console.WriteLine($"Type         : {type}");
            Console.WriteLine($"Destinataire : {user.Prenom} {user.Nom} ({user.Email})");
            Console.WriteLine($"Canaux       : {string.Join(", ", channels)}");
            Console.WriteLine($"Titre        : {title}");
            Console.WriteLine($"Message      : {message}");
            Console.WriteLine(separator);
            Console.WriteLine();

            // Log normal pour les fichiers de log
            _logger.LogInformation(
                "[NOTIFICATION] {Type} | To: {UserEmail} ({UserName}) | Channels: {Channels} | Title: {Title} | Message: {Message}",
                type,
                user.Email,
                $"{user.Prenom} {user.Nom}",
                string.Join(", ", channels),
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

    /// <summary>
    /// Envoie une notification de confirmation de réservation à un membre
    /// </summary>
    public async Task NotifyReservationConfirmeeAsync(string userId, DateTime dateReservation, TimeSpan heureDebut, TimeSpan heureFin, string nomAlveole, string nomMoniteur)
    {
        if (!int.TryParse(userId, out int userIdInt))
        {
            _logger.LogWarning("UserId invalide : {UserId}", userId);
            return;
        }

        var user = await _context.Users.FindAsync(userIdInt);
        if (user == null || !user.NotifMail)
            return;

        try
        {
            var baseUrl = _configuration["Application:BaseUrl"] ?? "http://localhost:5127";

            // Définir la culture de l'utilisateur pour la génération du template
            var userCulture = !string.IsNullOrEmpty(user.PreferenceLangue)
                ? new CultureInfo(user.PreferenceLangue)
                : CultureInfo.CurrentUICulture;

            CultureInfo.CurrentUICulture = userCulture;

            var subject = _emailTemplateService.GetSubject("ConfirmationReservation");
            var emailBody = await _emailTemplateService.RenderTemplateAsync("ConfirmationReservation", new Dictionary<string, string>
            {
                { "NomMembre", user.NomComplet },
                { "DateReservation", dateReservation.ToString("dd/MM/yyyy") },
                { "HeureDebut", heureDebut.ToString(@"hh\:mm") },
                { "HeureFin", heureFin.ToString(@"hh\:mm") },
                { "NomAlveole", nomAlveole },
                { "NomMoniteur", nomMoniteur },
                { "UrlApplication", baseUrl },
                { "Year", DateTime.Now.Year.ToString() },
                { "Lang", userCulture.TwoLetterISOLanguageName }
            });

            await _emailService.SendEmailAsync(user.Email!, subject, emailBody, isHtml: true);
            _logger.LogInformation("Email de confirmation de réservation envoyé à {UserEmail}", user.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de l'envoi de l'email de confirmation à {UserId}", userId);
        }
    }

    /// <summary>
    /// Envoie une notification d'annulation de réservation à un membre
    /// </summary>
    public async Task NotifyReservationAnnuleeAsync(string userId, DateTime dateReservation, TimeSpan heureDebut, TimeSpan heureFin, string nomAlveole, string raisonAnnulation)
    {
        if (!int.TryParse(userId, out int userIdInt))
        {
            _logger.LogWarning("UserId invalide : {UserId}", userId);
            return;
        }

        var user = await _context.Users.FindAsync(userIdInt);
        if (user == null || !user.NotifMail)
            return;

        try
        {
            var baseUrl = _configuration["Application:BaseUrl"] ?? "http://localhost:5127";

            var userCulture = !string.IsNullOrEmpty(user.PreferenceLangue)
                ? new CultureInfo(user.PreferenceLangue)
                : CultureInfo.CurrentUICulture;

            CultureInfo.CurrentUICulture = userCulture;

            var subject = _emailTemplateService.GetSubject("AnnulationReservation");
            var emailBody = await _emailTemplateService.RenderTemplateAsync("AnnulationReservation", new Dictionary<string, string>
            {
                { "NomMembre", user.NomComplet },
                { "DateReservation", dateReservation.ToString("dd/MM/yyyy") },
                { "HeureDebut", heureDebut.ToString(@"hh\:mm") },
                { "HeureFin", heureFin.ToString(@"hh\:mm") },
                { "NomAlveole", nomAlveole },
                { "RaisonAnnulation", raisonAnnulation },
                { "UrlApplication", baseUrl },
                { "Year", DateTime.Now.Year.ToString() },
                { "Lang", userCulture.TwoLetterISOLanguageName }
            });

            await _emailService.SendEmailAsync(user.Email!, subject, emailBody, isHtml: true);
            _logger.LogInformation("Email d'annulation de réservation envoyé à {UserEmail}", user.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de l'envoi de l'email d'annulation à {UserId}", userId);
        }
    }

    /// <summary>
    /// Envoie une notification à plusieurs membres lorsqu'un moniteur valide sa présence
    /// </summary>
    public async Task NotifyMoniteurValideAsync(List<string> userIds, DateTime dateReservation, TimeSpan heureDebut, TimeSpan heureFin, string nomMoniteur)
    {
        foreach (var userId in userIds)
        {
            if (!int.TryParse(userId, out int userIdInt))
                continue;

            var user = await _context.Users.FindAsync(userIdInt);
            if (user == null || !user.NotifMail)
                continue;

            try
            {
                var baseUrl = _configuration["Application:BaseUrl"] ?? "http://localhost:5127";

                var userCulture = !string.IsNullOrEmpty(user.PreferenceLangue)
                    ? new CultureInfo(user.PreferenceLangue)
                    : CultureInfo.CurrentUICulture;

                CultureInfo.CurrentUICulture = userCulture;

                var subject = _emailTemplateService.GetSubject("MoniteurValide");
                var emailBody = await _emailTemplateService.RenderTemplateAsync("MoniteurValide", new Dictionary<string, string>
                {
                    { "NomMembre", user.NomComplet },
                    { "DateReservation", dateReservation.ToString("dd/MM/yyyy") },
                    { "HeureDebut", heureDebut.ToString(@"hh\:mm") },
                    { "HeureFin", heureFin.ToString(@"hh\:mm") },
                    { "NomMoniteur", nomMoniteur },
                    { "UrlApplication", baseUrl },
                    { "Year", DateTime.Now.Year.ToString() },
                    { "Lang", userCulture.TwoLetterISOLanguageName }
                });

                await _emailService.SendEmailAsync(user.Email!, subject, emailBody, isHtml: true);
                _logger.LogInformation("Email de validation moniteur envoyé à {UserEmail}", user.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'envoi de l'email à {UserId}", userId);
            }
        }
    }

    /// <summary>
    /// Envoie une notification à plusieurs membres lorsqu'un moniteur annule sa présence
    /// </summary>
    public async Task NotifyMoniteurAnnuleAsync(List<string> userIds, DateTime dateReservation, TimeSpan heureDebut, TimeSpan heureFin)
    {
        foreach (var userId in userIds)
        {
            if (!int.TryParse(userId, out int userIdInt))
                continue;

            var user = await _context.Users.FindAsync(userIdInt);
            if (user == null || !user.NotifMail)
                continue;

            try
            {
                var baseUrl = _configuration["Application:BaseUrl"] ?? "http://localhost:5127";

                var userCulture = !string.IsNullOrEmpty(user.PreferenceLangue)
                    ? new CultureInfo(user.PreferenceLangue)
                    : CultureInfo.CurrentUICulture;

                CultureInfo.CurrentUICulture = userCulture;

                var subject = _emailTemplateService.GetSubject("MoniteurAnnule");
                var emailBody = await _emailTemplateService.RenderTemplateAsync("MoniteurAnnule", new Dictionary<string, string>
                {
                    { "NomMembre", user.NomComplet },
                    { "DateReservation", dateReservation.ToString("dd/MM/yyyy") },
                    { "HeureDebut", heureDebut.ToString(@"hh\:mm") },
                    { "HeureFin", heureFin.ToString(@"hh\:mm") },
                    { "UrlApplication", baseUrl },
                    { "Year", DateTime.Now.Year.ToString() },
                    { "Lang", userCulture.TwoLetterISOLanguageName }
                });

                await _emailService.SendEmailAsync(user.Email!, subject, emailBody, isHtml: true);
                _logger.LogInformation("Email d'annulation moniteur envoyé à {UserEmail}", user.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'envoi de l'email à {UserId}", userId);
            }
        }
    }
}

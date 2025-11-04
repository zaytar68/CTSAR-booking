// ====================================================================
// EmailService.cs : Service d'envoi d'emails par SMTP
// ====================================================================
// Ce fichier implémente le service d'envoi d'emails en utilisant MailKit.
// MailKit est une bibliothèque moderne et sécurisée pour l'envoi d'emails.
//
// FONCTIONNALITÉS :
// - Envoi d'emails à un ou plusieurs destinataires
// - Support du HTML et du texte brut
// - Connexion sécurisée SSL/TLS
// - Gestion des erreurs avec logging
//
// CONFIGURATION :
// - Les paramètres SMTP sont lus depuis appsettings.json via SmtpSettings
// - Injecté via IOptions<SmtpSettings>

using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using CTSAR.Booking.Configuration;

namespace CTSAR.Booking.Services;

/// <summary>
/// Implémentation du service d'envoi d'emails par SMTP avec MailKit.
/// Permet d'envoyer des notifications par email aux utilisateurs.
/// </summary>
public class EmailService : IEmailService
{
    private readonly SmtpSettings _smtpSettings;
    private readonly ILogger<EmailService> _logger;

    /// <summary>
    /// Constructeur du service d'emails.
    /// </summary>
    /// <param name="smtpSettings">Configuration SMTP injectée depuis appsettings.json</param>
    /// <param name="logger">Logger pour tracer les envois et les erreurs</param>
    public EmailService(
        IOptions<SmtpSettings> smtpSettings,
        ILogger<EmailService> logger)
    {
        _smtpSettings = smtpSettings.Value;
        _logger = logger;
    }

    // ================================================================
    // MÉTHODES PUBLIQUES
    // ================================================================

    /// <summary>
    /// Envoie un email à un destinataire unique.
    /// </summary>
    public async Task<bool> SendEmailAsync(string to, string subject, string body, bool isHtml = true)
    {
        try
        {
            _logger.LogInformation(
                "[EMAIL] Préparation de l'envoi à {To} | Sujet: {Subject}",
                to,
                subject);

            var message = CreateEmailMessage(
                new List<string> { to },
                subject,
                body,
                isHtml);

            var success = await SendEmailViaSmtpAsync(message);

            if (success)
            {
                _logger.LogInformation(
                    "[EMAIL] ✅ Email envoyé avec succès à {To}",
                    to);
            }
            else
            {
                _logger.LogWarning(
                    "[EMAIL] ❌ Échec de l'envoi de l'email à {To}",
                    to);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[EMAIL] Erreur lors de l'envoi de l'email à {To}",
                to);
            return false;
        }
    }

    /// <summary>
    /// Envoie un email à plusieurs destinataires.
    /// </summary>
    public async Task<int> SendEmailToMultipleAsync(
        List<string> recipients,
        string subject,
        string body,
        bool isHtml = true)
    {
        if (recipients == null || recipients.Count == 0)
        {
            _logger.LogWarning("[EMAIL] Aucun destinataire spécifié");
            return 0;
        }

        _logger.LogInformation(
            "[EMAIL] Envoi d'un email à {Count} destinataire(s) | Sujet: {Subject}",
            recipients.Count,
            subject);

        int successCount = 0;

        // Envoyer un email individuel à chaque destinataire
        // (Évite de révéler les adresses email des autres destinataires)
        foreach (var recipient in recipients)
        {
            var success = await SendEmailAsync(recipient, subject, body, isHtml);
            if (success)
            {
                successCount++;
            }
        }

        _logger.LogInformation(
            "[EMAIL] Envoi terminé : {SuccessCount}/{TotalCount} emails envoyés avec succès",
            successCount,
            recipients.Count);

        return successCount;
    }

    // ================================================================
    // MÉTHODES PRIVÉES
    // ================================================================

    /// <summary>
    /// Crée un message email au format MimeMessage (MailKit).
    /// </summary>
    private MimeMessage CreateEmailMessage(
        List<string> recipients,
        string subject,
        string body,
        bool isHtml)
    {
        var message = new MimeMessage();

        // Expéditeur (From)
        message.From.Add(new MailboxAddress(
            _smtpSettings.FromName,
            _smtpSettings.FromEmail));

        // Destinataires (To)
        foreach (var recipient in recipients)
        {
            message.To.Add(MailboxAddress.Parse(recipient));
        }

        // Sujet
        message.Subject = subject;

        // Corps du message (HTML ou texte brut)
        var bodyBuilder = new BodyBuilder();
        if (isHtml)
        {
            bodyBuilder.HtmlBody = body;
        }
        else
        {
            bodyBuilder.TextBody = body;
        }

        message.Body = bodyBuilder.ToMessageBody();

        return message;
    }

    /// <summary>
    /// Envoie un message email via le serveur SMTP configuré.
    /// Utilise MailKit.Net.Smtp.SmtpClient.
    /// </summary>
    private async Task<bool> SendEmailViaSmtpAsync(MimeMessage message)
    {
        try
        {
            using var smtpClient = new SmtpClient();

            // Connexion au serveur SMTP
            // Port 587 nécessite STARTTLS, port 465 nécessite SSL/TLS
            var secureSocketOptions = _smtpSettings.Port == 587
                ? SecureSocketOptions.StartTls
                : (_smtpSettings.EnableSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.None);

            await smtpClient.ConnectAsync(
                _smtpSettings.Host,
                _smtpSettings.Port,
                secureSocketOptions);

            // Authentification
            await smtpClient.AuthenticateAsync(
                _smtpSettings.Username,
                _smtpSettings.Password);

            // Envoi du message
            await smtpClient.SendAsync(message);

            // Déconnexion
            await smtpClient.DisconnectAsync(true);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[EMAIL] Erreur SMTP lors de l'envoi du message | Host: {Host}:{Port}",
                _smtpSettings.Host,
                _smtpSettings.Port);
            return false;
        }
    }
}

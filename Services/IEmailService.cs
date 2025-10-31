// ====================================================================
// IEmailService.cs : Interface du service d'envoi d'emails
// ====================================================================
// Ce fichier définit le contrat pour le service d'envoi d'emails par SMTP.
// L'implémentation concrète utilise MailKit pour envoyer les emails.
//
// UTILISATION :
// - Injecter IEmailService dans les services qui ont besoin d'envoyer des emails
// - Exemples : NotificationService, UserService, etc.

namespace CTSAR.Booking.Services;

/// <summary>
/// Service d'envoi d'emails par SMTP.
/// Permet d'envoyer des notifications par email aux utilisateurs.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Envoie un email à un destinataire unique.
    /// </summary>
    /// <param name="to">
    /// Adresse email du destinataire.
    /// Exemple : "utilisateur@example.com"
    /// </param>
    /// <param name="subject">
    /// Sujet de l'email.
    /// Exemple : "Confirmation de réservation"
    /// </param>
    /// <param name="body">
    /// Corps du message.
    /// Peut être du HTML si isHtml=true, sinon du texte brut.
    /// </param>
    /// <param name="isHtml">
    /// Indique si le corps du message est au format HTML.
    /// Par défaut : true
    /// </param>
    /// <returns>
    /// True si l'email a été envoyé avec succès, false sinon.
    /// </returns>
    /// <example>
    /// await _emailService.SendEmailAsync(
    ///     "membre@ctsar.fr",
    ///     "Réservation confirmée",
    ///     "<h1>Votre réservation est confirmée</h1><p>Merci !</p>",
    ///     isHtml: true);
    /// </example>
    Task<bool> SendEmailAsync(string to, string subject, string body, bool isHtml = true);

    /// <summary>
    /// Envoie un email à plusieurs destinataires.
    /// </summary>
    /// <param name="recipients">
    /// Liste des adresses email des destinataires.
    /// Exemple : ["user1@example.com", "user2@example.com"]
    /// </param>
    /// <param name="subject">
    /// Sujet de l'email.
    /// Exemple : "Annulation de réservation"
    /// </param>
    /// <param name="body">
    /// Corps du message.
    /// Peut être du HTML si isHtml=true, sinon du texte brut.
    /// </param>
    /// <param name="isHtml">
    /// Indique si le corps du message est au format HTML.
    /// Par défaut : true
    /// </param>
    /// <returns>
    /// Nombre d'emails envoyés avec succès.
    /// Si tous les envois échouent, retourne 0.
    /// </returns>
    /// <example>
    /// var recipients = new List&lt;string&gt; { "user1@ctsar.fr", "user2@ctsar.fr" };
    /// int sent = await _emailService.SendEmailToMultipleAsync(
    ///     recipients,
    ///     "Fermeture exceptionnelle",
    ///     "<p>Le club sera fermé demain.</p>");
    /// </example>
    Task<int> SendEmailToMultipleAsync(List<string> recipients, string subject, string body, bool isHtml = true);
}

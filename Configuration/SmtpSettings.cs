// ====================================================================
// SmtpSettings.cs : Configuration SMTP pour l'envoi d'emails
// ====================================================================
// Ce fichier définit la classe de configuration pour le service SMTP.
// Les valeurs sont lues depuis appsettings.json et injectées via IOptions.
//
// UTILISATION :
// - Configurer dans appsettings.json sous la section "SmtpSettings"
// - Injecter via IOptions<SmtpSettings> dans les services
//
// SÉCURITÉ :
// - NE PAS commit les vrais identifiants SMTP dans le code source
// - Utiliser des User Secrets en développement
// - Utiliser des variables d'environnement en production

using System.ComponentModel.DataAnnotations;

namespace CTSAR.Booking.Configuration;

/// <summary>
/// Paramètres de configuration pour le serveur SMTP.
/// Utilisé pour envoyer des emails de notification aux utilisateurs.
/// </summary>
public class SmtpSettings
{
    /// <summary>
    /// Adresse du serveur SMTP.
    /// Exemples :
    /// - Gmail : smtp.gmail.com
    /// - Outlook : smtp-mail.outlook.com
    /// - Office 365 : smtp.office365.com
    /// - Serveur local : localhost
    /// </summary>
    [Required(ErrorMessage = "L'adresse du serveur SMTP est obligatoire")]
    public string Host { get; set; } = string.Empty;

    /// <summary>
    /// Port du serveur SMTP.
    /// Ports standards :
    /// - 587 : Port standard pour SMTP avec TLS (STARTTLS)
    /// - 465 : Port standard pour SMTP avec SSL
    /// - 25 : Port non chiffré (déconseillé)
    /// </summary>
    [Range(1, 65535, ErrorMessage = "Le port doit être entre 1 et 65535")]
    public int Port { get; set; } = 587;

    /// <summary>
    /// Activer la connexion sécurisée SSL/TLS.
    /// Recommandé : true
    /// </summary>
    public bool EnableSsl { get; set; } = true;

    /// <summary>
    /// Nom d'utilisateur pour l'authentification SMTP.
    /// Généralement l'adresse email du compte expéditeur.
    /// Exemple : moncompte@gmail.com
    /// </summary>
    [Required(ErrorMessage = "Le nom d'utilisateur SMTP est obligatoire")]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Mot de passe pour l'authentification SMTP.
    /// IMPORTANT :
    /// - Gmail : Utiliser un "App Password" (mot de passe d'application)
    /// - Outlook : Mot de passe du compte Microsoft
    /// - NE JAMAIS commit ce mot de passe dans le code source !
    /// </summary>
    [Required(ErrorMessage = "Le mot de passe SMTP est obligatoire")]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Adresse email de l'expéditeur (From).
    /// Cette adresse apparaîtra comme l'expéditeur des emails.
    /// Exemple : noreply@ctsar.fr
    /// </summary>
    [Required(ErrorMessage = "L'adresse email de l'expéditeur est obligatoire")]
    [EmailAddress(ErrorMessage = "L'adresse email de l'expéditeur n'est pas valide")]
    public string FromEmail { get; set; } = string.Empty;

    /// <summary>
    /// Nom affiché de l'expéditeur (From Name).
    /// Ce nom apparaîtra à côté de l'adresse email dans la boîte de réception.
    /// Exemple : "CTSAR Booking"
    /// </summary>
    [Required(ErrorMessage = "Le nom de l'expéditeur est obligatoire")]
    public string FromName { get; set; } = string.Empty;
}

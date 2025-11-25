using System.ComponentModel.DataAnnotations;

namespace CTSAR.Booking.Configuration;

/// <summary>
/// Configuration pour les clés VAPID (Voluntary Application Server Identification)
/// utilisées pour les notifications push Web.
/// </summary>
public class VapidSettings
{
    /// <summary>
    /// Adresse email de contact de l'administrateur au format mailto:email@domaine.com
    /// Utilisée pour identifier le serveur d'application auprès des services de push.
    /// </summary>
    [Required]
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// Clé publique VAPID (Base64 URL-safe encoded)
    /// Partagée avec les clients pour créer des souscriptions push.
    /// </summary>
    [Required]
    public string PublicKey { get; set; } = string.Empty;

    /// <summary>
    /// Clé privée VAPID (Base64 URL-safe encoded)
    /// À GARDER SECRÈTE - Ne jamais committer dans Git.
    /// Utilisée uniquement côté serveur pour signer les requêtes push.
    /// </summary>
    [Required]
    public string PrivateKey { get; set; } = string.Empty;
}

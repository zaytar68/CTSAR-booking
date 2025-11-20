namespace CTSAR.Booking.Services;

/// <summary>
/// Service pour gérer les templates d'emails multilingues
/// </summary>
public interface IEmailTemplateService
{
    /// <summary>
    /// Récupère le sujet localisé d'un email
    /// </summary>
    /// <param name="templateName">Nom du template (ex: "ConfirmationReservation")</param>
    /// <returns>Le sujet traduit dans la langue courante</returns>
    string GetSubject(string templateName);

    /// <summary>
    /// Génère le contenu HTML d'un email à partir d'un template
    /// </summary>
    /// <param name="templateName">Nom du template (ex: "ConfirmationReservation")</param>
    /// <param name="variables">Dictionnaire des variables à remplacer (ex: {"NomMembre", "Jean"})</param>
    /// <returns>Le HTML complet avec les variables remplacées</returns>
    Task<string> RenderTemplateAsync(string templateName, Dictionary<string, string> variables);

    /// <summary>
    /// Vérifie si un template existe pour la culture courante
    /// </summary>
    /// <param name="templateName">Nom du template</param>
    /// <returns>True si le template existe</returns>
    bool TemplateExists(string templateName);
}

using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Localization;

namespace CTSAR.Booking.Services;

/// <summary>
/// Implémentation du service de templates d'emails multilingues
/// </summary>
public class EmailTemplateService : IEmailTemplateService
{
    private readonly IStringLocalizer<EmailTemplateService> _localizer;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<EmailTemplateService> _logger;
    private readonly string _templatesBasePath;

    public EmailTemplateService(
        IStringLocalizer<EmailTemplateService> localizer,
        IWebHostEnvironment environment,
        ILogger<EmailTemplateService> logger)
    {
        _localizer = localizer;
        _environment = environment;
        _logger = logger;
        _templatesBasePath = Path.Combine(_environment.ContentRootPath, "EmailTemplates");
    }

    /// <inheritdoc/>
    public string GetSubject(string templateName)
    {
        var key = $"{templateName}_Subject";
        var subject = _localizer[key];

        if (subject.ResourceNotFound)
        {
            _logger.LogWarning("Sujet d'email non trouvé pour le template: {TemplateName}", templateName);
            return templateName; // Fallback au nom du template
        }

        return subject.Value;
    }

    /// <inheritdoc/>
    public async Task<string> RenderTemplateAsync(string templateName, Dictionary<string, string> variables)
    {
        var culture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

        // Chercher le template dans l'ordre: culture spécifique -> fr (défaut)
        var templatePath = GetTemplatePath(templateName, culture);

        if (!File.Exists(templatePath))
        {
            // Fallback vers le français
            templatePath = GetTemplatePath(templateName, "fr");

            if (!File.Exists(templatePath))
            {
                _logger.LogError("Template d'email non trouvé: {TemplateName} pour la culture {Culture}",
                    templateName, culture);
                throw new FileNotFoundException($"Template d'email non trouvé: {templateName}");
            }
        }

        // Lire le contenu du template
        var templateContent = await File.ReadAllTextAsync(templatePath);

        // Charger le layout de base si disponible
        var layoutPath = Path.Combine(_templatesBasePath, "Layouts", "BaseLayout.html");
        if (File.Exists(layoutPath))
        {
            var layoutContent = await File.ReadAllTextAsync(layoutPath);
            templateContent = layoutContent.Replace("{{Content}}", templateContent);
        }

        // Remplacer les variables
        var result = ReplaceVariables(templateContent, variables);

        return result;
    }

    /// <inheritdoc/>
    public bool TemplateExists(string templateName)
    {
        var culture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        var templatePath = GetTemplatePath(templateName, culture);

        if (File.Exists(templatePath))
            return true;

        // Vérifier le fallback français
        templatePath = GetTemplatePath(templateName, "fr");
        return File.Exists(templatePath);
    }

    /// <summary>
    /// Construit le chemin du fichier template
    /// </summary>
    private string GetTemplatePath(string templateName, string culture)
    {
        return Path.Combine(_templatesBasePath, culture, $"{templateName}.html");
    }

    /// <summary>
    /// Remplace les variables {{Variable}} par leurs valeurs
    /// </summary>
    private string ReplaceVariables(string content, Dictionary<string, string> variables)
    {
        if (variables == null || variables.Count == 0)
            return content;

        var result = content;

        foreach (var variable in variables)
        {
            // Remplacer {{NomVariable}} par la valeur
            var pattern = $"{{{{{variable.Key}}}}}";
            result = result.Replace(pattern, variable.Value ?? string.Empty);
        }

        // Log des variables non remplacées (aide au debug)
        var unreplacedVariables = Regex.Matches(result, @"\{\{(\w+)\}\}");
        if (unreplacedVariables.Count > 0)
        {
            var variableNames = string.Join(", ", unreplacedVariables.Select(m => m.Groups[1].Value));
            _logger.LogWarning("Variables non remplacées dans le template: {Variables}", variableNames);
        }

        return result;
    }
}

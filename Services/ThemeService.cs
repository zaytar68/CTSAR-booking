// ====================================================================
// ThemeService.cs : Service pour gérer le thème global de l'application
// ====================================================================
// Ce service permet de partager l'état du thème entre tous les composants.

using Blazored.LocalStorage;
using System.Globalization;

namespace CTSAR.Booking.Services;

/// <summary>
/// Service pour gérer le thème (clair/foncé) et la langue de l'application.
/// </summary>
public class ThemeService
{
    private readonly ILocalStorageService _localStorage;
    private bool _isDarkMode = false;
    private string _selectedLanguage = "fr";

    /// <summary>
    /// Événement déclenché quand le thème change.
    /// </summary>
    public event Action? OnThemeChanged;

    /// <summary>
    /// Événement déclenché quand la langue change.
    /// </summary>
    public event Action? OnLanguageChanged;

    public ThemeService(ILocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }

    /// <summary>
    /// Obtient ou définit si le mode sombre est activé.
    /// </summary>
    public bool IsDarkMode
    {
        get => _isDarkMode;
        set
        {
            if (_isDarkMode != value)
            {
                _isDarkMode = value;
                OnThemeChanged?.Invoke();
            }
        }
    }

    /// <summary>
    /// Obtient ou définit la langue sélectionnée.
    /// </summary>
    public string SelectedLanguage
    {
        get => _selectedLanguage;
        set
        {
            if (_selectedLanguage != value)
            {
                _selectedLanguage = value;
                OnLanguageChanged?.Invoke();
            }
        }
    }

    /// <summary>
    /// Initialise le service en chargeant les préférences depuis le LocalStorage.
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            var savedTheme = await _localStorage.GetItemAsync<bool?>("isDarkMode");
            if (savedTheme.HasValue)
            {
                _isDarkMode = savedTheme.Value;
            }

            var savedLanguage = await _localStorage.GetItemAsync<string>("selectedLanguage");
            if (!string.IsNullOrEmpty(savedLanguage))
            {
                _selectedLanguage = savedLanguage;

                // Appliquer la culture sauvegardée
                var culture = new CultureInfo(savedLanguage);
                CultureInfo.CurrentCulture = culture;
                CultureInfo.CurrentUICulture = culture;
            }
        }
        catch (Exception)
        {
            // En cas d'erreur, utiliser les valeurs par défaut
        }
    }

    /// <summary>
    /// Bascule entre le mode clair et le mode sombre.
    /// </summary>
    public async Task ToggleThemeAsync()
    {
        IsDarkMode = !IsDarkMode;

        try
        {
            await _localStorage.SetItemAsync("isDarkMode", IsDarkMode);
        }
        catch (Exception)
        {
            // Ignorer l'erreur de sauvegarde
        }
    }

    /// <summary>
    /// Change la langue de l'application et met à jour la culture.
    /// </summary>
    public async Task SetLanguageAsync(string language)
    {
        SelectedLanguage = language;

        // Changer la culture de l'application
        var culture = new CultureInfo(language);
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;

        try
        {
            await _localStorage.SetItemAsync("selectedLanguage", SelectedLanguage);
        }
        catch (Exception)
        {
            // Ignorer l'erreur de sauvegarde
        }
    }
}

// ====================================================================
// IMPORTS : Tous les namespaces nécessaires à l'application
// ====================================================================
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using CTSAR.Booking.Components;
using CTSAR.Booking.Data;
using CTSAR.Booking.Services;
using MudBlazor.Services;  // Pour MudBlazor (interface utilisateur moderne)
using Blazored.LocalStorage;  // Pour le stockage local (thème, langue, etc.)
using System.Globalization;  // Pour la gestion des cultures (langues)

// ====================================================================
// CONFIGURATION DE L'APPLICATION
// Le builder est utilisé pour ajouter tous les services nécessaires
// ====================================================================
var builder = WebApplication.CreateBuilder(args);

// --------------------------------------------------------------------
// SERVICES BLAZOR : Configuration des composants Razor
// --------------------------------------------------------------------
// AddRazorComponents() : Active les composants Blazor
// AddInteractiveServerComponents() : Active le mode Interactive Server
//   (tout s'exécute côté serveur, communication via SignalR)
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// --------------------------------------------------------------------
// HTTP CONTEXT : Nécessaire pour accéder au HttpContext dans les composants
// --------------------------------------------------------------------
builder.Services.AddHttpContextAccessor();

// --------------------------------------------------------------------
// CONTROLLERS : Support des contrôleurs MVC (pour CultureController)
// --------------------------------------------------------------------
builder.Services.AddControllers();

// --------------------------------------------------------------------
// MUDBLAZOR : Framework UI moderne (Material Design)
// --------------------------------------------------------------------
// Ajoute tous les services MudBlazor (dialogues, snackbars, etc.)
builder.Services.AddMudServices();

// --------------------------------------------------------------------
// LOCAL STORAGE : Stockage local pour les préférences utilisateur
// --------------------------------------------------------------------
// Permet de sauvegarder le thème, la langue, etc. dans le navigateur
builder.Services.AddBlazoredLocalStorage();

// --------------------------------------------------------------------
// LOCALISATION : Configuration du système multilingue
// --------------------------------------------------------------------
// Configure les cultures supportées par l'application
var supportedCultures = new[] { "fr", "de", "en" };
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture("fr");
    options.SupportedCultures = supportedCultures.Select(c => new CultureInfo(c)).ToList();
    options.SupportedUICultures = supportedCultures.Select(c => new CultureInfo(c)).ToList();
});

// Ajoute les services de localisation
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

// --------------------------------------------------------------------
// AUTHENTIFICATION : Configuration du système custom (sans Identity)
// --------------------------------------------------------------------
// CascadingAuthenticationState : Permet de partager l'état d'authentification
//   dans toute l'application (savoir si l'utilisateur est connecté)
builder.Services.AddCascadingAuthenticationState();

// Notre AuthenticationStateProvider custom
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();
builder.Services.AddScoped<CustomAuthenticationStateProvider>();

// Configuration de l'authentification par cookies
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
        options.AccessDeniedPath = "/login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);  // Session de 8 heures par défaut
        options.SlidingExpiration = true;  // Renouveler la session automatiquement
    });

// --------------------------------------------------------------------
// BASE DE DONNÉES : Configuration d'Entity Framework Core avec SQLite
// --------------------------------------------------------------------
// Récupère la chaîne de connexion depuis appsettings.json
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// Configure le contexte de base de données pour utiliser SQLite
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

// Page d'erreur détaillée pour les problèmes de base de données (développement uniquement)
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// --------------------------------------------------------------------
// NOS SERVICES MÉTIER : Services personnalisés de l'application
// --------------------------------------------------------------------
// Service d'authentification custom (remplace UserManager/SignInManager)
builder.Services.AddScoped<AuthService>();

// Ajoute notre UserService pour gérer les utilisateurs
// Scoped : Une nouvelle instance par requête HTTP
builder.Services.AddScoped<UserService>();

// Ajoute notre ThemeService pour gérer le thème et la langue
// Scoped : Une nouvelle instance par requête HTTP
builder.Services.AddScoped<ThemeService>();

// Ajoute AlveoleService pour gérer les alvéoles (postes de tir)
builder.Services.AddScoped<AlveoleService>();

// Ajoute ReservationService pour gérer les inscriptions de tir
builder.Services.AddScoped<ReservationService>();

// Ajoute FermetureClubService pour gérer les fermetures planifiées du club
builder.Services.AddScoped<FermetureClubService>();

// Ajoute NotificationService pour envoyer des notifications aux utilisateurs
builder.Services.AddScoped<INotificationService, NotificationService>();

// --------------------------------------------------------------------
// AUTORISATION : Configuration des policies (règles d'accès)
// --------------------------------------------------------------------
// Une policy est une règle qui définit qui peut accéder à quoi
builder.Services.AddAuthorization(options =>
{
    // Policy "AdminOnly" : Seuls les Administrateurs peuvent accéder
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Administrateur"));

    // Policy "MoniteurOrAdmin" : Moniteurs OU Administrateurs peuvent accéder
    options.AddPolicy("MoniteurOrAdmin", policy =>
        policy.RequireRole("Moniteur", "Administrateur"));
});

// ====================================================================
// CONSTRUCTION DE L'APPLICATION
// Après avoir configuré tous les services, on construit l'app
// ====================================================================
var app = builder.Build();

// --------------------------------------------------------------------
// INITIALISATION DE LA BASE DE DONNÉES (au démarrage de l'app)
// --------------------------------------------------------------------
// Crée un scope pour accéder aux services
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        // Récupère le contexte de base de données
        var context = services.GetRequiredService<ApplicationDbContext>();

        // Applique les migrations automatiquement (crée/met à jour la base)
        context.Database.Migrate();

        // Initialise les rôles et l'utilisateur admin
        await DbSeeder.SeedAsync(context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Une erreur est survenue lors de l'initialisation de la base de données.");
    }
}

// ====================================================================
// PIPELINE HTTP : Configuration de la façon dont les requêtes sont traitées
// ====================================================================

// En développement : page d'erreur détaillée pour les migrations
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else  // En production : gestion d'erreur simple
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // HSTS : Force l'utilisation de HTTPS
    app.UseHsts();
}

// Redirige automatiquement HTTP vers HTTPS
app.UseHttpsRedirection();

// Sert les fichiers statiques (CSS, JS, images)
app.UseStaticFiles();

// AUTHENTICATION ET AUTHORIZATION (requis pour antiforgery)
app.UseAuthentication();
app.UseAuthorization();

// Protection contre les attaques CSRF (Cross-Site Request Forgery)
app.UseAntiforgery();

// Active le middleware de localisation
app.UseRequestLocalization();

// Map les controllers (pour CultureController)
app.MapControllers();

// Configure les composants Razor en mode Interactive Server
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// ====================================================================
// DÉMARRAGE DE L'APPLICATION
// ====================================================================
app.Run();

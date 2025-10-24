// ====================================================================
// CustomAuthenticationStateProvider.cs : Gère l'état d'authentification
// ====================================================================
// Remplace le AuthenticationStateProvider d'Identity.
// Inspiré de "Web Development with Blazor - Third Edition", Chapter 8.

using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Identity;

namespace CTSAR.Booking.Services;

/// <summary>
/// Fournisseur custom de l'état d'authentification.
/// Gère la connexion/déconnexion et l'état de l'utilisateur.
/// </summary>
public class CustomAuthenticationStateProvider : RevalidatingServerAuthenticationStateProvider
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CustomAuthenticationStateProvider> _logger;

    public CustomAuthenticationStateProvider(
        ILoggerFactory loggerFactory,
        IServiceScopeFactory scopeFactory)
        : base(loggerFactory)
    {
        _scopeFactory = scopeFactory;
        _logger = loggerFactory.CreateLogger<CustomAuthenticationStateProvider>();
    }

    /// <summary>
    /// Intervalle de revalidation de l'état d'authentification.
    /// Vérifie toutes les 30 minutes si l'utilisateur est toujours valide.
    /// </summary>
    protected override TimeSpan RevalidationInterval => TimeSpan.FromMinutes(30);

    /// <summary>
    /// Vérifie si le principal d'authentification est toujours valide.
    /// </summary>
    protected override async Task<bool> ValidateAuthenticationStateAsync(
        AuthenticationState authenticationState,
        CancellationToken cancellationToken)
    {
        try
        {
            // Récupérer le ClaimsPrincipal de l'état d'authentification
            var user = authenticationState.User;

            if (user?.Identity == null || !user.Identity.IsAuthenticated)
            {
                return false;
            }

            // Récupérer l'ID de l'utilisateur depuis les claims
            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                _logger.LogWarning("Impossible de récupérer l'ID de l'utilisateur depuis les claims");
                return false;
            }

            // Vérifier si l'utilisateur existe toujours et est actif
            using var scope = _scopeFactory.CreateScope();
            var authService = scope.ServiceProvider.GetRequiredService<AuthService>();
            var dbUser = await authService.GetUserByIdAsync(userId);

            if (dbUser == null || !dbUser.IsActive)
            {
                _logger.LogWarning("L'utilisateur {UserId} n'existe plus ou est désactivé", userId);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la validation de l'état d'authentification");
            return false;
        }
    }

    /// <summary>
    /// Marque un utilisateur comme authentifié.
    /// À appeler après une connexion réussie.
    /// </summary>
    public void MarkUserAsAuthenticated(string email, int userId, List<string> roles)
    {
        try
        {
            // Créer les claims de l'utilisateur
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, email),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            };

            // Ajouter un claim pour chaque rôle
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            // Créer l'identité
            var identity = new ClaimsIdentity(claims, "CustomAuth");
            var user = new ClaimsPrincipal(identity);

            // Notifier le changement d'état d'authentification
            var authState = Task.FromResult(new AuthenticationState(user));
            NotifyAuthenticationStateChanged(authState);

            _logger.LogInformation("Utilisateur marqué comme authentifié : {Email}", email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la mise à jour de l'état d'authentification pour : {Email}", email);
        }
    }

    /// <summary>
    /// Marque un utilisateur comme déconnecté.
    /// À appeler après une déconnexion.
    /// </summary>
    public void MarkUserAsLoggedOut()
    {
        try
        {
            // Créer un utilisateur anonyme
            var anonymous = new ClaimsPrincipal(new ClaimsIdentity());
            var authState = Task.FromResult(new AuthenticationState(anonymous));

            // Notifier le changement d'état
            NotifyAuthenticationStateChanged(authState);

            _logger.LogInformation("Utilisateur déconnecté");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la déconnexion");
        }
    }
}

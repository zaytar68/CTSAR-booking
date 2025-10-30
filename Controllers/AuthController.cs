using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using CTSAR.Booking.Services;

namespace CTSAR.Booking.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(AuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            // Authentifier l'utilisateur
            var user = await _authService.LoginAsync(request.Email, request.Password);

            if (user == null)
            {
                return Unauthorized(new { message = "Email ou mot de passe incorrect" });
            }

            // Récupérer les rôles de l'utilisateur
            var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();

            // Créer les claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Email),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.GivenName, user.Prenom),
                new Claim(ClaimTypes.Surname, user.Nom)
            };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            // Créer l'identité et le principal
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            // Se connecter avec le cookie
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = request.RememberMe,
                    ExpiresUtc = request.RememberMe ? DateTimeOffset.UtcNow.AddDays(7) : DateTimeOffset.UtcNow.AddHours(8)
                });

            _logger.LogInformation("Utilisateur connecté : {Email}", user.Email);

            return Ok(new
            {
                userId = user.Id,
                email = user.Email,
                nom = user.Nom,
                prenom = user.Prenom,
                roles = roles
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la connexion de {Email}", request.Email);
            return StatusCode(500, new { message = "Une erreur est survenue lors de la connexion" });
        }
    }

    [HttpPost("login-page")]
    [Consumes("application/x-www-form-urlencoded")]
    public async Task<IActionResult> LoginPage([FromForm] string email, [FromForm] string password, [FromForm] bool rememberMe = false, [FromForm] string? returnUrl = null)
    {
        try
        {
            // Authentifier l'utilisateur
            var user = await _authService.LoginAsync(email, password);

            if (user == null)
            {
                return Redirect($"/login?error=invalid&returnUrl={Uri.EscapeDataString(returnUrl ?? "/")}");
            }

            // Récupérer les rôles de l'utilisateur
            var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();

            // Créer les claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Email),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.GivenName, user.Prenom),
                new Claim(ClaimTypes.Surname, user.Nom)
            };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            // Créer l'identité et le principal
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            // Se connecter avec le cookie
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = rememberMe,
                    ExpiresUtc = rememberMe ? DateTimeOffset.UtcNow.AddDays(7) : DateTimeOffset.UtcNow.AddHours(8)
                });

            _logger.LogInformation("Utilisateur connecté via formulaire : {Email}", user.Email);

            return Redirect(returnUrl ?? "/");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la connexion de {Email}", email);
            return Redirect($"/login?error=unexpected&returnUrl={Uri.EscapeDataString(returnUrl ?? "/")}");
        }
    }

    [HttpPost("register-page")]
    [Consumes("application/x-www-form-urlencoded")]
    public async Task<IActionResult> RegisterPage(
        [FromForm] string email,
        [FromForm] string password,
        [FromForm] string confirmPassword,
        [FromForm] string nom,
        [FromForm] string prenom,
        [FromForm] string role,
        [FromForm] string? returnUrl = null)
    {
        try
        {
            // Valider que les mots de passe correspondent
            if (password != confirmPassword)
            {
                return Redirect($"/register?error=passwordmismatch&returnUrl={Uri.EscapeDataString(returnUrl ?? "/")}");
            }

            // Valider la force du mot de passe
            if (password.Length < 6)
            {
                return Redirect($"/register?error=weak&returnUrl={Uri.EscapeDataString(returnUrl ?? "/")}");
            }

            // Créer l'utilisateur
            var (success, errorMessage, user) = await _authService.RegisterAsync(email, password, nom, prenom, role);

            if (!success || user == null)
            {
                var errorCode = errorMessage?.Contains("existe déjà") == true ? "exists" : "unexpected";
                return Redirect($"/register?error={errorCode}&returnUrl={Uri.EscapeDataString(returnUrl ?? "/")}");
            }

            // Récupérer les rôles de l'utilisateur
            var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();

            // Créer les claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Email),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.GivenName, user.Prenom),
                new Claim(ClaimTypes.Surname, user.Nom)
            };

            foreach (var userRole in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, userRole));
            }

            // Créer l'identité et le principal
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            // Se connecter automatiquement après l'inscription
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = false,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
                });

            _logger.LogInformation("Nouvel utilisateur inscrit et connecté : {Email}", user.Email);

            return Redirect(returnUrl ?? "/");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de l'inscription de {Email}", email);
            return Redirect($"/register?error=unexpected&returnUrl={Uri.EscapeDataString(returnUrl ?? "/")}");
        }
    }

    [HttpGet("logout-page")]
    [HttpPost("logout-page")]
    public async Task<IActionResult> LogoutPage()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        _logger.LogInformation("Utilisateur déconnecté");
        return Redirect("/");
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Ok();
    }
}

public class LoginRequest
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public bool RememberMe { get; set; }
}

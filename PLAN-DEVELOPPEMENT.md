# 🗺️ PLAN DE DÉVELOPPEMENT - CTSAR BOOKING

**Application de réservation d'alvéoles pour club de tir**

---

## 📋 CONFIGURATION DU PROJET

| Élément | Choix |
|---------|-------|
| **Nom du projet** | CTSAR.Booking |
| **Type** | Blazor Web App (.NET 8) - Interactive Server |
| **Interface** | MudBlazor (Material Design - joli et moderne) |
| **Validation** | Data Annotations (simple avec [Required], [EmailAddress], etc.) |
| **Base de données** | SQLite (fichier local - simple pour développement) |
| **Compte admin** | Login: `admin` / Mot de passe: `admin` |

---

## 🏗️ ARCHITECTURE SIMPLIFIÉE

```
CTSAR.Booking/
│
├── Components/                      ← TOUTES LES PAGES ET COMPOSANTS
│   ├── Account/                     ← Pages de connexion (du template)
│   │   ├── Login.razor              ← Page de connexion
│   │   └── Register.razor           ← Page d'inscription
│   │
│   ├── Pages/                       ← PAGES PRINCIPALES
│   │   ├── Home.razor               ← Page d'accueil
│   │   ├── MonProfil.razor          ← Page profil utilisateur
│   │   │
│   │   ├── Admin/                   ← PAGES ADMINISTRATION (Admin seulement)
│   │   │   └── Utilisateurs/
│   │   │       └── Liste.razor      ← Liste et gestion des utilisateurs
│   │   │
│   │   └── Planning/                ← PLANNING (tout le monde - Phase 4)
│   │       └── Index.razor          ← Calendrier des réservations
│   │
│   ├── Layout/                      ← MISE EN PAGE
│   │   ├── MainLayout.razor         ← Layout principal avec menu
│   │   └── NavMenu.razor            ← Menu de navigation
│   │
│   └── Shared/                      ← COMPOSANTS RÉUTILISABLES
│       ├── UserCard.razor           ← Carte utilisateur
│       └── ConfirmDialog.razor      ← Dialogue de confirmation
│
├── Data/                            ← BASE DE DONNÉES
│   ├── ApplicationDbContext.cs      ← Configuration base de données
│   ├── DbInitializer.cs             ← Création des données de départ
│   │
│   └── Models/                      ← MODÈLES (tables de la base)
│       ├── ApplicationUser.cs       ← Utilisateur (hérite de IdentityUser)
│       ├── Alveole.cs              ← Stand de tir (Phase 3)
│       ├── Reservation.cs          ← Réservation (Phase 4)
│       └── PeriodeFermeture.cs     ← Fermeture alvéole (Phase 3)
│
├── Services/                        ← LOGIQUE MÉTIER (le cerveau de l'app)
│   ├── UserService.cs               ← Tout ce qui concerne les utilisateurs
│   ├── AlveoleService.cs           ← Gestion des alvéoles (Phase 3)
│   ├── ReservationService.cs       ← Gestion des réservations (Phase 4)
│   └── NotificationService.cs      ← Envoi de notifications (Phase 5)
│
├── DTOs/                            ← OBJETS DE TRANSFERT (données simplifiées)
│   └── UserDto.cs                   ← Versions simplifiées de User pour les formulaires
│
├── Constants/                       ← CONSTANTES (valeurs fixes)
│   └── RoleConstants.cs             ← Noms des rôles (Administrateur, Membre, Moniteur)
│
├── Migrations/                      ← HISTORIQUE BASE DE DONNÉES (auto-généré)
│
├── wwwroot/                         ← FICHIERS PUBLICS (CSS, images, etc.)
│
├── Program.cs                       ← POINT D'ENTRÉE (configuration de l'app)
├── appsettings.json                ← CONFIGURATION (connexion BDD, etc.)
└── _Imports.razor                  ← Imports communs pour toutes les pages

```

---

## 🎯 PHASE 1 : GESTION DES UTILISATEURS

**Objectif** : Créer l'interface pour gérer les membres du club (admin uniquement)

### 📦 Ce qu'on va créer

#### 1️⃣ **Modèle de données étendu** (Qui sont les utilisateurs ?)

**Fichier : `Data/Models/ApplicationUser.cs`**

```csharp
/// <summary>
/// Représente un utilisateur de l'application (membre du club).
/// Hérite de IdentityUser qui fournit déjà Email, Password, etc.
/// </summary>
public class ApplicationUser : IdentityUser
{
    /// <summary>
    /// Nom de famille de l'utilisateur (ex: "Dupont")
    /// Obligatoire, maximum 100 caractères
    /// </summary>
    [Required(ErrorMessage = "Le nom est obligatoire")]
    [MaxLength(100)]
    public string Nom { get; set; } = string.Empty;

    /// <summary>
    /// Prénom de l'utilisateur (ex: "Jean")
    /// Obligatoire, maximum 100 caractères
    /// </summary>
    [Required(ErrorMessage = "Le prénom est obligatoire")]
    [MaxLength(100)]
    public string Prenom { get; set; } = string.Empty;

    /// <summary>
    /// Langue préférée de l'utilisateur (fr, de, en)
    /// Par défaut : français
    /// </summary>
    [MaxLength(5)]
    public string PreferenceLangue { get; set; } = "fr";

    /// <summary>
    /// Nom complet calculé automatiquement
    /// Exemple : "Jean Dupont"
    /// </summary>
    public string NomComplet => $"{Prenom} {Nom}";

    /// <summary>
    /// Initiales pour l'avatar
    /// Exemple : "JD"
    /// </summary>
    public string Initiales => $"{(Prenom.Length > 0 ? Prenom[0] : ' ')}{(Nom.Length > 0 ? Nom[0] : ' ')}";
}
```

#### 2️⃣ **Les rôles** (Qui peut faire quoi ?)

**Fichier : `Constants/RoleConstants.cs`**

```csharp
/// <summary>
/// Définit les rôles disponibles dans l'application.
/// Utilisé pour les autorisations (qui peut accéder à quoi).
/// </summary>
public static class RoleConstants
{
    /// <summary>
    /// Administrateur : accès total à l'application
    /// Peut gérer les utilisateurs, la configuration, etc.
    /// </summary>
    public const string Administrateur = "Administrateur";

    /// <summary>
    /// Moniteur : peut valider les réservations
    /// Un moniteur DOIT être présent pour qu'une séance soit confirmée
    /// </summary>
    public const string Moniteur = "Moniteur";

    /// <summary>
    /// Membre : utilisateur de base
    /// Peut s'inscrire aux créneaux de tir
    /// </summary>
    public const string Membre = "Membre";

    /// <summary>
    /// Liste de tous les rôles disponibles
    /// Utile pour les boucles et les formulaires
    /// </summary>
    public static readonly string[] TousLesRoles = { Administrateur, Moniteur, Membre };
}
```

#### 3️⃣ **Le service utilisateur** (Comment manipuler les utilisateurs ?)

**Fichier : `Services/UserService.cs`**

```csharp
/// <summary>
/// Service qui gère TOUT ce qui concerne les utilisateurs.
/// Centralise toute la logique métier pour éviter de la dupliquer dans les pages.
/// </summary>
public class UserService
{
    // Dépendances injectées automatiquement par .NET
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public UserService(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    /// <summary>
    /// Récupère TOUS les utilisateurs avec leurs rôles.
    /// Utilisé pour afficher la liste dans l'interface admin.
    /// </summary>
    public async Task<List<UserDto>> GetAllUsersAsync()
    {
        // 1. Récupérer tous les utilisateurs de la base
        var users = _userManager.Users.ToList();

        var userDtos = new List<UserDto>();

        // 2. Pour chaque utilisateur, récupérer ses rôles
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);

            // 3. Créer un objet simplifié (DTO) pour l'affichage
            userDtos.Add(new UserDto
            {
                Id = user.Id,
                Nom = user.Nom,
                Prenom = user.Prenom,
                Email = user.Email,
                NomComplet = user.NomComplet,
                Initiales = user.Initiales,
                Roles = roles.ToList()
            });
        }

        return userDtos;
    }

    /// <summary>
    /// Récupère UN utilisateur par son ID.
    /// Utilisé quand on clique sur "Modifier" un utilisateur.
    /// </summary>
    public async Task<UserDto?> GetUserByIdAsync(string userId)
    {
        // Chercher l'utilisateur
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return null;

        // Récupérer ses rôles
        var roles = await _userManager.GetRolesAsync(user);

        // Retourner un objet simplifié
        return new UserDto
        {
            Id = user.Id,
            Nom = user.Nom,
            Prenom = user.Prenom,
            Email = user.Email,
            NomComplet = user.NomComplet,
            Initiales = user.Initiales,
            Roles = roles.ToList()
        };
    }

    /// <summary>
    /// Crée un nouvel utilisateur avec son mot de passe et ses rôles.
    /// Utilisé dans le formulaire "Nouvel utilisateur".
    /// </summary>
    public async Task<(bool Success, string Message)> CreateUserAsync(
        string nom,
        string prenom,
        string email,
        string password,
        List<string> roles)
    {
        // 1. Créer l'objet utilisateur
        var user = new ApplicationUser
        {
            UserName = email,  // On utilise l'email comme nom d'utilisateur
            Email = email,
            Nom = nom,
            Prenom = prenom,
            EmailConfirmed = true  // On confirme l'email automatiquement
        };

        // 2. Créer l'utilisateur avec son mot de passe
        var result = await _userManager.CreateAsync(user, password);

        if (!result.Succeeded)
        {
            // Si échec, retourner les erreurs
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return (false, $"Erreur lors de la création : {errors}");
        }

        // 3. Assigner les rôles
        if (roles.Any())
        {
            await _userManager.AddToRolesAsync(user, roles);
        }

        return (true, "Utilisateur créé avec succès !");
    }

    /// <summary>
    /// Met à jour les informations d'un utilisateur existant.
    /// Utilisé dans le formulaire "Modifier utilisateur".
    /// </summary>
    public async Task<(bool Success, string Message)> UpdateUserAsync(
        string userId,
        string nom,
        string prenom,
        string email,
        List<string> newRoles)
    {
        // 1. Trouver l'utilisateur
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return (false, "Utilisateur introuvable");

        // 2. Mettre à jour les propriétés
        user.Nom = nom;
        user.Prenom = prenom;
        user.Email = email;
        user.UserName = email;

        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return (false, $"Erreur lors de la mise à jour : {errors}");
        }

        // 3. Mettre à jour les rôles
        var currentRoles = await _userManager.GetRolesAsync(user);

        // Retirer les anciens rôles
        await _userManager.RemoveFromRolesAsync(user, currentRoles);

        // Ajouter les nouveaux rôles
        if (newRoles.Any())
        {
            await _userManager.AddToRolesAsync(user, newRoles);
        }

        return (true, "Utilisateur modifié avec succès !");
    }

    /// <summary>
    /// Supprime un utilisateur.
    /// ATTENTION : suppression définitive !
    /// Alternative recommandée : ajouter un flag "IsDeleted" pour une suppression logique.
    /// </summary>
    public async Task<(bool Success, string Message)> DeleteUserAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return (false, "Utilisateur introuvable");

        var result = await _userManager.DeleteAsync(user);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return (false, $"Erreur lors de la suppression : {errors}");
        }

        return (true, "Utilisateur supprimé avec succès !");
    }

    /// <summary>
    /// Récupère tous les utilisateurs qui ont un rôle spécifique.
    /// Exemple : tous les moniteurs.
    /// </summary>
    public async Task<List<UserDto>> GetUsersInRoleAsync(string roleName)
    {
        var usersInRole = await _userManager.GetUsersInRoleAsync(roleName);

        return usersInRole.Select(user => new UserDto
        {
            Id = user.Id,
            Nom = user.Nom,
            Prenom = user.Prenom,
            Email = user.Email,
            NomComplet = user.NomComplet,
            Initiales = user.Initiales,
            Roles = new List<string> { roleName }
        }).ToList();
    }
}
```

#### 4️⃣ **DTO (Data Transfer Object)** (Version simplifiée pour affichage)

**Fichier : `DTOs/UserDto.cs`**

```csharp
/// <summary>
/// Version SIMPLIFIÉE d'un utilisateur pour l'affichage.
/// On ne montre pas tout (pas de mot de passe, etc.)
/// Utilisé dans les listes, formulaires, etc.
/// </summary>
public class UserDto
{
    /// <summary>
    /// Identifiant unique de l'utilisateur (GUID)
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Nom de famille
    /// </summary>
    [Required(ErrorMessage = "Le nom est obligatoire")]
    [MaxLength(100, ErrorMessage = "Le nom ne peut pas dépasser 100 caractères")]
    public string Nom { get; set; } = string.Empty;

    /// <summary>
    /// Prénom
    /// </summary>
    [Required(ErrorMessage = "Le prénom est obligatoire")]
    [MaxLength(100, ErrorMessage = "Le prénom ne peut pas dépasser 100 caractères")]
    public string Prenom { get; set; } = string.Empty;

    /// <summary>
    /// Adresse email (sert aussi de nom d'utilisateur)
    /// </summary>
    [Required(ErrorMessage = "L'email est obligatoire")]
    [EmailAddress(ErrorMessage = "Format d'email invalide")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Nom complet calculé (ex: "Jean Dupont")
    /// </summary>
    public string NomComplet { get; set; } = string.Empty;

    /// <summary>
    /// Initiales pour avatar (ex: "JD")
    /// </summary>
    public string Initiales { get; set; } = string.Empty;

    /// <summary>
    /// Liste des rôles de l'utilisateur
    /// Peut contenir : "Administrateur", "Moniteur", "Membre"
    /// </summary>
    public List<string> Roles { get; set; } = new();

    /// <summary>
    /// Mot de passe (uniquement pour création)
    /// </summary>
    [Required(ErrorMessage = "Le mot de passe est obligatoire")]
    [MinLength(6, ErrorMessage = "Le mot de passe doit contenir au moins 6 caractères")]
    public string? Password { get; set; }

    /// <summary>
    /// Confirmation du mot de passe
    /// </summary>
    [Compare(nameof(Password), ErrorMessage = "Les mots de passe ne correspondent pas")]
    public string? ConfirmPassword { get; set; }
}
```

#### 5️⃣ **La page de liste des utilisateurs** (Interface admin)

**Fichier : `Components/Pages/Admin/Utilisateurs/Liste.razor`**

```razor
@page "/admin/utilisateurs"
@* Cette page est UNIQUEMENT accessible aux Administrateurs *@
@attribute [Authorize(Policy = "AdminOnly")]
@inject UserService UserService
@inject IDialogService DialogService
@inject ISnackbar Snackbar

<PageTitle>Gestion des utilisateurs</PageTitle>

<MudContainer MaxWidth="MaxWidth.ExtraLarge" Class="mt-4">

    @* TITRE DE LA PAGE *@
    <MudText Typo="Typo.h4" Class="mb-4">
        <MudIcon Icon="@Icons.Material.Filled.People" Class="mr-2" />
        Gestion des utilisateurs
    </MudText>

    @* BARRE D'ACTIONS *@
    <MudPaper Class="pa-4 mb-4">
        <MudGrid>
            <MudItem xs="12" sm="6">
                @* BARRE DE RECHERCHE *@
                <MudTextField @bind-Value="searchString"
                              Placeholder="Rechercher par nom ou email..."
                              Adornment="Adornment.Start"
                              AdornmentIcon="@Icons.Material.Filled.Search"
                              IconSize="Size.Medium"
                              Immediate="true"
                              OnDebounceInterval="300"
                              DebounceInterval="300" />
            </MudItem>

            <MudItem xs="12" sm="3">
                @* FILTRE PAR RÔLE *@
                <MudSelect T="string"
                           @bind-Value="selectedRole"
                           Label="Filtrer par rôle"
                           Clearable="true">
                    <MudSelectItem Value="@string.Empty">Tous les rôles</MudSelectItem>
                    <MudSelectItem Value="@RoleConstants.Administrateur">Administrateur</MudSelectItem>
                    <MudSelectItem Value="@RoleConstants.Moniteur">Moniteur</MudSelectItem>
                    <MudSelectItem Value="@RoleConstants.Membre">Membre</MudSelectItem>
                </MudSelect>
            </MudItem>

            <MudItem xs="12" sm="3" Class="d-flex align-end justify-end">
                @* BOUTON NOUVEL UTILISATEUR *@
                <MudButton Variant="Variant.Filled"
                           Color="Color.Primary"
                           StartIcon="@Icons.Material.Filled.PersonAdd"
                           OnClick="OpenCreateDialog">
                    Nouvel utilisateur
                </MudButton>
            </MudItem>
        </MudGrid>
    </MudPaper>

    @* TABLEAU DES UTILISATEURS *@
    <MudDataGrid Items="@FilteredUsers"
                 Loading="@loading"
                 Dense="true"
                 Hover="true"
                 Striped="true">

        @* COLONNE : AVATAR *@
        <Columns>
            <PropertyColumn Property="x => x.Initiales" Title="Avatar">
                <CellTemplate>
                    <MudAvatar Color="Color.Primary" Size="Size.Small">
                        @context.Item.Initiales
                    </MudAvatar>
                </CellTemplate>
            </PropertyColumn>

            @* COLONNE : NOM *@
            <PropertyColumn Property="x => x.Nom" Title="Nom" />

            @* COLONNE : PRÉNOM *@
            <PropertyColumn Property="x => x.Prenom" Title="Prénom" />

            @* COLONNE : EMAIL *@
            <PropertyColumn Property="x => x.Email" Title="Email" />

            @* COLONNE : RÔLES *@
            <PropertyColumn Property="x => x.Roles" Title="Rôles">
                <CellTemplate>
                    @foreach (var role in context.Item.Roles)
                    {
                        <MudChip Size="Size.Small" Color="GetRoleColor(role)">
                            @role
                        </MudChip>
                    }
                </CellTemplate>
            </PropertyColumn>

            @* COLONNE : ACTIONS *@
            <TemplateColumn Title="Actions" Sortable="false">
                <CellTemplate>
                    <MudIconButton Icon="@Icons.Material.Filled.Edit"
                                   Size="Size.Small"
                                   Color="Color.Primary"
                                   OnClick="@(() => OpenEditDialog(context.Item))"
                                   Title="Modifier" />

                    <MudIconButton Icon="@Icons.Material.Filled.Delete"
                                   Size="Size.Small"
                                   Color="Color.Error"
                                   OnClick="@(() => DeleteUser(context.Item))"
                                   Title="Supprimer" />
                </CellTemplate>
            </TemplateColumn>
        </Columns>
    </MudDataGrid>

</MudContainer>

@code {
    // VARIABLES D'ÉTAT
    private List<UserDto> allUsers = new();           // Tous les utilisateurs
    private string searchString = string.Empty;        // Texte de recherche
    private string selectedRole = string.Empty;        // Rôle sélectionné pour filtre
    private bool loading = true;                       // Indicateur de chargement

    /// <summary>
    /// Liste FILTRÉE des utilisateurs (selon recherche et rôle)
    /// </summary>
    private IEnumerable<UserDto> FilteredUsers
    {
        get
        {
            var filtered = allUsers.AsEnumerable();

            // Filtre par texte de recherche (nom, prénom ou email)
            if (!string.IsNullOrWhiteSpace(searchString))
            {
                filtered = filtered.Where(u =>
                    u.Nom.Contains(searchString, StringComparison.OrdinalIgnoreCase) ||
                    u.Prenom.Contains(searchString, StringComparison.OrdinalIgnoreCase) ||
                    u.Email.Contains(searchString, StringComparison.OrdinalIgnoreCase));
            }

            // Filtre par rôle
            if (!string.IsNullOrWhiteSpace(selectedRole))
            {
                filtered = filtered.Where(u => u.Roles.Contains(selectedRole));
            }

            return filtered;
        }
    }

    /// <summary>
    /// Au chargement de la page, récupérer tous les utilisateurs
    /// </summary>
    protected override async Task OnInitializedAsync()
    {
        await LoadUsers();
    }

    /// <summary>
    /// Charge (ou recharge) la liste des utilisateurs depuis la base de données
    /// </summary>
    private async Task LoadUsers()
    {
        loading = true;
        allUsers = await UserService.GetAllUsersAsync();
        loading = false;
    }

    /// <summary>
    /// Ouvre la boîte de dialogue pour CRÉER un nouvel utilisateur
    /// </summary>
    private async Task OpenCreateDialog()
    {
        var dialog = await DialogService.ShowAsync<CreateUserDialog>("Nouvel utilisateur");
        var result = await dialog.Result;

        // Si l'utilisateur a confirmé (et non annulé)
        if (!result.Cancelled)
        {
            await LoadUsers(); // Recharger la liste
            Snackbar.Add("Utilisateur créé avec succès !", Severity.Success);
        }
    }

    /// <summary>
    /// Ouvre la boîte de dialogue pour MODIFIER un utilisateur existant
    /// </summary>
    private async Task OpenEditDialog(UserDto user)
    {
        var parameters = new DialogParameters { ["User"] = user };
        var dialog = await DialogService.ShowAsync<EditUserDialog>("Modifier l'utilisateur", parameters);
        var result = await dialog.Result;

        if (!result.Cancelled)
        {
            await LoadUsers();
            Snackbar.Add("Utilisateur modifié avec succès !", Severity.Success);
        }
    }

    /// <summary>
    /// Supprime un utilisateur après confirmation
    /// </summary>
    private async Task DeleteUser(UserDto user)
    {
        // Demander confirmation
        bool? confirm = await DialogService.ShowMessageBox(
            "Confirmer la suppression",
            $"Êtes-vous sûr de vouloir supprimer {user.NomComplet} ?",
            yesText: "Supprimer",
            cancelText: "Annuler");

        if (confirm == true)
        {
            var (success, message) = await UserService.DeleteUserAsync(user.Id);

            if (success)
            {
                await LoadUsers();
                Snackbar.Add(message, Severity.Success);
            }
            else
            {
                Snackbar.Add(message, Severity.Error);
            }
        }
    }

    /// <summary>
    /// Retourne la couleur du chip selon le rôle
    /// </summary>
    private Color GetRoleColor(string role)
    {
        return role switch
        {
            RoleConstants.Administrateur => Color.Error,      // Rouge pour admin
            RoleConstants.Moniteur => Color.Warning,          // Orange pour moniteur
            RoleConstants.Membre => Color.Info,               // Bleu pour membre
            _ => Color.Default
        };
    }
}
```

---

## 📈 ÉTAT ACTUEL DU PROJET (Dernière mise à jour: 16 octobre 2025)

### ✅ Phase 1 - TERMINÉE

**Fonctionnalités implémentées :**
- ✅ Gestion complète des utilisateurs (CRUD)
- ✅ Interface admin avec MudBlazor DataGrid
- ✅ Dialogues de création et modification d'utilisateurs
- ✅ Système de filtrage et recherche
- ✅ Gestion des rôles (Administrateur, Moniteur, Membre)
- ✅ Sécurité et autorisations
- ✅ Navigation et menu adaptatif selon les rôles
- ✅ Page Mon Profil pour tous les utilisateurs

**Améliorations UI récentes :**
- ✅ Ajout du sélecteur de langue (FR/DE/EN) dans le header
- ✅ Switch thème clair/foncé avec persistance localStorage
- ✅ Navbar avec fond gris clair uni (#e8e8e8)
- ✅ Suppression du lien "About" remplacé par les contrôles thème/langue
- ✅ Page Mon Profil avec 3 sections : Infos personnelles, Rôles, Mot de passe

**Fichiers créés/modifiés :**
- `Components/Layout/MainLayout.razor` - Ajout switch thème et sélecteur langue
- `Components/Layout/MainLayout.razor.css` - Style navbar gris clair
- `Components/Layout/NavMenu.razor` - Menu avec lien Mon Profil
- `Components/Pages/Admin/Utilisateurs.razor` - Page de gestion utilisateurs
- `Components/Pages/MonProfil.razor` - Page de profil utilisateur
- `Components/Dialogs/CreateUserDialog.razor` - Dialogue création utilisateur
- `Components/Dialogs/EditUserDialog.razor` - Dialogue modification utilisateur
- `Services/UserService.cs` - Service métier utilisateurs (avec ChangePasswordAsync)
- `DTOs/UserDto.cs` - Objets de transfert (UserDto, CreateUserDto, UpdateUserDto, ChangePasswordDto)
- `Constants/RoleConstants.cs` - Constantes des rôles
- `Data/DbInitializer.cs` - Initialisation données de test

---

## ✅ CHECKLIST PHASE 1

### Étape 1 : Initialisation (15 min)
- [x] Créer nouveau projet Blazor Web App avec Identity
- [x] Mode Interactive Server configuré
- [x] Installer package NuGet `MudBlazor`
- [x] Configurer MudBlazor dans `Program.cs`
- [x] Configurer SQLite dans `appsettings.json`
- [x] Créer structure de dossiers
- [x] Init Git + premier commit

### Étape 2 : Modèle utilisateur (30 min)
- [x] Créer `ApplicationUser.cs` avec Nom, Prénom, PreferenceLangue
- [x] Créer `RoleConstants.cs` avec les 3 rôles
- [x] Créer `DbInitializer.cs` pour seed data
- [x] Modifier `ApplicationDbContext.cs`
- [x] Créer migration `dotnet ef migrations add InitialCreate`
- [x] Appliquer migration `dotnet ef database update`
- [x] Vérifier données de test en base (admin + moniteurs + membres)

### Étape 3 : Service utilisateur (1h)
- [x] Créer `DTOs/UserDto.cs` avec validations
- [x] Créer `Services/UserService.cs`
- [x] Implémenter `GetAllUsersAsync()`
- [x] Implémenter `GetUserByIdAsync()`
- [x] Implémenter `CreateUserAsync()`
- [x] Implémenter `UpdateUserAsync()`
- [x] Implémenter `DeleteUserAsync()`
- [x] Implémenter `GetUsersInRoleAsync()`
- [x] Enregistrer service dans `Program.cs` avec `builder.Services.AddScoped<UserService>()`

### Étape 4 : Interface admin (2h30)
- [x] Créer page `Components/Pages/Admin/Utilisateurs.razor`
- [x] MudDataGrid avec colonnes (Avatar, Nom, Prénom, Email, Rôles, Actions)
- [x] Barre de recherche fonctionnelle
- [x] Filtre par rôle fonctionnel
- [x] Créer composant `Components/Dialogs/CreateUserDialog.razor`
- [x] Formulaire création avec validation
- [x] Sélection multi-rôles avec checkboxes
- [x] Créer composant `Components/Dialogs/EditUserDialog.razor`
- [x] Formulaire modification avec validation
- [x] Gestion rôles (ajout/retrait)
- [x] Dialogue de confirmation suppression
- [x] MudSnackbar pour notifications (succès/erreur)

### Étape 5 : Sécurité (30 min)
- [x] Créer policy `AdminOnly` dans `Program.cs`
- [x] Ajouter `@attribute [Authorize(Roles = "Administrateur")]` sur page Utilisateurs
- [x] Tester qu'un Membre ne peut PAS accéder à `/admin/utilisateurs`
- [x] Tester qu'un Admin PEUT accéder
- [x] Validation côté serveur dans tous les services

### Étape 6 : Navigation et UI (45 min)
- [x] Modifier `NavMenu.razor` : ajouter section Admin (visible Admin only)
- [x] Item menu "Gestion utilisateurs" → `/admin/utilisateurs`
- [x] Ajout sélecteur de langue dans le header (FR/DE/EN)
- [x] Ajout switch thème clair/foncé avec persistance
- [x] Navbar avec fond gris clair uni
- [x] Créer page `Components/Pages/MonProfil.razor`
- [x] Formulaire modification nom, prénom, email, téléphone, langue
- [x] Section changement de mot de passe avec validation
- [x] Affichage rôles actuels (lecture seule avec chips colorés)

### Étape 7 : Tests (30 min)
- [x] Tester création utilisateur Membre
- [x] Tester création utilisateur avec plusieurs rôles
- [x] Tester modification nom/prénom
- [x] Tester modification rôles
- [x] Tester suppression utilisateur
- [x] Tester recherche par nom
- [x] Tester filtre par rôle
- [x] Tester responsive (desktop, tablette, mobile)
- [x] Tester validation formulaires (champs requis, email, password)
- [x] Tester restrictions accès (Membre → Admin page)
- [x] Commits Git réguliers effectués

### 🎯 Prochaines tâches
- [x] Créer la page MonProfil pour que les utilisateurs puissent modifier leurs propres informations
- [ ] Implémenter la traduction multilingue (Phase 2)
- [ ] Tester le changement de langue dans toute l'application
- [ ] Phase 2 : Traduction FR/DE/EN
- [ ] Phase 3 : Gestion alvéoles + fermetures
- [ ] Phase 4 : Planning + réservations

---

## 📚 DOCUMENTATION POUR DÉBUTANT

### 🔍 Concepts clés expliqués simplement

#### 1. **Qu'est-ce qu'un Service ?**
Un service, c'est comme une "boîte à outils" qui contient toutes les fonctions pour manipuler quelque chose (ici, les utilisateurs).

**Sans service** (❌ mauvais) :
```csharp
// Dans chaque page, on répète le même code
var user = dbContext.Users.Find(id);
user.Nom = "Nouveau nom";
dbContext.SaveChanges();
```

**Avec service** (✅ bon) :
```csharp
// Dans n'importe quelle page, on appelle simplement
await UserService.UpdateUserAsync(id, nom, prenom, email, roles);
// Le service s'occupe de TOUT !
```

**Avantages** :
- Code réutilisable (pas de copier-coller)
- Facile à tester
- Si on change quelque chose, on le change à UN SEUL endroit

#### 2. **Qu'est-ce qu'un DTO ?**
DTO = Data Transfer Object = "Objet pour transférer des données"

C'est une **version simplifiée** d'un objet pour l'affichage ou les formulaires.

**Exemple** :
```csharp
// ApplicationUser (modèle complet de la base de données)
public class ApplicationUser
{
    public string Id { get; set; }
    public string UserName { get; set; }
    public string PasswordHash { get; set; }  // ❌ On ne veut PAS montrer ça !
    public string SecurityStamp { get; set; } // ❌ Ni ça !
    public string Nom { get; set; }
    public string Prenom { get; set; }
    // + 20 autres propriétés...
}

// UserDto (version simplifiée pour l'affichage)
public class UserDto
{
    public string Id { get; set; }
    public string Nom { get; set; }
    public string Prenom { get; set; }
    public string Email { get; set; }
    public List<string> Roles { get; set; }
    // Uniquement ce qui nous intéresse !
}
```

#### 3. **Qu'est-ce qu'une Policy (Stratégie) ?**
Une policy, c'est une **règle d'autorisation**.

```csharp
// Dans Program.cs, on définit les règles
builder.Services.AddAuthorization(options =>
{
    // Règle : "AdminOnly" = doit avoir le rôle "Administrateur"
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole(RoleConstants.Administrateur));

    // Règle : "MoniteurOrAdmin" = doit avoir Moniteur OU Administrateur
    options.AddPolicy("MoniteurOrAdmin", policy =>
        policy.RequireRole(RoleConstants.Moniteur, RoleConstants.Administrateur));
});
```

```razor
@* Dans une page, on applique la règle *@
@attribute [Authorize(Policy = "AdminOnly")]

@* Cette page ne sera accessible QUE aux Administrateurs *@
```

#### 4. **Lifecycle Blazor : OnInitializedAsync**
C'est une méthode **spéciale** qui s'exécute automatiquement quand une page se charge.

```csharp
protected override async Task OnInitializedAsync()
{
    // Ce code s'exécute AUTOMATIQUEMENT au chargement de la page
    await LoadUsers(); // On charge les utilisateurs
}
```

**Autre méthode utile** :
```csharp
protected override async Task OnAfterRenderAsync(bool firstRender)
{
    // S'exécute APRÈS que la page soit affichée
    if (firstRender) // Uniquement la première fois
    {
        // Code qui doit s'exécuter UNE SEULE fois
    }
}
```

#### 5. **Injection de dépendances**
C'est un mécanisme **automatique** qui fournit les services aux pages.

**Configuration** (dans `Program.cs`) :
```csharp
// On enregistre le service
builder.Services.AddScoped<UserService>();
```

**Utilisation** (dans une page `.razor`) :
```razor
@inject UserService UserService
@* .NET va AUTOMATIQUEMENT créer une instance de UserService *@

@code {
    // On peut maintenant utiliser UserService
    await UserService.GetAllUsersAsync();
}
```

**Types de durée de vie** :
- `AddScoped` : Une instance PAR requête (recommandé pour les services métier)
- `AddSingleton` : UNE SEULE instance pour toute l'application
- `AddTransient` : Une nouvelle instance à CHAQUE utilisation

---

## 🎨 CONVENTIONS DE CODE

Pour un code **facile à lire et maintenir**, suivez ces conventions :

### Nommage
```csharp
// Classes : PascalCase (première lettre de chaque mot en majuscule)
public class UserService { }

// Méthodes : PascalCase
public async Task GetAllUsersAsync() { }

// Variables privées : _camelCase (underscore + première lettre minuscule)
private readonly UserManager<ApplicationUser> _userManager;

// Variables locales : camelCase (première lettre minuscule)
var userName = "admin";

// Constantes : PascalCase
public const string Administrateur = "Administrateur";
```

### Commentaires
```csharp
/// <summary>
/// Documentation XML : explique CE QUE fait la méthode
/// Visible dans IntelliSense (auto-complétion)
/// </summary>
/// <param name="userId">Explique chaque paramètre</param>
/// <returns>Explique ce qui est retourné</returns>
public async Task<UserDto?> GetUserByIdAsync(string userId)
{
    // Commentaire simple : explique POURQUOI on fait quelque chose
    // Utilisé pour clarifier une logique complexe

    var user = await _userManager.FindByIdAsync(userId);

    return user; // Commentaire de fin de ligne (rare, seulement si nécessaire)
}
```

### Organisation du code
```csharp
public class UserService
{
    // 1. CHAMPS PRIVÉS (variables de la classe)
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<UserService> _logger;

    // 2. CONSTRUCTEUR (initialisation)
    public UserService(
        UserManager<ApplicationUser> userManager,
        ILogger<UserService> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    // 3. MÉTHODES PUBLIQUES (dans l'ordre logique d'utilisation)
    public async Task<List<UserDto>> GetAllUsersAsync() { }
    public async Task<UserDto?> GetUserByIdAsync(string userId) { }
    public async Task CreateUserAsync() { }
    public async Task UpdateUserAsync() { }
    public async Task DeleteUserAsync() { }

    // 4. MÉTHODES PRIVÉES (helpers)
    private string GenerateRandomPassword() { }
}
```

---

## 🚀 COMMANDES UTILES

### Entity Framework (Base de données)
```bash
# Créer une nouvelle migration (après avoir modifié les modèles)
dotnet ef migrations add NomDeLaMigration

# Appliquer les migrations à la base de données
dotnet ef database update

# Voir la liste des migrations
dotnet ef migrations list

# Annuler la dernière migration (attention, perte de données possible !)
dotnet ef migrations remove

# Supprimer TOUTE la base de données (recommencer de zéro)
dotnet ef database drop
```

### Lancement de l'application
```bash
# Lancer l'application (mode développement)
dotnet run

# Lancer avec rechargement automatique (Hot Reload)
dotnet watch

# Compiler sans lancer
dotnet build

# Nettoyer les fichiers compilés
dotnet clean
```

### NuGet (packages)
```bash
# Installer un package
dotnet add package NomDuPackage

# Exemple : installer MudBlazor
dotnet add package MudBlazor

# Lister les packages installés
dotnet list package

# Mettre à jour un package
dotnet add package NomDuPackage --version X.Y.Z
```

### Git
```bash
# Voir l'état actuel (fichiers modifiés)
git status

# Ajouter TOUS les fichiers modifiés
git add .

# Créer un commit
git commit -m "Message descriptif de ce qui a été fait"

# Voir l'historique des commits
git log --oneline

# Créer une nouvelle branche
git checkout -b nom-branche

# Revenir sur la branche principale
git checkout master
```

---

## 📊 PROCHAINES PHASES (Résumé)

| Phase | Objectif | Durée estimée |
|-------|----------|---------------|
| **Phase 2** | Traduction FR/DE/EN | 2-3h |
| **Phase 3** | Gestion alvéoles + fermetures | 4-5h |
| **Phase 4** | Planning + réservations | 8-10h |
| **Phase 5** | Notifications Email | 3-4h |
| **Phase 6** | Configuration horaires | 2-3h |
| **Phase 7** | Météo + optimisations | 2-3h |
| **Phase 8** | Docker + déploiement | 3-4h |

**Total projet complet** : ~30-40h de développement

---

## 💡 CONSEILS POUR DÉBUTANT

### 1. **Testez souvent**
Ne codez pas pendant 3 heures d'affilée. Après chaque petite modification, testez !

```
✅ Bon : Créer une méthode → Tester → Créer la suivante → Tester
❌ Mauvais : Créer 10 méthodes → Tester → 50 erreurs !
```

### 2. **Utilisez le débogueur**
Dans Visual Studio / VS Code, mettez des **points d'arrêt** (breakpoints) :
- Cliquez dans la marge gauche à côté du numéro de ligne
- Lancez en mode Debug (F5)
- L'exécution s'arrête → vous pouvez inspecter les variables

### 3. **Lisez les messages d'erreur**
Les erreurs sont **vos amis** ! Elles vous disent exactement ce qui ne va pas.

```
Erreur : "Object reference not set to an instance of an object"
Traduction : Vous essayez d'utiliser une variable qui est null

Solution : Vérifier avec if (variable != null) avant
```

### 4. **Copiez intelligemment**
Si vous copiez du code existant :
1. **Lisez-le** ligne par ligne pour comprendre
2. **Adaptez-le** à votre besoin (ne copiez pas bêtement)
3. **Testez-le** pour vérifier qu'il marche

### 5. **Documentez au fur et à mesure**
Quand vous écrivez une méthode compliquée, mettez un commentaire **immédiatement**.

Dans 2 semaines, vous aurez oublié ce que vous avez fait !

### 6. **Committez régulièrement**
Faites des commits Git après chaque fonctionnalité qui marche.

```bash
# Exemple de commits réguliers
git commit -m "✅ Ajout du modèle ApplicationUser"
git commit -m "✅ Création du UserService"
git commit -m "✅ Page liste utilisateurs fonctionnelle"
git commit -m "✅ Formulaire création utilisateur"
```

Si vous cassez quelque chose, vous pourrez revenir en arrière !

---

## 📖 RESSOURCES UTILES

### Documentation officielle
- **Blazor** : https://learn.microsoft.com/fr-fr/aspnet/core/blazor/
- **MudBlazor** : https://mudblazor.com/
- **Entity Framework** : https://learn.microsoft.com/fr-fr/ef/core/

### Tutoriels recommandés
- Blazor pour débutants : https://dotnet.microsoft.com/learn/aspnet/blazor-tutorial/intro
- MudBlazor exemples : https://mudblazor.com/components/list

### Communauté
- **Discord MudBlazor** : Support réactif
- **Stack Overflow** : Recherchez vos erreurs (99% déjà résolues !)

---

## 📝 NOTES IMPORTANTES

### À faire ABSOLUMENT
1. ✅ Commitez AVANT de faire des changements risqués
2. ✅ Testez sur mobile régulièrement (responsive)
3. ✅ Validez TOUJOURS côté serveur (sécurité)
4. ✅ Utilisez des mots de passe forts en production (pas admin/admin !)

### À NE PAS faire
1. ❌ Coder sans comprendre
2. ❌ Ignorer les warnings du compilateur
3. ❌ Mettre des mots de passe en dur dans le code
4. ❌ Faire confiance uniquement à la validation client
5. ❌ Supprimer des migrations déjà appliquées

---

## 🎯 OBJECTIF PHASE 1

À la fin de la Phase 1, vous aurez une application fonctionnelle où :

✅ Un **Administrateur** peut se connecter
✅ Il voit la liste de tous les membres du club
✅ Il peut **créer** un nouveau membre (Membre / Moniteur / Admin)
✅ Il peut **modifier** les infos d'un membre
✅ Il peut **supprimer** un membre
✅ Il peut **chercher** par nom ou email
✅ Il peut **filtrer** par rôle
✅ L'interface est **jolie** (MudBlazor)
✅ L'interface fonctionne sur **mobile**
✅ Tout est **sécurisé** (validations, autorisations)

**C'est la fondation pour les phases suivantes !** 🎉

---

**FIN DU PLAN - PHASE 1**

*Ce fichier sera mis à jour au fur et à mesure de l'avancement du projet.*

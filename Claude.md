# Claude.md

## Project overview

Application **Blazor Web App** (.NET 8) de gestion de réservations d'alvéoles pour club de tir. Architecture 

**Principe :** Une séance de tir doit obligatoirement être validée par la présence d'un moniteur.
- Tout le monde peut voir la présence de tous les membres et moniteurs
- Un ou plusieurs membres peuvent s'inscrire sur une réservation
- Un moniteur peut valider une ou plusieurs inscriptions
- Interface différenciée visuellement selon le statut (confirmée/en attente)

## Fonctionalités

- Affichage d'un planning mensuel avec une interface très conviviale et simple d'utilisation.
- Gestion des membres avec authentification, autorisation et rôles.
- Interface de gestion des membres.
- Gérer la disponibilité des alvéoles. Possibilité de les fermer (travaux, jour férié, réservations extérieures, etc.)
- Possibilité pour un moniteur d'annuler sa présence.
- Sauvegarder la préférence de langue par utilisateur.

## Notifications

- prévoir un système modulaire ou les membres peuvent s'abonner à plusieurs systèmes de notification ( mail, whatsapp, etc.).
- Notifier les membres inscrits à la réservation lorsqu'un moniteur valide sa présence.
- Notifier les membres inscrits à la réservation lorsqu'un moniteur annule sa présence.

## Page de configuration (accessible Administrateurs)

- Réglage des horaires d'ouverture chaque jour de la semaine (Par défaut du lundi au samedi 8h00-12h00 14h00-17h00 et le dimanche 8h00-12h00)

## Development Guidelines

- Demander l'utilisateur (Human in the loop) si il y a des questions sur le fonctionnement.
- L'application doit supporter le multi-language (Français par défaut, allemand, anglais)
- L'application doit être modulaire afin d'y ajouter facilement des fonctionnalités.
- Toujours utiliser des noms de variables descriptives (Always use descriptive variable names)
- Follow existing validation patterns using Data Annotations
- use context7
- génère et met à jour une todo list pour suivre l'avancement du projet
- utiliser github pour suivre le développement
- penser mobile first pour avoir un design compatible smart phone

## Modèles

- role : Administrateur, Membre, Moniteur
- alveole : nom
- membre : nom, prenom, mail, role
- inscription : date, heure début, heure fin, membres inscrits, moniteurs inscrits

## Nice to have

- notification Whatsapp
- Module météo

## Considérations techniques

### Stack Technique
- **Framework :** Blazor Web App (.NET 8) avec modes hybrides (Server/WebAssembly/Static)
- **UI Framework :** MudBlazor 8.13.0 (Material Design)
- **Base de données :** SQLite (dev) → PostgreSQL/SQL Server (prod)
- **ORM :** Entity Framework Core 9.0
- **Localisation :** ASP.NET Core Localization avec fichiers .resx (FR/DE/EN)
- **Thème :** MudBlazor ThemeProvider (dark/light mode automatique)
- **Stockage local :** Blazored.LocalStorage pour préférences utilisateur
- **Architecture :** à définir
- **Déploiement :** Conteneurisé sous Linux (Docker)

### MudBlazor - Architecture des Providers

**⚠️ RÈGLE CRITIQUE : Les providers doivent être placés UNE SEULE FOIS dans MainLayout.razor**

Les 4 providers MudBlazor sont configurés dans `Components/Layout/MainLayout.razor` :

```razor
<MudThemeProvider @bind-IsDarkMode="@_isDarkMode" />
<MudPopoverProvider />
<MudDialogProvider />
<MudSnackbarProvider />
```

**Rôles des providers :**
- **MudThemeProvider** : Gère le thème dark/light mode automatiquement avec CSS
- **MudPopoverProvider** : Requis pour MudSelect, MudMenu, tooltips, etc.
- **MudDialogProvider** : Requis pour MudDialog (utilisé dans EditUser/CreateUser)
- **MudSnackbarProvider** : Pour les notifications toast

**Problèmes à éviter :**
1. ❌ **Ne JAMAIS dupliquer les providers** dans d'autres composants
2. ❌ Éviter `@rendermode InteractiveServer` sur les composants qui utilisent MudBlazor (crée des scopes séparés)
3. ✅ Les providers au niveau MainLayout sont accessibles à toute l'application

**Erreur typique si duplication :**
```
There is already a subscriber to the content with the given section ID 'mud-overlay-to-popover-provider'
```

### Système de localisation multilingue

**Configuration : 3 langues supportées**
- Français (par défaut)
- Deutsch (allemand)
- English (anglais)

**Architecture :**
1. **Fichiers de ressources** : `Resources/*.resx` (Shared, Pages/Home, Pages/NavMenu, Pages/Utilisateurs)
   - Fichiers configurés comme `EmbeddedResource` dans `CTSAR.Booking.csproj`
   - Génère des DLL de ressources : `de/CTSAR.Booking.resources.dll`, `en/CTSAR.Booking.resources.dll`

2. **CultureController** : `Controllers/CultureController.cs`
   - Endpoint `/Culture/Set?culture={fr|de|en}&redirectUri={url}`
   - Définit un cookie avec expiration de 1 an
   - Utilise `CookieRequestCultureProvider`

3. **Changement de langue** :
   - `NavigateTo(cultureUrl, forceLoad: true)` est **OBLIGATOIRE** pour recharger la page
   - Le middleware `UseRequestLocalization()` détecte le cookie au chargement

4. **Utilisation dans les composants** :
```razor
@inject IStringLocalizer<CTSAR.Booking.Resources.Shared> Loc

<h1>@Loc["WelcomeTitle"]</h1>
```

### Système de thème dark/light mode

**Implémentation avec MudBlazor :**
- `MudThemeProvider` avec `@bind-IsDarkMode` gère automatiquement les styles CSS
- Pas besoin de CSS custom pour le mode sombre (MudBlazor l'applique automatiquement)

**Persistance :**
- `ThemeService` sauvegarde la préférence dans `LocalStorage`
- Chargement automatique au démarrage de l'application

**Composant de contrôle :**
- `ThemeLanguageControls.razor` : Bouton avec icône soleil/lune
- Utilise du HTML pur (select) pour le sélecteur de langue (évite les conflits de providers)

### État du Projet

**✅ Phase 1 : Authentification et gestion utilisateurs (terminée)**
- Système ASP.NET Core Identity avec rôles (Administrateur, Membre, Moniteur)
- Page de gestion des utilisateurs avec MudDataGrid
- Dialogues Create/Edit avec validation
- 15 utilisateurs de test créés

**✅ Phase 2 : Localisation multilingue FR/DE/EN (terminée)**
- Configuration complète du système de localisation
- Fichiers de ressources .resx pour toutes les pages principales
- CultureController pour changement de langue via cookies
- Sélecteur de langue avec drapeaux dans le header

**✅ Thème dark/light mode (terminée)**
- MudBlazor ThemeProvider configuré
- Bouton de basculement dans le header
- Persistance de la préférence utilisateur

### Prochaines Étapes

1. **Planning de réservation**
   - Créer les modèles (Alvéole, Inscription, Séance)
   - Interface calendrier mensuel
   - Système de validation par moniteur

2. **Notifications**
   - Architecture modulaire pour différents canaux (email, WhatsApp)
   - Notifications aux membres lors de validation/annulation par moniteur

3. **Configuration système**
   - Page d'administration pour horaires d'ouverture
   - Gestion de la disponibilité des alvéoles

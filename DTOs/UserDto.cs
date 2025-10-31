// ====================================================================
// UserDto.cs : Objet de transfert de données pour afficher un utilisateur
// ====================================================================
// Ce fichier définit les DTOs (Data Transfer Objects) utilisés pour
// transférer les données des utilisateurs entre les différentes couches
// de l'application (Service -> Interface).
//
// POURQUOI DES DTOs ?
// - Sécurité : On ne renvoie jamais le mot de passe ou des infos sensibles
// - Performance : On envoie seulement les données nécessaires
// - Simplicité : Format adapté aux besoins de l'interface

using System.ComponentModel.DataAnnotations;

namespace CTSAR.Booking.DTOs;

/// <summary>
/// DTO utilisé pour AFFICHER un utilisateur dans une liste ou un détail.
/// Contient toutes les informations nécessaires pour l'affichage.
/// N'inclut PAS le mot de passe pour des raisons de sécurité.
/// </summary>
public class UserDto
{
    /// <summary>
    /// Identifiant unique de l'utilisateur (GUID).
    /// Exemple : "a1b2c3d4-e5f6-7890-abcd-ef1234567890"
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Nom de famille de l'utilisateur.
    /// Exemple : "Dupont"
    /// </summary>
    public string Nom { get; set; } = string.Empty;

    /// <summary>
    /// Prénom de l'utilisateur.
    /// Exemple : "Jean"
    /// </summary>
    public string Prenom { get; set; } = string.Empty;

    /// <summary>
    /// Nom complet (Prénom + Nom).
    /// Exemple : "Jean Dupont"
    /// Calculé automatiquement pour faciliter l'affichage.
    /// </summary>
    public string NomComplet => $"{Prenom} {Nom}";

    /// <summary>
    /// Adresse email de l'utilisateur (utilisée aussi comme nom d'utilisateur).
    /// Exemple : "jean.dupont@ctsar.fr"
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Numéro de téléphone (optionnel).
    /// Exemple : "+33 6 12 34 56 78"
    /// Peut être null si non renseigné.
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Langue préférée de l'utilisateur pour l'interface.
    /// Valeurs : "fr", "de", "en"
    /// Par défaut : "fr"
    /// </summary>
    public string PreferenceLangue { get; set; } = "fr";

    /// <summary>
    /// Préférence de notification par email.
    /// </summary>
    public bool NotifMail { get; set; } = true;

    /// <summary>
    /// Préférence de notification via le canal 2 (ex: WhatsApp).
    /// </summary>
    public bool Notif2 { get; set; } = false;

    /// <summary>
    /// Préférence de notification via le canal 3 (réservé pour usage futur).
    /// </summary>
    public bool Notif3 { get; set; } = false;

    /// <summary>
    /// Liste des rôles de l'utilisateur.
    /// Exemple : ["Administrateur"] ou ["Moniteur", "Membre"]
    /// Un utilisateur peut avoir plusieurs rôles.
    /// </summary>
    public List<string> Roles { get; set; } = new();

    /// <summary>
    /// Rôles affichés sous forme de texte pour l'interface.
    /// Exemple : "Administrateur, Moniteur"
    /// Utile pour afficher dans un tableau.
    /// </summary>
    public string RolesText => string.Join(", ", Roles);

    /// <summary>
    /// L'utilisateur est-il un Administrateur ?
    /// Utile pour les contrôles conditionnels dans l'interface.
    /// </summary>
    public bool EstAdministrateur => Roles.Contains("Administrateur");

    /// <summary>
    /// L'utilisateur est-il un Moniteur ?
    /// Utile pour les contrôles conditionnels dans l'interface.
    /// </summary>
    public bool EstMoniteur => Roles.Contains("Moniteur");

    /// <summary>
    /// Compte verrouillé ?
    /// Si true, l'utilisateur ne peut plus se connecter.
    /// </summary>
    public bool LockoutEnabled { get; set; }

    /// <summary>
    /// Date de fin du verrouillage (si compte verrouillé).
    /// Si null, pas de verrouillage.
    /// Si une date future, le compte est verrouillé jusqu'à cette date.
    /// </summary>
    public DateTimeOffset? LockoutEnd { get; set; }

    /// <summary>
    /// Le compte est-il actuellement verrouillé ?
    /// True si LockoutEnd existe et est dans le futur.
    /// </summary>
    public bool EstVerrouille => LockoutEnd.HasValue && LockoutEnd.Value > DateTimeOffset.Now;
}

/// <summary>
/// DTO utilisé pour CRÉER un nouvel utilisateur.
/// Contient tous les champs obligatoires pour la création.
/// Inclut le mot de passe (qui sera crypté par le système).
/// </summary>
public class CreateUserDto
{
    /// <summary>
    /// Nom de famille (obligatoire).
    /// Exemple : "Dupont"
    /// </summary>
    [Required(ErrorMessage = "Le nom est obligatoire")]
    [MaxLength(100, ErrorMessage = "Le nom ne peut pas dépasser 100 caractères")]
    public string Nom { get; set; } = string.Empty;

    /// <summary>
    /// Prénom (obligatoire).
    /// Exemple : "Jean"
    /// </summary>
    [Required(ErrorMessage = "Le prénom est obligatoire")]
    [MaxLength(100, ErrorMessage = "Le prénom ne peut pas dépasser 100 caractères")]
    public string Prenom { get; set; } = string.Empty;

    /// <summary>
    /// Adresse email (obligatoire, doit être valide).
    /// Exemple : "jean.dupont@ctsar.fr"
    /// Sera aussi utilisée comme nom d'utilisateur.
    /// </summary>
    [Required(ErrorMessage = "L'email est obligatoire")]
    [EmailAddress(ErrorMessage = "L'email doit être valide")]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Mot de passe (obligatoire, minimum 6 caractères).
    /// Exemple : "MonMotDePasse123"
    /// Sera crypté avant d'être stocké en base.
    /// </summary>
    [Required(ErrorMessage = "Le mot de passe est obligatoire")]
    [StringLength(100, ErrorMessage = "Le mot de passe doit contenir au moins {2} caractères", MinimumLength = 6)]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Confirmation du mot de passe (doit être identique).
    /// Exemple : "MonMotDePasse123"
    /// Permet d'éviter les erreurs de frappe.
    /// </summary>
    [Required(ErrorMessage = "La confirmation du mot de passe est obligatoire")]
    [Compare("Password", ErrorMessage = "Les mots de passe ne correspondent pas")]
    [DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = string.Empty;

    /// <summary>
    /// Numéro de téléphone (optionnel).
    /// Exemple : "+33 6 12 34 56 78"
    /// </summary>
    [Phone(ErrorMessage = "Le numéro de téléphone n'est pas valide")]
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Langue préférée (par défaut : "fr").
    /// Valeurs possibles : "fr", "de", "en"
    /// </summary>
    [Required]
    [MaxLength(5)]
    public string PreferenceLangue { get; set; } = "fr";

    /// <summary>
    /// Préférence de notification par email.
    /// </summary>
    public bool NotifMail { get; set; } = true;

    /// <summary>
    /// Préférence de notification via le canal 2 (ex: WhatsApp).
    /// </summary>
    public bool Notif2 { get; set; } = false;

    /// <summary>
    /// Préférence de notification via le canal 3 (réservé pour usage futur).
    /// </summary>
    public bool Notif3 { get; set; } = false;

    /// <summary>
    /// Liste des rôles à assigner à l'utilisateur.
    /// Exemple : ["Moniteur"] ou ["Administrateur", "Moniteur"]
    /// Au minimum un rôle doit être sélectionné.
    /// </summary>
    [Required(ErrorMessage = "Au moins un rôle doit être sélectionné")]
    [MinLength(1, ErrorMessage = "Au moins un rôle doit être sélectionné")]
    public List<string> Roles { get; set; } = new();
}

/// <summary>
/// DTO utilisé pour MODIFIER un utilisateur existant.
/// Similaire à CreateUserDto, mais SANS le mot de passe.
/// Le mot de passe se change via une fonction séparée pour plus de sécurité.
/// Inclut l'Id pour identifier l'utilisateur à modifier.
/// </summary>
public class UpdateUserDto
{
    /// <summary>
    /// Identifiant unique de l'utilisateur à modifier.
    /// Exemple : "a1b2c3d4-e5f6-7890-abcd-ef1234567890"
    /// </summary>
    [Required]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Nom de famille (obligatoire).
    /// Exemple : "Dupont"
    /// </summary>
    [Required(ErrorMessage = "Le nom est obligatoire")]
    [MaxLength(100, ErrorMessage = "Le nom ne peut pas dépasser 100 caractères")]
    public string Nom { get; set; } = string.Empty;

    /// <summary>
    /// Prénom (obligatoire).
    /// Exemple : "Jean"
    /// </summary>
    [Required(ErrorMessage = "Le prénom est obligatoire")]
    [MaxLength(100, ErrorMessage = "Le prénom ne peut pas dépasser 100 caractères")]
    public string Prenom { get; set; } = string.Empty;

    /// <summary>
    /// Adresse email (obligatoire, doit être valide).
    /// Exemple : "jean.dupont@ctsar.fr"
    /// </summary>
    [Required(ErrorMessage = "L'email est obligatoire")]
    [EmailAddress(ErrorMessage = "L'email doit être valide")]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Numéro de téléphone (optionnel).
    /// Exemple : "+33 6 12 34 56 78"
    /// </summary>
    [Phone(ErrorMessage = "Le numéro de téléphone n'est pas valide")]
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Langue préférée.
    /// Valeurs possibles : "fr", "de", "en"
    /// </summary>
    [Required]
    [MaxLength(5)]
    public string PreferenceLangue { get; set; } = "fr";

    /// <summary>
    /// Préférence de notification par email.
    /// </summary>
    public bool NotifMail { get; set; } = true;

    /// <summary>
    /// Préférence de notification via le canal 2 (ex: WhatsApp).
    /// </summary>
    public bool Notif2 { get; set; } = false;

    /// <summary>
    /// Préférence de notification via le canal 3 (réservé pour usage futur).
    /// </summary>
    public bool Notif3 { get; set; } = false;

    /// <summary>
    /// Liste des rôles de l'utilisateur.
    /// Exemple : ["Moniteur"] ou ["Administrateur", "Moniteur"]
    /// Au minimum un rôle doit être sélectionné.
    /// </summary>
    [Required(ErrorMessage = "Au moins un rôle doit être sélectionné")]
    [MinLength(1, ErrorMessage = "Au moins un rôle doit être sélectionné")]
    public List<string> Roles { get; set; } = new();
}

/// <summary>
/// DTO utilisé pour CHANGER le mot de passe d'un utilisateur.
/// Séparé de UpdateUserDto pour plus de sécurité.
/// Nécessite l'ancien mot de passe pour confirmer l'identité.
/// </summary>
public class ChangePasswordDto
{
    /// <summary>
    /// Identifiant unique de l'utilisateur.
    /// Exemple : "a1b2c3d4-e5f6-7890-abcd-ef1234567890"
    /// </summary>
    [Required]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Mot de passe actuel (pour vérification).
    /// Exemple : "AncienMotDePasse123"
    /// Obligatoire pour des raisons de sécurité.
    /// </summary>
    [Required(ErrorMessage = "Le mot de passe actuel est obligatoire")]
    [DataType(DataType.Password)]
    public string CurrentPassword { get; set; } = string.Empty;

    /// <summary>
    /// Nouveau mot de passe (minimum 6 caractères).
    /// Exemple : "NouveauMotDePasse123"
    /// </summary>
    [Required(ErrorMessage = "Le nouveau mot de passe est obligatoire")]
    [StringLength(100, ErrorMessage = "Le mot de passe doit contenir au moins {2} caractères", MinimumLength = 6)]
    [DataType(DataType.Password)]
    public string NewPassword { get; set; } = string.Empty;

    /// <summary>
    /// Confirmation du nouveau mot de passe (doit être identique).
    /// Exemple : "NouveauMotDePasse123"
    /// </summary>
    [Required(ErrorMessage = "La confirmation du mot de passe est obligatoire")]
    [Compare("NewPassword", ErrorMessage = "Les mots de passe ne correspondent pas")]
    [DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = string.Empty;
}

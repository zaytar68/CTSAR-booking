// ====================================================================
// ApplicationUser : Modèle de données pour les utilisateurs
// ====================================================================
// Ce fichier définit les propriétés d'un utilisateur de l'application.
// Il hérite de IdentityUser qui fournit déjà : Email, Password, etc.

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace CTSAR.Booking.Data;

/// <summary>
/// Représente un utilisateur de l'application CTSAR Booking.
/// Hérite de IdentityUser qui fournit déjà :
/// - Id (string) : Identifiant unique
/// - UserName (string) : Nom d'utilisateur (on utilise l'email)
/// - Email (string) : Adresse email
/// - PasswordHash (string) : Mot de passe crypté
/// - PhoneNumber (string) : Numéro de téléphone (optionnel)
/// - EmailConfirmed (bool) : Email confirmé ?
/// - etc.
/// </summary>
public class ApplicationUser : IdentityUser
{
    // ================================================================
    // PROPRIÉTÉS AJOUTÉES POUR CTSAR BOOKING
    // ================================================================

    /// <summary>
    /// Nom de famille de l'utilisateur.
    /// Exemple : "Dupont"
    /// Obligatoire, maximum 100 caractères.
    /// </summary>
    [Required(ErrorMessage = "Le nom est obligatoire")]
    [MaxLength(100, ErrorMessage = "Le nom ne peut pas dépasser 100 caractères")]
    public string Nom { get; set; } = string.Empty;

    /// <summary>
    /// Prénom de l'utilisateur.
    /// Exemple : "Jean"
    /// Obligatoire, maximum 100 caractères.
    /// </summary>
    [Required(ErrorMessage = "Le prénom est obligatoire")]
    [MaxLength(100, ErrorMessage = "Le prénom ne peut pas dépasser 100 caractères")]
    public string Prenom { get; set; } = string.Empty;

    /// <summary>
    /// Langue préférée de l'utilisateur pour l'interface.
    /// Valeurs possibles : "fr" (français), "de" (allemand), "en" (anglais)
    /// Par défaut : "fr"
    /// </summary>
    [MaxLength(5)]
    public string PreferenceLangue { get; set; } = "fr";

    // ================================================================
    // PROPRIÉTÉS CALCULÉES (pas stockées en base, calculées à la volée)
    // ================================================================

    /// <summary>
    /// Nom complet de l'utilisateur (Prénom + Nom).
    /// Exemple : "Jean Dupont"
    /// Cette propriété est calculée automatiquement, pas stockée en base.
    /// </summary>
    public string NomComplet => $"{Prenom} {Nom}";

    /// <summary>
    /// Initiales de l'utilisateur pour afficher un avatar.
    /// Exemple : "JD" pour Jean Dupont
    /// Prend la première lettre du prénom et du nom.
    /// </summary>
    public string Initiales
    {
        get
        {
            var premiereLettrePrenom = Prenom.Length > 0 ? Prenom[0] : ' ';
            var premiereLettreNom = Nom.Length > 0 ? Nom[0] : ' ';
            return $"{premiereLettrePrenom}{premiereLettreNom}".Trim();
        }
    }
}


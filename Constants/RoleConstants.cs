// ====================================================================
// RoleConstants : Définit tous les rôles disponibles dans l'application
// ====================================================================
// Ce fichier centralise les noms des rôles pour éviter les erreurs de frappe.
// Au lieu d'écrire "Administrateur" partout, on utilise RoleConstants.Administrateur

namespace CTSAR.Booking.Constants;

/// <summary>
/// Constantes définissant les rôles disponibles dans l'application.
/// Un utilisateur peut avoir un ou plusieurs rôles.
/// Les rôles déterminent les permissions (qui peut faire quoi).
/// </summary>
public static class RoleConstants
{
    // ================================================================
    // DÉFINITION DES RÔLES
    // ================================================================

    /// <summary>
    /// Rôle : Administrateur
    ///
    /// PERMISSIONS :
    /// - Accès total à l'application
    /// - Gestion des utilisateurs (créer, modifier, supprimer)
    /// - Gestion des alvéoles (créer, fermer, etc.)
    /// - Configuration de l'application (horaires, paramètres)
    /// - Peut faire tout ce qu'un Moniteur et un Membre peuvent faire
    /// </summary>
    public const string Administrateur = "Administrateur";

    /// <summary>
    /// Rôle : Moniteur
    ///
    /// PERMISSIONS :
    /// - Valider sa présence sur des créneaux (confirme les réservations)
    /// - Annuler sa présence (annule les réservations)
    /// - S'inscrire aux créneaux (comme un membre)
    /// - Voir toutes les réservations
    ///
    /// IMPORTANT : Une séance de tir DOIT être validée par un Moniteur
    /// pour passer du statut "En attente" à "Confirmée".
    /// </summary>
    public const string Moniteur = "Moniteur";

    /// <summary>
    /// Rôle : Membre
    ///
    /// PERMISSIONS :
    /// - S'inscrire aux créneaux de tir
    /// - Se désinscrire des créneaux
    /// - Voir le planning des réservations
    /// - Voir la présence des moniteurs
    /// - Modifier son profil
    ///
    /// C'est le rôle par défaut pour un nouvel utilisateur.
    /// </summary>
    public const string Membre = "Membre";

    // ================================================================
    // LISTE DE TOUS LES RÔLES (utile pour les boucles et formulaires)
    // ================================================================

    /// <summary>
    /// Tableau contenant tous les rôles disponibles.
    /// Utilisé pour :
    /// - Générer des checkboxes dans les formulaires
    /// - Initialiser les rôles au démarrage de l'app
    /// - Afficher la liste des rôles possibles
    /// </summary>
    public static readonly string[] TousLesRoles =
    {
        Administrateur,
        Moniteur,
        Membre
    };

    // ================================================================
    // MÉTHODES UTILITAIRES (optionnel mais pratique)
    // ================================================================

    /// <summary>
    /// Vérifie si un nom de rôle est valide.
    /// </summary>
    /// <param name="roleName">Nom du rôle à vérifier</param>
    /// <returns>True si le rôle existe, False sinon</returns>
    public static bool EstUnRoleValide(string roleName)
    {
        return TousLesRoles.Contains(roleName);
    }

    /// <summary>
    /// Retourne un nom de rôle plus lisible pour l'affichage.
    /// (Pour l'instant, retourne le même nom, mais pourrait être étendu
    /// pour supporter la traduction multi-langue)
    /// </summary>
    /// <param name="roleName">Nom technique du rôle</param>
    /// <returns>Nom d'affichage du rôle</returns>
    public static string GetNomAffichage(string roleName)
    {
        return roleName switch
        {
            Administrateur => "Administrateur",
            Moniteur => "Moniteur",
            Membre => "Membre",
            _ => roleName  // Si le rôle n'est pas reconnu, retourne tel quel
        };
    }
}

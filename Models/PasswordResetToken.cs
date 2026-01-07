using System.ComponentModel.DataAnnotations;
using CTSAR.Booking.Data;

namespace CTSAR.Booking.Models;

/// <summary>
/// Token temporaire pour la création/réinitialisation de mot de passe
/// </summary>
public class PasswordResetToken
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// ID de l'utilisateur concerné
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Token unique et sécurisé
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Date de création du token
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Date d'expiration du token (24h par défaut)
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Indique si le token a déjà été utilisé
    /// </summary>
    public bool IsUsed { get; set; }

    /// <summary>
    /// Type de token : FirstLogin (première connexion) ou Reset (réinitialisation)
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string TokenType { get; set; } = "FirstLogin";

    // Navigation property
    public User? User { get; set; }
}

// ====================================================================
// CommentaireEntryDto.cs : Représente une entrée dans les commentaires
// ====================================================================

namespace CTSAR.Booking.DTOs;

/// <summary>
/// Représente une entrée parsée dans les commentaires d'une réservation
/// Format attendu : [DD/MM HH:mm - Prénom NOM] Contenu
/// </summary>
public class CommentaireEntryDto
{
    /// <summary>
    /// Horodatage de l'entrée (format "DD/MM HH:mm")
    /// </summary>
    public string Timestamp { get; set; } = string.Empty;

    /// <summary>
    /// Nom complet de l'auteur (Prénom NOM)
    /// </summary>
    public string AuthorName { get; set; } = string.Empty;

    /// <summary>
    /// ID de l'auteur (pour déterminer si c'est un moniteur)
    /// </summary>
    public string AuthorId { get; set; } = string.Empty;

    /// <summary>
    /// Indique si l'auteur est un moniteur
    /// </summary>
    public bool IsMoniteur { get; set; }

    /// <summary>
    /// Contenu du commentaire
    /// </summary>
    public string Content { get; set; } = string.Empty;
}

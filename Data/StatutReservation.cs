// ====================================================================
// StatutReservation.cs : Énumération des statuts de réservation
// ====================================================================

namespace CTSAR.Booking.Data;

/// <summary>
/// Statuts possibles d'une réservation.
/// Le statut est calculé automatiquement selon la présence d'un moniteur.
/// </summary>
public enum StatutReservation
{
    /// <summary>
    /// Aucun moniteur n'est inscrit sur cette réservation.
    /// Affichage : couleur jaune
    /// </summary>
    EnAttente = 0,

    /// <summary>
    /// Au moins un moniteur est inscrit et a confirmé sa présence.
    /// Affichage : couleur verte
    /// </summary>
    Confirmee = 1
}

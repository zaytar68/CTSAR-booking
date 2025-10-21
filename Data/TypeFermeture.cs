// ====================================================================
// TypeFermeture.cs : Énumération des types de fermeture d'alvéole
// ====================================================================

namespace CTSAR.Booking.Data;

/// <summary>
/// Types de fermeture possibles pour une alvéole
/// </summary>
public enum TypeFermeture
{
    /// <summary>
    /// Travaux de maintenance ou réparation
    /// </summary>
    Travaux = 0,

    /// <summary>
    /// Jour férié / vacances
    /// </summary>
    JourFerie = 1,

    /// <summary>
    /// Réservation externe (autre organisation)
    /// </summary>
    ReservationExterne = 2,

    /// <summary>
    /// Autre raison
    /// </summary>
    Autre = 3
}

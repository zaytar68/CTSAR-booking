using System.ComponentModel.DataAnnotations;

namespace CTSAR.Booking.Models;

public class Alveole
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Le nom de l'alvéole est obligatoire")]
    [StringLength(50, ErrorMessage = "Le nom ne peut pas dépasser 50 caractères")]
    public string Nom { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "La description ne peut pas dépasser 500 caractères")]
    public string? Description { get; set; }

    public bool EstActive { get; set; } = true;

    [Range(1, 10, ErrorMessage = "Le nombre de tireurs doit être entre 1 et 10")]
    public int NombreMaxTireurs { get; set; } = 8;

    public DateTime DateCreation { get; set; } = DateTime.Now;

    // Navigation properties
    public virtual ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
    public virtual ICollection<PeriodeFermeture> PeriodeseFermeture { get; set; } = new List<PeriodeFermeture>();

    public bool EstDisponible(DateTime date, TimeSpan heureDebut, TimeSpan heureFin)
    {
        if (!EstActive)
            return false;

        return !PeriodeseFermeture.Any(p =>
            p.DateDebut.Date <= date.Date &&
            date.Date <= p.DateFin.Date);
    }
}

public class PeriodeFermeture
{
    public int Id { get; set; }

    public int AlveoleId { get; set; }
    public virtual Alveole Alveole { get; set; } = null!;

    [Required]
    public DateTime DateDebut { get; set; }

    [Required]
    public DateTime DateFin { get; set; }

    [Required(ErrorMessage = "La raison de fermeture est obligatoire")]
    [StringLength(200, ErrorMessage = "La raison ne peut pas dépasser 200 caractères")]
    public string Raison { get; set; } = string.Empty;

    public DateTime DateCreation { get; set; } = DateTime.Now;
}
using Microsoft.EntityFrameworkCore;
using CTSAR.Booking.Models;

namespace CTSAR.Booking.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Membre> Membres { get; set; }
    public DbSet<Alveole> Alveoles { get; set; }
    public DbSet<Reservation> Reservations { get; set; }
    public DbSet<PeriodeFermeture> PeriodesFermeture { get; set; }
    public DbSet<MembreReservation> MembresReservations { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configuration Membre
        modelBuilder.Entity<Membre>(entity =>
        {
            entity.HasKey(m => m.Id);
            entity.Property(m => m.Email).HasMaxLength(255);
            entity.HasIndex(m => m.Email).IsUnique();
            entity.Property(m => m.Role).HasConversion<int>();
        });

        // Configuration Alveole
        modelBuilder.Entity<Alveole>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.Property(a => a.Nom).HasMaxLength(50);
            entity.HasIndex(a => a.Nom).IsUnique();
        });

        // Configuration Reservation
        modelBuilder.Entity<Reservation>(entity =>
        {
            entity.HasKey(r => r.Id);

            entity.HasOne(r => r.Alveole)
                  .WithMany(a => a.Reservations)
                  .HasForeignKey(r => r.AlveoleId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(r => r.MembreCreateur)
                  .WithMany(m => m.ReservationsCreees)
                  .HasForeignKey(r => r.MembreCreateurId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(r => r.MoniteurValidateur)
                  .WithMany(m => m.ReservationsValidees)
                  .HasForeignKey(r => r.MoniteurValidateurId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // Configuration PeriodeFermeture
        modelBuilder.Entity<PeriodeFermeture>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.HasOne(p => p.Alveole)
                  .WithMany(a => a.PeriodeseFermeture)
                  .HasForeignKey(p => p.AlveoleId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configuration MembreReservation
        modelBuilder.Entity<MembreReservation>(entity =>
        {
            entity.HasKey(mr => new { mr.MembreId, mr.ReservationId });

            entity.HasOne(mr => mr.Membre)
                  .WithMany()
                  .HasForeignKey(mr => mr.MembreId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(mr => mr.Reservation)
                  .WithMany(r => r.MembresInscrits)
                  .HasForeignKey(mr => mr.ReservationId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Données de test
        SeedData(modelBuilder);
    }

    private void SeedData(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Alveole>().HasData(
            new Alveole
            {
                Id = 1,
                Nom = "Alvéole 1",
                Description = "Stand de tir 25m - Carabine",
                EstActive = true,
                NombreMaxTireurs = 8,
                DateCreation = DateTime.Now
            },
            new Alveole
            {
                Id = 2,
                Nom = "Alvéole 2",
                Description = "Stand de tir 50m - Pistolet/Carabine",
                EstActive = true,
                NombreMaxTireurs = 6,
                DateCreation = DateTime.Now
            }
        );

        modelBuilder.Entity<Membre>().HasData(
            new Membre
            {
                Id = 1,
                Nom = "Martin",
                Prenom = "Jean",
                Email = "jean.martin@club-tir.fr",
                Role = Role.Administrateur,
                LanguePreferee = "fr-FR",
                EstActif = true,
                DateCreation = DateTime.Now
            },
            new Membre
            {
                Id = 2,
                Nom = "Dubois",
                Prenom = "Marie",
                Email = "marie.dubois@club-tir.fr",
                Role = Role.Moniteur,
                LanguePreferee = "fr-FR",
                EstActif = true,
                DateCreation = DateTime.Now
            }
        );
    }
}
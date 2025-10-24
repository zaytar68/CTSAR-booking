using Microsoft.EntityFrameworkCore;

namespace CTSAR.Booking.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    // ================================================================
    // DBSETS : Tables de la base de données
    // ================================================================

    /// <summary>
    /// Table des utilisateurs (authentification custom)
    /// </summary>
    public DbSet<User> Users { get; set; }

    /// <summary>
    /// Table des rôles
    /// </summary>
    public DbSet<Role> Roles { get; set; }

    /// <summary>
    /// Table de liaison User-Role (many-to-many)
    /// </summary>
    public DbSet<UserRole> UserRoles { get; set; }

    /// <summary>
    /// Table des alvéoles (postes de tir)
    /// </summary>
    public DbSet<Alveole> Alveoles { get; set; }

    /// <summary>
    /// Table des réservations
    /// </summary>
    public DbSet<Reservation> Reservations { get; set; }

    /// <summary>
    /// Table de liaison Reservation-Alveole (many-to-many)
    /// </summary>
    public DbSet<ReservationAlveole> ReservationAlveoles { get; set; }

    /// <summary>
    /// Table de liaison Reservation-Participant (many-to-many)
    /// </summary>
    public DbSet<ReservationParticipant> ReservationParticipants { get; set; }

    /// <summary>
    /// Table des fermetures d'alvéoles
    /// </summary>
    public DbSet<FermetureAlveole> FermetureAlveoles { get; set; }

    // ================================================================
    // CONFIGURATION DES MODÈLES
    // ================================================================

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // ================================================================
        // CONFIGURATION DES RELATIONS D'AUTHENTIFICATION
        // ================================================================

        // Configuration de la clé primaire composite pour UserRole
        builder.Entity<UserRole>()
            .HasKey(ur => new { ur.UserId, ur.RoleId });

        builder.Entity<UserRole>()
            .HasOne(ur => ur.User)
            .WithMany(u => u.UserRoles)
            .HasForeignKey(ur => ur.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<UserRole>()
            .HasOne(ur => ur.Role)
            .WithMany(r => r.UserRoles)
            .HasForeignKey(ur => ur.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        // Index unique sur User.Email
        builder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        // Index unique sur Role.Name
        builder.Entity<Role>()
            .HasIndex(r => r.Name)
            .IsUnique();

        // ================================================================
        // CONFIGURATION DES RELATIONS MÉTIER
        // ================================================================

        // Configuration de la relation many-to-many Reservation-Alveole
        builder.Entity<ReservationAlveole>()
            .HasKey(ra => new { ra.ReservationId, ra.AlveoleId });

        builder.Entity<ReservationAlveole>()
            .HasOne(ra => ra.Reservation)
            .WithMany(r => r.ReservationAlveoles)
            .HasForeignKey(ra => ra.ReservationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ReservationAlveole>()
            .HasOne(ra => ra.Alveole)
            .WithMany(a => a.ReservationAlveoles)
            .HasForeignKey(ra => ra.AlveoleId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configuration de la relation many-to-many Reservation-Participant
        builder.Entity<ReservationParticipant>()
            .HasKey(rp => new { rp.ReservationId, rp.UserId });

        builder.Entity<ReservationParticipant>()
            .HasOne(rp => rp.Reservation)
            .WithMany(r => r.Participants)
            .HasForeignKey(rp => rp.ReservationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ReservationParticipant>()
            .HasOne(rp => rp.User)
            .WithMany()
            .HasForeignKey(rp => rp.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configuration de la relation Reservation-CreatedBy
        builder.Entity<Reservation>()
            .HasOne(r => r.CreatedBy)
            .WithMany()
            .HasForeignKey(r => r.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict); // Ne pas supprimer les réservations si on supprime l'utilisateur

        // Configuration de la relation FermetureAlveole-Alveole
        builder.Entity<FermetureAlveole>()
            .HasOne(f => f.Alveole)
            .WithMany(a => a.Fermetures)
            .HasForeignKey(f => f.AlveoleId)
            .OnDelete(DeleteBehavior.Cascade);

        // Index pour optimiser les recherches fréquentes
        builder.Entity<Reservation>()
            .HasIndex(r => r.DateDebut);

        builder.Entity<Reservation>()
            .HasIndex(r => r.DateFin);

        builder.Entity<Alveole>()
            .HasIndex(a => a.Nom)
            .IsUnique();

        builder.Entity<Alveole>()
            .HasIndex(a => a.Ordre);
    }
}

using EasyParking.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EasyParking.Data;

public class EasyParkingDbContext : DbContext
{
    public EasyParkingDbContext(DbContextOptions<EasyParkingDbContext> options) : base(options) { }

    public DbSet<Parkhaus> Parkhaeuser => Set<Parkhaus>();
    public DbSet<Stockwerk> Stockwerke => Set<Stockwerk>();
    public DbSet<Parkplatz> Parkplaetze => Set<Parkplatz>();
    public DbSet<Dauermieter> Dauermieter => Set<Dauermieter>();
    public DbSet<Mietzahlung> Mietzahlungen => Set<Mietzahlung>();
    public DbSet<Parkticket> Parktickets => Set<Parkticket>();
    public DbSet<Tarif> Tarife => Set<Tarif>();
    public DbSet<Feiertag> Feiertage => Set<Feiertag>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Parkhaus>(e =>
        {
            e.HasMany(x => x.Stockwerke).WithOne(x => x.Parkhaus!).HasForeignKey(x => x.ParkhausId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Tarife).WithOne(x => x.Parkhaus!).HasForeignKey(x => x.ParkhausId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Dauermieter).WithOne(x => x.Parkhaus!).HasForeignKey(x => x.ParkhausId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Parktickets).WithOne(x => x.Parkhaus!).HasForeignKey(x => x.ParkhausId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Stockwerk>(e =>
        {
            e.HasMany(x => x.Parkplaetze).WithOne(x => x.Stockwerk!).HasForeignKey(x => x.StockwerkId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Parkplatz>(e =>
        {
            e.HasOne(x => x.Dauermieter).WithOne(x => x.FesterParkplatz!).HasForeignKey<Dauermieter>(x => x.FesterParkplatzId).OnDelete(DeleteBehavior.SetNull);
        });

        b.Entity<Dauermieter>(e =>
        {
            e.HasIndex(x => x.Code).IsUnique();
            e.HasMany(x => x.Mietzahlungen).WithOne(x => x.Dauermieter!).HasForeignKey(x => x.DauermieterId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Parkticket>(e =>
        {
            e.HasIndex(x => x.TicketNummer).IsUnique();
            e.HasOne(x => x.Parkplatz).WithMany().HasForeignKey(x => x.ParkplatzId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Dauermieter).WithMany().HasForeignKey(x => x.DauermieterId).OnDelete(DeleteBehavior.SetNull);
            e.Property(x => x.Betrag).HasPrecision(10, 2);
        });

        b.Entity<Tarif>(e =>
        {
            e.Property(x => x.PreisProStunde).HasPrecision(10, 2);
        });

        b.Entity<Mietzahlung>(e =>
        {
            e.Property(x => x.Betrag).HasPrecision(10, 2);
        });

        b.Entity<Feiertag>(e =>
        {
            e.HasOne(x => x.Parkhaus).WithMany().HasForeignKey(x => x.ParkhausId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.Datum, x.ParkhausId });
        });
    }
}

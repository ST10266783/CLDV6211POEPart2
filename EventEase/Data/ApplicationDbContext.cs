using EventEase.Models;
using Microsoft.EntityFrameworkCore;

namespace EventEase.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<Venue> Venues => Set<Venue>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Booking> Bookings => Set<Booking>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Booking>(entity =>
        {
            entity.HasOne(b => b.Venue)
                  .WithMany(v => v.Bookings)
                  .HasForeignKey(b => b.VenueId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(b => b.Event)
                  .WithMany(e => e.Bookings)
                  .HasForeignKey(b => b.EventId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(b => new { b.VenueId, b.StartDate, b.EndDate });
        });
    }
}
using Microsoft.EntityFrameworkCore;
using Models;

namespace control_lavanderia.Components.Data;

public class LavanderiaDbContext : DbContext
{
    public LavanderiaDbContext(DbContextOptions<LavanderiaDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderDetail> OrderDetails { get; set; }
    public DbSet<Client> Clients { get; set; }
    public DbSet<Location> Locations { get; set; }
    public DbSet<OrderMovement> OrderMovements { get; set; }
    public DbSet<Profile> Profiles { get; set; }
    public DbSet<Suggestion> Suggestions { get; set; }
    public DbSet<Configuration> Configurations { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Order>()
            .HasIndex(o => o.Folio)
            .IsUnique();
        modelBuilder.Entity<Configuration>()
            .HasIndex(c => c.Key)
            .IsUnique();
    }
}
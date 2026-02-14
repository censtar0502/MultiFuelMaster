using Microsoft.EntityFrameworkCore;
using MultiFuelMaster.Models;

namespace MultiFuelMaster.Data
{
    /// <summary>
    /// Main database context for the application
    /// </summary>
    public class AppDbContext : DbContext
    {
        public DbSet<FuelStation> FuelStations { get; set; } = null!;
        public DbSet<FuelType> FuelTypes { get; set; } = null!;
        public DbSet<Dispenser> Dispensers { get; set; } = null!;
        public DbSet<Transaction> Transactions { get; set; } = null!;
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<StationSettings> StationSettings { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlite("Data Source=fuelmaster.db");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure FuelStation entity
            modelBuilder.Entity<FuelStation>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Address).IsRequired().HasMaxLength(50);
                entity.Property(e => e.PhoneNumber).HasMaxLength(20);
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // Configure FuelType entity
            modelBuilder.Entity<FuelType>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ShortName).IsRequired().HasMaxLength(50);
                entity.HasIndex(e => e.ShortName);
            });

            // Configure Dispenser entity
            modelBuilder.Entity<Dispenser>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.DispenserNumber).IsRequired().HasMaxLength(20);
                entity.HasIndex(e => new { e.StationId, e.DispenserNumber }).IsUnique();
                
                entity.HasOne(d => d.Station)
                      .WithMany()
                      .HasForeignKey(d => d.StationId)
                      .OnDelete(DeleteBehavior.Cascade);
                      
                entity.HasOne(d => d.FuelType)
                      .WithMany()
                      .HasForeignKey(d => d.FuelTypeId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure Transaction entity
            modelBuilder.Entity<Transaction>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.VehiclePlate).HasMaxLength(50);
                entity.Property(e => e.DriverName).HasMaxLength(100);
                
                entity.HasOne(t => t.Dispenser)
                      .WithMany()
                      .HasForeignKey(t => t.DispenserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Seed initial data
            SeedData(modelBuilder);
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            // Seed fuel types
            modelBuilder.Entity<FuelType>().HasData(
                new FuelType { Id = 1, Number = 1, ShortName = "А-80", FullName = "Автобензин А-80", ReceiptName = "А-80", Unit = "литры", Color = "#0000FF" },
                new FuelType { Id = 2, Number = 2, ShortName = "А-92", FullName = "Автобензин А-92", ReceiptName = "А-92", Unit = "литры", Color = "#FF0000" },
                new FuelType { Id = 3, Number = 3, ShortName = "А-95", FullName = "Автобензин А-95", ReceiptName = "А-95", Unit = "литры", Color = "#00FF00" },
                new FuelType { Id = 4, Number = 4, ShortName = "ДТ", FullName = "Дизельное топливо", ReceiptName = "ДТ", Unit = "литры", Color = "#000000" }
            );

            // Seed fuel stations
            modelBuilder.Entity<FuelStation>().HasData(
                new FuelStation 
                { 
                    Id = 1, 
                    Name = "АЗС Центральная", 
                    Address = "ул. Пушкина, 10", 
                    Latitude = 41.3111, 
                    Longitude = 69.2797,
                    PhoneNumber = "+998901234567",
                    CreatedDate = DateTime.Now, 
                    LastUpdated = DateTime.Now 
                },
                new FuelStation 
                { 
                    Id = 2, 
                    Name = "АЗС Северная", 
                    Address = "пр. Навои, 45", 
                    Latitude = 41.3256, 
                    Longitude = 69.2401,
                    PhoneNumber = "+998907654321",
                    CreatedDate = DateTime.Now, 
                    LastUpdated = DateTime.Now 
                }
            );

            // Seed dispensers
            modelBuilder.Entity<Dispenser>().HasData(
                new Dispenser { Id = 1, StationId = 1, FuelTypeId = 1, DispenserNumber = "1", CurrentVolume = 1000, TotalVolume = 0, CurrentPrice = 45.50m, CreatedDate = DateTime.Now, LastUpdated = DateTime.Now },
                new Dispenser { Id = 2, StationId = 1, FuelTypeId = 2, DispenserNumber = "2", CurrentVolume = 1000, TotalVolume = 0, CurrentPrice = 48.75m, CreatedDate = DateTime.Now, LastUpdated = DateTime.Now },
                new Dispenser { Id = 3, StationId = 1, FuelTypeId = 3, DispenserNumber = "3", CurrentVolume = 800, TotalVolume = 0, CurrentPrice = 52.00m, CreatedDate = DateTime.Now, LastUpdated = DateTime.Now },
                new Dispenser { Id = 4, StationId = 2, FuelTypeId = 1, DispenserNumber = "1", CurrentVolume = 1200, TotalVolume = 0, CurrentPrice = 45.50m, CreatedDate = DateTime.Now, LastUpdated = DateTime.Now },
                new Dispenser { Id = 5, StationId = 2, FuelTypeId = 4, DispenserNumber = "2", CurrentVolume = 500, TotalVolume = 0, CurrentPrice = 22.30m, CreatedDate = DateTime.Now, LastUpdated = DateTime.Now }
            );
        }
    }
}
using Microsoft.EntityFrameworkCore;
using VehicleRental.Rentals.Infrastructure.ReadModels;
using VehicleRental.Vehicles.Infrastructure;
using VehicleRental.Vehicles.Infrastructure.ReadModels;

namespace VehicleRental.Persistence;

internal sealed class AppReadDbContext(DbContextOptions<AppReadDbContext> options) :
    DbContext(options)
{
    public DbSet<VehicleReadModel> Vehicles { get; set; } = null!;

    public DbSet<VehicleLegalDocumentReadModel> VehicleLegalDocuments { get; set; } = null!;

    public DbSet<VehicleFailureReadModel> VehicleFailures { get; set; } = null!;

    public DbSet<RentalReadModel> Rentals { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        VehicleModuleReadConfiguration.Configure(modelBuilder);
    }
}
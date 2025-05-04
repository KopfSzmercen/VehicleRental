using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using VehicleRental.Users.Domain;
using VehicleRental.Vehicles.Domain.VehicleFailures;
using VehicleRental.Vehicles.Domain.Vehicles;
using VehicleRental.Vehicles.Infrastructure;

namespace VehicleRental.Persistence;

internal sealed class AppDbContext(DbContextOptions<AppDbContext> options) :
    IdentityDbContext<
        User,
        UserRole,
        Guid
    >(options), IDataProtectionKeyContext
{
    public DbSet<Vehicle> Vehicles { get; set; } = null!;

    public DbSet<VehicleFailure> VehicleFailures { get; set; } = null!;

    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        VehicleConfiguration.Configure(modelBuilder);
        VehicleFailureConfiguration.Configure(modelBuilder);
    }
}
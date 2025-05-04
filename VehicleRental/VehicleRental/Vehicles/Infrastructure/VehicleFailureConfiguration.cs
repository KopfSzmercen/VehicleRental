using Microsoft.EntityFrameworkCore;
using VehicleRental.Vehicles.Domain.VehicleFailures;
using VehicleRental.Vehicles.Domain.Vehicles;

namespace VehicleRental.Vehicles.Infrastructure;

internal static class VehicleFailureConfiguration
{
    public static void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<VehicleFailure>(b =>
        {
            b.HasKey(v => v.Id);

            b.Property(v => v.Id)
                .ValueGeneratedNever();

            b.Property(v => v.Name)
                .IsRequired()
                .HasMaxLength(200);

            b.Property(v => v.Status)
                .IsRequired()
                .HasConversion<string>();

            b.Property(v => v.CreatedAt)
                .IsRequired();

            b.Property(v => v.UpdatedAt)
                .IsRequired(false);

            b.HasOne<Vehicle>()
                .WithMany()
                .HasForeignKey(x => x.VehicleId);
        });
    }
}
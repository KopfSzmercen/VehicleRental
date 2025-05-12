using Microsoft.EntityFrameworkCore;
using VehicleRental.Vehicles.Infrastructure.ReadModels;

namespace VehicleRental.Vehicles.Infrastructure;

internal static class VehicleModuleReadConfiguration
{
    public static void Configure(ModelBuilder builder)
    {
        builder.Entity<VehicleReadModel>(b =>
        {
            b.ToTable("Vehicles");

            b.HasKey(v => v.Id);
            b.OwnsOne(x => x.CurrentGeoLocalization);

            b.Property(x => x.Status)
                .HasConversion<string>();
        });

        builder.Entity<VehicleLegalDocumentReadModel>(b =>
        {
            b.ToTable("VehicleLegalDocuments");
            b.HasKey(d => d.Id);

            b.HasOne(d => d.Vehicle)
                .WithMany(v => v.LegalDocuments)
                .HasForeignKey(d => d.VehicleId);
        });

        builder.Entity<VehicleFailureReadModel>(b =>
        {
            b.ToTable("VehicleFailures");
            b.HasKey(f => f.Id);

            b.HasOne(f => f.Vehicle)
                .WithMany(v => v.Failures)
                .HasForeignKey(f => f.VehicleId);
        });
    }
}
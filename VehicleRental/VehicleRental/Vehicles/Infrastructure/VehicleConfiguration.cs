using Microsoft.EntityFrameworkCore;
using VehicleRental.Vehicles.Domain.Vehicles;

namespace VehicleRental.Vehicles.Infrastructure;

internal static class VehicleConfiguration
{
    public static void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Vehicle>(b =>
        {
            b.HasKey(v => v.Id);

            b.Property(x => x.Id).ValueGeneratedNever();

            b.Property(v => v.Name)
                .IsRequired()
                .HasMaxLength(200);

            b.Property(v => v.RegistrationNumber)
                .IsRequired()
                .HasMaxLength(50);

            b.Property(v => v.Status)
                .IsRequired()
                .HasConversion<string>();

            b.OwnsOne(x => x.CurrentGeoLocalization);

            b.Property(v => v.IsAvailableForRental).IsRequired();

            b.OwnsMany(x => x.LegalDocuments, legalDocument =>
            {
                legalDocument.HasKey(x => x.Id);

                legalDocument.Property(x => x.Id).ValueGeneratedNever();

                legalDocument.Property(x => x.Name)
                    .IsRequired()
                    .HasMaxLength(200);
            });
        });
    }
}
using Microsoft.EntityFrameworkCore;
using VehicleRental.Rentals.Infrastructure.ReadModels;

namespace VehicleRental.Rentals.Infrastructure;

internal static class RentalsModuleReadConfiguration
{
    public static void Configure(ModelBuilder builder)
    {
        builder.Entity<RentalReadModel>(b =>
        {
            b.ToTable("Rentals");

            b.HasKey(r => r.Id);

            b.Property(x => x.Status)
                .HasConversion<string>();
        });
    }
}
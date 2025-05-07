using Microsoft.EntityFrameworkCore;
using VehicleRental.Rentals.Domain;
using VehicleRental.Users.Domain;
using VehicleRental.Vehicles.Domain.Vehicles;

namespace VehicleRental.Rentals.Infrastructure;

internal static class RentalConfiguration
{
    public static void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Rental>(b =>
        {
            b.HasKey(r => r.Id);

            b.Property(r => r.Id)
                .ValueGeneratedNever();

            b.HasOne<Vehicle>()
                .WithMany()
                .HasForeignKey(r => r.VehicleId);

            b.Property(r => r.VehicleId)
                .ValueGeneratedNever();

            b.HasOne<User>()
                .WithMany()
                .HasForeignKey(r => r.CustomerId);

            b.Property(r => r.CustomerId)
                .ValueGeneratedNever();

            b.Property(r => r.StartDate)
                .IsRequired();

            b.Property(r => r.EndDate)
                .IsRequired();

            b.Property(r => r.CreatedAt)
                .IsRequired();

            b.Property(r => r.CompletedAt)
                .IsRequired(false);

            b.Property(r => r.CancelledAt)
                .IsRequired(false);

            b.Property(r => r.Status)
                .IsRequired()
                .HasConversion<string>();
        });
    }
}
using Microsoft.EntityFrameworkCore;
using VehicleRental.Rentals.Domain;
using VehicleRental.Rentals.Domain.Reservations;

namespace VehicleRental.Rentals.Infrastructure;

internal static class RentalsVehicleConfiguration
{
    public static void Configure(ModelBuilder builder)
    {
        builder.Entity<RentalsVehicle>(b =>
        {
            b.HasKey(v => v.Id);

            b.Property(v => v.Id)
                .ValueGeneratedNever();

            b.Property(v => v.UpdatedAt)
                .IsRequired();

            b.HasOne(x => x.Rental)
                .WithOne()
                .HasForeignKey<Rental>(r => r.CurrentVehicleId);

            b.HasOne(x => x.Reservation)
                .WithOne()
                .HasForeignKey<Reservation>(r => r.CurrentVehicleId);
        });

        builder.Entity<Rental>(b =>
        {
            b.ToTable("Rentals");

            b.HasKey(r => r.Id);

            b.Property(r => r.Id)
                .ValueGeneratedNever();

            b.Property(r => r.VehicleId)
                .ValueGeneratedNever();

            b.Property(r => r.CustomerId)
                .ValueGeneratedNever();

            b.Property(r => r.CurrentVehicleId)
                .ValueGeneratedNever();

            b.Property(r => r.StartDate)
                .IsRequired();

            b.Property(r => r.EndDate)
                .IsRequired();

            b.Property(r => r.CreatedAt)
                .IsRequired();

            b.Property(r => r.Status)
                .IsRequired()
                .HasConversion<string>();
        });

        builder.Entity<Reservation>(b =>
        {
            b.ToTable("Reservations");

            b.HasKey(r => r.Id);

            b.Property(r => r.Id)
                .ValueGeneratedNever();

            b.Property(r => r.VehicleId)
                .ValueGeneratedNever();

            b.Property(r => r.UserId)
                .ValueGeneratedNever();

            b.Property(r => r.CurrentVehicleId)
                .ValueGeneratedNever();

            b.Property(x => x.Duration)
                .HasConversion(
                    v => v.Value,
                    v => new ReservationDurationInSeconds(v));
        });
    }
}
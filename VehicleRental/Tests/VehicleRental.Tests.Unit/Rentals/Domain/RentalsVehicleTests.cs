using Shouldly;
using VehicleRental.Common.ErrorHandling;
using VehicleRental.Rentals.Domain;
using VehicleRental.Rentals.Domain.Reservations;

namespace VehicleTests.Tests.Unit.Rentals.Domain;

public class RentalsVehicleTests
{
    [Fact]
    public void CreateNew_ShouldInitializeProperties()
    {
        // Arrange
        var id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        // Act
        var rentalsVehicle = RentalsVehicle.CreateNew(id, now);

        // Assert
        rentalsVehicle.Id.ShouldBe(id);
    }

    [Fact]
    public void Reserve_ShouldSetReservationAndUpdateTimestamp()
    {
        // Arrange
        var now = new DateTimeOffset(2023, 10, 1, 12, 0, 0, TimeSpan.Zero);

        var rentalsVehicle = RentalsVehicle.CreateNew(Guid.NewGuid(), now);

        var reservation = Reservation.CreateNew(
            Guid.NewGuid(),
            now.AddHours(1),
            new ReservationDurationInSeconds(3600),
            now,
            Guid.NewGuid()
        );


        // Act
        rentalsVehicle.Reserve(reservation, now);

        // Assert
        rentalsVehicle.Reservation.ShouldNotBeNull();
        rentalsVehicle.UpdatedAt.ShouldBe(now);
    }

    [Fact]
    public void Reserve_WhenAlreadyReserved_ShouldThrowBusinessRuleValidationException()
    {
        // Arrange
        var now = new DateTimeOffset(2023, 10, 1, 12, 0, 0, TimeSpan.Zero);

        var rentalsVehicle = RentalsVehicle.CreateNew(Guid.NewGuid(), now);

        var reservation = Reservation.CreateNew(
            Guid.NewGuid(),
            now.AddHours(1),
            new ReservationDurationInSeconds(3600),
            now,
            Guid.NewGuid()
        );

        rentalsVehicle.Reserve(reservation, now);

        // Act & Assert
        Should.Throw<BusinessRuleValidationException>(() => rentalsVehicle.Reserve(reservation, now));
    }

    [Fact]
    public void Reserve_WhenAlreadyRented_ShouldThrowBusinessRuleValidationException()
    {
        // Arrange
        var now = new DateTimeOffset(2023, 10, 1, 12, 0, 0, TimeSpan.Zero);

        var rentalsVehicle = RentalsVehicle.CreateNew(Guid.NewGuid(), now);

        var rental = Rental.CreateNew(
            Guid.NewGuid(),
            Guid.NewGuid(),
            now.AddHours(1),
            now.AddHours(2),
            new Money(100, Currency.AUD),
            now
        );

        var reservation = Reservation.CreateNew(
            Guid.NewGuid(),
            now.AddHours(1),
            new ReservationDurationInSeconds(3600),
            now,
            Guid.NewGuid()
        );

        rentalsVehicle.Rent(rental, now);

        // Act & Assert
        Should.Throw<BusinessRuleValidationException>(() => rentalsVehicle.Reserve(reservation, now));
    }

    [Fact]
    public void CancelReservation_ShouldSetReservationToNullAndUpdateTimestamp()
    {
        // Arrange
        var now = new DateTimeOffset(2023, 10, 1, 12, 0, 0, TimeSpan.Zero);

        var rentalsVehicle = RentalsVehicle.CreateNew(Guid.NewGuid(), now);

        var reservation = Reservation.CreateNew(
            Guid.NewGuid(),
            now.AddHours(1),
            new ReservationDurationInSeconds(3600),
            now,
            Guid.NewGuid()
        );

        rentalsVehicle.Reserve(reservation, now);

        // Act
        rentalsVehicle.CancelReservation(now);

        // Assert
        rentalsVehicle.Reservation.ShouldBeNull();
        rentalsVehicle.UpdatedAt.ShouldBe(now);
    }

    [Fact]
    public void CancelReservation_WhenNotReserved_ShouldThrowBusinessRuleValidationException()
    {
        // Arrange
        var now = new DateTimeOffset(2023, 10, 1, 12, 0, 0, TimeSpan.Zero);

        var rentalsVehicle = RentalsVehicle.CreateNew(Guid.NewGuid(), now);

        // Act & Assert
        Should.Throw<BusinessRuleValidationException>(() => rentalsVehicle.CancelReservation(now));
    }

    [Fact]
    public void Rent_ShouldSetRentalAndUpdateTimestamp()
    {
        // Arrange
        var now = new DateTimeOffset(2023, 10, 1, 12, 0, 0, TimeSpan.Zero);

        var rentalsVehicle = RentalsVehicle.CreateNew(Guid.NewGuid(), now);

        var rental = Rental.CreateNew(
            Guid.NewGuid(),
            Guid.NewGuid(),
            now.AddHours(1),
            now.AddHours(2),
            new Money(100, Currency.AUD),
            now
        );

        // Act
        rentalsVehicle.Rent(rental, now);

        // Assert
        rentalsVehicle.Rental.ShouldNotBeNull();
        rentalsVehicle.UpdatedAt.ShouldBe(now);
    }

    [Fact]
    public void Rent_WhenAlreadyRented_ShouldThrowBusinessRuleValidationException()
    {
        // Arrange
        var now = new DateTimeOffset(2023, 10, 1, 12, 0, 0, TimeSpan.Zero);

        var rentalsVehicle = RentalsVehicle.CreateNew(Guid.NewGuid(), now);

        var rental = Rental.CreateNew(
            Guid.NewGuid(),
            Guid.NewGuid(),
            now.AddHours(1),
            now.AddHours(2),
            new Money(100, Currency.AUD),
            now
        );

        rentalsVehicle.Rent(rental, now);

        // Act & Assert
        Should.Throw<BusinessRuleValidationException>(() => rentalsVehicle.Rent(rental, now));
    }

    [Fact]
    public void Rent_WhenReserved_ShouldThrowBusinessRuleValidationException()
    {
        // Arrange
        var now = new DateTimeOffset(2023, 10, 1, 12, 0, 0, TimeSpan.Zero);

        var rentalsVehicle = RentalsVehicle.CreateNew(Guid.NewGuid(), now);

        var reservation = Reservation.CreateNew(
            Guid.NewGuid(),
            now.AddHours(1),
            new ReservationDurationInSeconds(3600),
            now,
            Guid.NewGuid()
        );

        var rental = Rental.CreateNew(
            Guid.NewGuid(),
            Guid.NewGuid(),
            now.AddHours(1),
            now.AddHours(2),
            new Money(100, Currency.AUD),
            now
        );

        rentalsVehicle.Reserve(reservation, now);

        // Act & Assert
        Should.Throw<BusinessRuleValidationException>(() => rentalsVehicle.Rent(rental, now));
    }

    [Fact]
    public void CompleteRental_ShouldSetRentalToNullAndUpdateTimestamp()
    {
        // Arrange
        var now = new DateTimeOffset(2023, 10, 1, 12, 0, 0, TimeSpan.Zero);

        var rentalsVehicle = RentalsVehicle.CreateNew(Guid.NewGuid(), now);

        var rental = Rental.CreateNew(
            Guid.NewGuid(),
            Guid.NewGuid(),
            now.AddHours(1),
            now.AddHours(2),
            new Money(100, Currency.AUD),
            now
        );

        rentalsVehicle.Rent(rental, now);

        // Act
        rentalsVehicle.CompleteRental(now);

        // Assert
        rentalsVehicle.Rental.ShouldBeNull();
        rentalsVehicle.UpdatedAt.ShouldBe(now);
    }

    [Fact]
    public void CompleteRental_WhenNotRented_ShouldThrowBusinessRuleValidationException()
    {
        // Arrange
        var now = new DateTimeOffset(2023, 10, 1, 12, 0, 0, TimeSpan.Zero);

        var rentalsVehicle = RentalsVehicle.CreateNew(Guid.NewGuid(), now);

        // Act & Assert
        Should.Throw<BusinessRuleValidationException>(() => rentalsVehicle.CompleteRental(now));
    }
}
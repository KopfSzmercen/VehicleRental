using Shouldly;
using VehicleRental.Common.ErrorHandling;
using VehicleRental.Rentals.Domain;
using VehicleRental.Rentals.Domain.Reservations;

namespace VehicleTests.Tests.Unit.Rentals.Domain;

public class RentalsVehicleTests
{
    // The owner's reservation window is [StartDate, WindowEnd) = [13:00, 14:00).
    private static readonly DateTimeOffset Now = new(2023, 10, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset StartDate = Now.AddHours(1);
    private static readonly DateTimeOffset WindowEnd = StartDate.AddHours(1);
    private static readonly Guid OwnerId = Guid.NewGuid();
    private static readonly Guid OtherClientId = Guid.NewGuid();

    private static Reservation ReservationFor(Guid userId)
    {
        return Reservation.CreateNew(Guid.NewGuid(), StartDate, new ReservationDurationInSeconds(3600), Now, userId);
    }

    private static Rental RentalFor(Guid customerId, DateTimeOffset startDate, DateTimeOffset? endDate = null)
    {
        return Rental.CreateNew(
            Guid.NewGuid(),
            customerId,
            startDate,
            endDate ?? startDate.AddMinutes(30),
            new Money(1000, Currency.USD),
            Now
        );
    }

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
        rentalsVehicle.Reserve(reservation, true, now);

        // Assert
        rentalsVehicle.Reservation.ShouldNotBeNull();
        rentalsVehicle.UpdatedAt.ShouldBe(now);
    }

    [Fact]
    public void Reserve_WhenVehicleIsNotAvailable_ShouldThrowBusinessRuleValidationException()
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

        // Act & Assert
        Should.Throw<BusinessRuleValidationException>(() => rentalsVehicle.Reserve(reservation, false, now))
            .Message.ShouldBe("Vehicle is not available for rental.");
        rentalsVehicle.Reservation.ShouldBeNull();
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

        rentalsVehicle.Reserve(reservation, true, now);

        // Act & Assert
        Should.Throw<BusinessRuleValidationException>(() => rentalsVehicle.Reserve(reservation, true, now));
    }

    [Fact]
    public void Reserve_WhenAttachedReservationHasExpired_ShouldExpireItAndAttachNewReservation()
    {
        // Arrange
        var rentalsVehicle = RentalsVehicle.CreateNew(Guid.NewGuid(), Now);
        var oldReservation = ReservationFor(OwnerId);
        rentalsVehicle.Reserve(oldReservation, true, Now);

        var later = WindowEnd;
        var newReservation = Reservation.CreateNew(
            rentalsVehicle.Id, later.AddHours(1), new ReservationDurationInSeconds(3600), later, OtherClientId);

        // Act
        rentalsVehicle.Reserve(newReservation, true, later);

        // Assert
        oldReservation.Status.ShouldBe(ReservationStatus.Expired);
        oldReservation.CurrentVehicleId.ShouldBeNull();
        rentalsVehicle.Reservation.ShouldBe(newReservation);
    }

    [Fact]
    public void Reserve_WhenAttachedReservationIsActiveAndNotExpired_ShouldThrowAlreadyReserved()
    {
        // Arrange
        var rentalsVehicle = RentalsVehicle.CreateNew(Guid.NewGuid(), Now);
        var reservation = ReservationFor(OwnerId);
        rentalsVehicle.Reserve(reservation, true, Now);

        var justBeforeWindowEnd = WindowEnd.AddTicks(-1);
        var newReservation = Reservation.CreateNew(
            rentalsVehicle.Id, WindowEnd.AddHours(1), new ReservationDurationInSeconds(3600),
            justBeforeWindowEnd, OtherClientId);

        // Act & Assert
        Should.Throw<BusinessRuleValidationException>(() =>
                rentalsVehicle.Reserve(newReservation, true, justBeforeWindowEnd))
            .Message.ShouldBe("Vehicle is already reserved.");
        rentalsVehicle.Reservation.ShouldBe(reservation);
        reservation.Status.ShouldBe(ReservationStatus.Active);
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
            new Money(1000, Currency.USD),
            now
        );

        var reservation = Reservation.CreateNew(
            Guid.NewGuid(),
            now.AddHours(1),
            new ReservationDurationInSeconds(3600),
            now,
            Guid.NewGuid()
        );

        rentalsVehicle.Rent(rental, true, now);

        // Act & Assert
        Should.Throw<BusinessRuleValidationException>(() => rentalsVehicle.Reserve(reservation, true, now));
    }

    [Fact]
    public void Reserve_WhenVehicleIsNotAvailableAndAlreadyReserved_ShouldThrowNotAvailableBusinessRuleValidationException()
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

        rentalsVehicle.Reserve(reservation, true, now);

        // Act & Assert
        Should.Throw<BusinessRuleValidationException>(() => rentalsVehicle.Reserve(reservation, false, now))
            .Message.ShouldBe("Vehicle is not available for rental.");
    }

    [Fact]
    public void Reserve_WhenVehicleIsNotAvailableAndAlreadyRented_ShouldThrowNotAvailableBusinessRuleValidationException()
    {
        // Arrange
        var now = new DateTimeOffset(2023, 10, 1, 12, 0, 0, TimeSpan.Zero);

        var rentalsVehicle = RentalsVehicle.CreateNew(Guid.NewGuid(), now);

        var rental = Rental.CreateNew(
            Guid.NewGuid(),
            Guid.NewGuid(),
            now.AddHours(1),
            now.AddHours(2),
            new Money(1000, Currency.USD),
            now
        );

        var reservation = Reservation.CreateNew(
            Guid.NewGuid(),
            now.AddHours(1),
            new ReservationDurationInSeconds(3600),
            now,
            Guid.NewGuid()
        );

        rentalsVehicle.Rent(rental, true, now);

        // Act & Assert
        Should.Throw<BusinessRuleValidationException>(() => rentalsVehicle.Reserve(reservation, false, now))
            .Message.ShouldBe("Vehicle is not available for rental.");
    }

    [Theory]
    [InlineData(0)] // before the reservation starts
    [InlineData(90)] // inside the reservation window
    public void CancelReservation_ByOwnerWhileActive_ShouldCancelAndDetachReservation(int minutesAfterNow)
    {
        // Arrange
        var rentalsVehicle = RentalsVehicle.CreateNew(Guid.NewGuid(), Now);
        var reservation = ReservationFor(OwnerId);
        rentalsVehicle.Reserve(reservation, true, Now);
        var cancelledAt = Now.AddMinutes(minutesAfterNow);

        // Act
        rentalsVehicle.CancelReservation(reservation.Id, OwnerId, cancelledAt);

        // Assert
        rentalsVehicle.Reservation.ShouldBeNull();
        rentalsVehicle.UpdatedAt.ShouldBe(cancelledAt);
        reservation.Status.ShouldBe(ReservationStatus.Cancelled);
        reservation.CancelledAt.ShouldBe(cancelledAt);
    }

    [Fact]
    public void CancelReservation_ByAnotherClient_ShouldNotCancel()
    {
        // Arrange
        var rentalsVehicle = RentalsVehicle.CreateNew(Guid.NewGuid(), Now);
        var reservation = ReservationFor(OwnerId);
        rentalsVehicle.Reserve(reservation, true, Now);

        // Act & Assert
        Should.Throw<BusinessRuleValidationException>(() =>
                rentalsVehicle.CancelReservation(reservation.Id, OtherClientId, Now))
            .Message.ShouldBe($"Reservation with ID {reservation.Id} does not exist.");
        rentalsVehicle.Reservation.ShouldBe(reservation);
        reservation.Status.ShouldBe(ReservationStatus.Active);
    }

    [Fact]
    public void CancelReservation_WithWrongId_ShouldNotCancel()
    {
        // Arrange
        var rentalsVehicle = RentalsVehicle.CreateNew(Guid.NewGuid(), Now);
        var reservation = ReservationFor(OwnerId);
        rentalsVehicle.Reserve(reservation, true, Now);
        var wrongId = Guid.NewGuid();

        // Act & Assert
        Should.Throw<BusinessRuleValidationException>(() =>
                rentalsVehicle.CancelReservation(wrongId, OwnerId, Now))
            .Message.ShouldBe($"Reservation with ID {wrongId} does not exist.");
        reservation.Status.ShouldBe(ReservationStatus.Active);
    }

    [Fact]
    public void CancelReservation_WhenNotReserved_ShouldThrowBusinessRuleValidationException()
    {
        var rentalsVehicle = RentalsVehicle.CreateNew(Guid.NewGuid(), Now);

        Should.Throw<BusinessRuleValidationException>(() =>
            rentalsVehicle.CancelReservation(Guid.NewGuid(), OwnerId, Now));
    }

    [Fact]
    public void CancelReservation_WhenReservationHasExpired_ShouldNotCancel()
    {
        // Arrange
        var rentalsVehicle = RentalsVehicle.CreateNew(Guid.NewGuid(), Now);
        var reservation = ReservationFor(OwnerId);
        rentalsVehicle.Reserve(reservation, true, Now);

        // Act & Assert
        Should.Throw<BusinessRuleValidationException>(() =>
            rentalsVehicle.CancelReservation(reservation.Id, OwnerId, WindowEnd));
        reservation.Status.ShouldNotBe(ReservationStatus.Cancelled);
    }

    [Fact]
    public void ExpireReservationIfDue_WhenNotReserved_ShouldReturnFalse()
    {
        var rentalsVehicle = RentalsVehicle.CreateNew(Guid.NewGuid(), Now);

        rentalsVehicle.ExpireReservationIfDue(WindowEnd).ShouldBeFalse();
    }

    [Fact]
    public void ExpireReservationIfDue_WhenReservationIsNotDue_ShouldReturnFalseAndKeepReservation()
    {
        // Arrange
        var rentalsVehicle = RentalsVehicle.CreateNew(Guid.NewGuid(), Now);
        var reservation = ReservationFor(OwnerId);
        rentalsVehicle.Reserve(reservation, true, Now);

        // Act
        var changed = rentalsVehicle.ExpireReservationIfDue(WindowEnd.AddTicks(-1));

        // Assert
        changed.ShouldBeFalse();
        rentalsVehicle.Reservation.ShouldBe(reservation);
        reservation.Status.ShouldBe(ReservationStatus.Active);
    }

    [Fact]
    public void ExpireReservationIfDue_WhenReservationIsDue_ShouldExpireItOnlyOnce()
    {
        // Arrange
        var rentalsVehicle = RentalsVehicle.CreateNew(Guid.NewGuid(), Now);
        var reservation = ReservationFor(OwnerId);
        rentalsVehicle.Reserve(reservation, true, Now);

        // Act
        var firstCall = rentalsVehicle.ExpireReservationIfDue(WindowEnd);
        var secondCall = rentalsVehicle.ExpireReservationIfDue(WindowEnd);

        // Assert
        firstCall.ShouldBeTrue();
        secondCall.ShouldBeFalse();
        rentalsVehicle.Reservation.ShouldBeNull();
        reservation.Status.ShouldBe(ReservationStatus.Expired);
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
            new Money(1000, Currency.USD),
            now
        );

        // Act
        rentalsVehicle.Rent(rental, true, now);

        // Assert
        rentalsVehicle.Rental.ShouldNotBeNull();
        rentalsVehicle.UpdatedAt.ShouldBe(now);
    }

    [Fact]
    public void Rent_WhenVehicleIsNotAvailable_ShouldThrowBusinessRuleValidationException()
    {
        // Arrange
        var now = new DateTimeOffset(2023, 10, 1, 12, 0, 0, TimeSpan.Zero);

        var rentalsVehicle = RentalsVehicle.CreateNew(Guid.NewGuid(), now);

        var rental = Rental.CreateNew(
            Guid.NewGuid(),
            Guid.NewGuid(),
            now.AddHours(1),
            now.AddHours(2),
            new Money(1000, Currency.USD),
            now
        );

        // Act & Assert
        Should.Throw<BusinessRuleValidationException>(() => rentalsVehicle.Rent(rental, false, now))
            .Message.ShouldBe("Vehicle is not available for rental.");
        rentalsVehicle.Rental.ShouldBeNull();
    }

    [Fact]
    public void Rent_WhenVehicleIsNotAvailableAndAlreadyRented_ShouldThrowNotAvailableBusinessRuleValidationException()
    {
        // Arrange
        var now = new DateTimeOffset(2023, 10, 1, 12, 0, 0, TimeSpan.Zero);

        var rentalsVehicle = RentalsVehicle.CreateNew(Guid.NewGuid(), now);

        var rental = Rental.CreateNew(
            Guid.NewGuid(),
            Guid.NewGuid(),
            now.AddHours(1),
            now.AddHours(2),
            new Money(1000, Currency.USD),
            now
        );

        rentalsVehicle.Rent(rental, true, now);

        // Act & Assert
        Should.Throw<BusinessRuleValidationException>(() => rentalsVehicle.Rent(rental, false, now))
            .Message.ShouldBe("Vehicle is not available for rental.");
    }

    [Fact]
    public void Rent_WhenVehicleIsNotAvailableAndReserved_ShouldThrowNotAvailableBusinessRuleValidationException()
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
            new Money(1000, Currency.USD),
            now
        );

        rentalsVehicle.Reserve(reservation, true, now);

        // Act & Assert
        Should.Throw<BusinessRuleValidationException>(() => rentalsVehicle.Rent(rental, false, now))
            .Message.ShouldBe("Vehicle is not available for rental.");
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
            new Money(1000, Currency.USD),
            now
        );

        rentalsVehicle.Rent(rental, true, now);

        // Act & Assert
        Should.Throw<BusinessRuleValidationException>(() => rentalsVehicle.Rent(rental, true, now));
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
            new Money(1000, Currency.USD),
            now
        );

        rentalsVehicle.Reserve(reservation, true, now);

        // Act & Assert
        Should.Throw<BusinessRuleValidationException>(() => rentalsVehicle.Rent(rental, true, now));
    }

    [Fact]
    public void Rent_ByOwnerInsideReservationWindow_ShouldFulfilReservationAndAttachRental()
    {
        // Arrange
        var rentalsVehicle = RentalsVehicle.CreateNew(Guid.NewGuid(), Now);
        var reservation = ReservationFor(OwnerId);
        rentalsVehicle.Reserve(reservation, true, Now);
        var rental = RentalFor(OwnerId, StartDate.AddMinutes(30));

        // Act
        rentalsVehicle.Rent(rental, true, Now);

        // Assert
        rentalsVehicle.Rental.ShouldBe(rental);
        rentalsVehicle.Reservation.ShouldBeNull();
        reservation.Status.ShouldBe(ReservationStatus.Fulfilled);
        reservation.CurrentVehicleId.ShouldBeNull();
    }

    [Fact]
    public void Rent_ByOwnerStartingExactlyAtReservationStart_ShouldFulfilReservation()
    {
        // Arrange
        var rentalsVehicle = RentalsVehicle.CreateNew(Guid.NewGuid(), Now);
        var reservation = ReservationFor(OwnerId);
        rentalsVehicle.Reserve(reservation, true, Now);

        // Act
        rentalsVehicle.Rent(RentalFor(OwnerId, StartDate), true, Now);

        // Assert
        reservation.Status.ShouldBe(ReservationStatus.Fulfilled);
        rentalsVehicle.Rental.ShouldNotBeNull();
    }

    [Fact]
    public void Rent_ByOwnerStartingBeforeReservationStart_ShouldThrowWindowError()
    {
        // Arrange
        var rentalsVehicle = RentalsVehicle.CreateNew(Guid.NewGuid(), Now);
        var reservation = ReservationFor(OwnerId);
        rentalsVehicle.Reserve(reservation, true, Now);

        // Act & Assert
        Should.Throw<BusinessRuleValidationException>(() =>
                rentalsVehicle.Rent(RentalFor(OwnerId, StartDate.AddTicks(-1)), true, Now))
            .Message.ShouldBe("Rental must start within the reservation window.");
        rentalsVehicle.Rental.ShouldBeNull();
        rentalsVehicle.Reservation.ShouldBe(reservation);
        reservation.Status.ShouldBe(ReservationStatus.Active);
    }

    [Fact]
    public void Rent_ByOwnerStartingExactlyAtWindowEnd_ShouldThrowWindowError()
    {
        // Arrange
        var rentalsVehicle = RentalsVehicle.CreateNew(Guid.NewGuid(), Now);
        var reservation = ReservationFor(OwnerId);
        rentalsVehicle.Reserve(reservation, true, Now);

        // Act & Assert
        Should.Throw<BusinessRuleValidationException>(() =>
                rentalsVehicle.Rent(RentalFor(OwnerId, WindowEnd), true, Now))
            .Message.ShouldBe("Rental must start within the reservation window.");
        reservation.Status.ShouldBe(ReservationStatus.Active);
    }

    [Fact]
    public void Rent_ByOwnerWhenReservationHasExpired_ShouldRentWithoutFulfillingReservation()
    {
        // Arrange
        var rentalsVehicle = RentalsVehicle.CreateNew(Guid.NewGuid(), Now);
        var reservation = ReservationFor(OwnerId);
        rentalsVehicle.Reserve(reservation, true, Now);
        var rental = RentalFor(OwnerId, WindowEnd);

        // Act
        rentalsVehicle.Rent(rental, true, WindowEnd);

        // Assert
        rentalsVehicle.Rental.ShouldBe(rental);
        rentalsVehicle.Reservation.ShouldBeNull();
        reservation.Status.ShouldBe(ReservationStatus.Expired);
    }

    [Fact]
    public void Rent_ByOwnerEndingAfterReservationWindow_ShouldSucceed()
    {
        // Arrange
        var rentalsVehicle = RentalsVehicle.CreateNew(Guid.NewGuid(), Now);
        var reservation = ReservationFor(OwnerId);
        rentalsVehicle.Reserve(reservation, true, Now);
        var rental = RentalFor(OwnerId, StartDate.AddMinutes(10), WindowEnd.AddDays(1));

        // Act
        rentalsVehicle.Rent(rental, true, Now);

        // Assert
        rentalsVehicle.Rental.ShouldBe(rental);
        reservation.Status.ShouldBe(ReservationStatus.Fulfilled);
    }

    [Fact]
    public void Rent_ByAnotherClientWhenReserved_ShouldThrowVehicleIsReserved()
    {
        // Arrange
        var rentalsVehicle = RentalsVehicle.CreateNew(Guid.NewGuid(), Now);
        var reservation = ReservationFor(OwnerId);
        rentalsVehicle.Reserve(reservation, true, Now);

        // Act & Assert
        Should.Throw<BusinessRuleValidationException>(() =>
                rentalsVehicle.Rent(RentalFor(OtherClientId, StartDate), true, Now))
            .Message.ShouldBe("Vehicle is reserved.");
        rentalsVehicle.Rental.ShouldBeNull();
        reservation.Status.ShouldBe(ReservationStatus.Active);
    }

    [Fact]
    public void Rent_ByAnotherClientWhenReservationHasExpired_ShouldSucceed()
    {
        // Arrange
        var rentalsVehicle = RentalsVehicle.CreateNew(Guid.NewGuid(), Now);
        var reservation = ReservationFor(OwnerId);
        rentalsVehicle.Reserve(reservation, true, Now);
        var rental = RentalFor(OtherClientId, WindowEnd.AddMinutes(1));

        // Act
        rentalsVehicle.Rent(rental, true, WindowEnd);

        // Assert
        rentalsVehicle.Rental.ShouldBe(rental);
        reservation.Status.ShouldBe(ReservationStatus.Expired);
    }

    [Fact]
    public void Rent_WhenRentedAfterOwnerFulfilledReservation_ShouldThrowAlreadyRented()
    {
        // Arrange
        var rentalsVehicle = RentalsVehicle.CreateNew(Guid.NewGuid(), Now);
        rentalsVehicle.Reserve(ReservationFor(OwnerId), true, Now);
        rentalsVehicle.Rent(RentalFor(OwnerId, StartDate), true, Now);

        // Act & Assert
        Should.Throw<BusinessRuleValidationException>(() =>
                rentalsVehicle.Rent(RentalFor(OwnerId, StartDate.AddMinutes(1)), true, Now))
            .Message.ShouldBe("Vehicle is already rented.");
    }

    [Fact]
    public void Rent_WhenVehicleIsNotAvailableAndOwnerRentsInsideWindow_ShouldThrowNotAvailable()
    {
        // Arrange
        var rentalsVehicle = RentalsVehicle.CreateNew(Guid.NewGuid(), Now);
        var reservation = ReservationFor(OwnerId);
        rentalsVehicle.Reserve(reservation, true, Now);

        // Act & Assert
        Should.Throw<BusinessRuleValidationException>(() =>
                rentalsVehicle.Rent(RentalFor(OwnerId, StartDate), false, Now))
            .Message.ShouldBe("Vehicle is not available for rental.");
        reservation.Status.ShouldBe(ReservationStatus.Active);
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
            new Money(1000, Currency.USD),
            now
        );

        rentalsVehicle.Rent(rental, true, now);

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
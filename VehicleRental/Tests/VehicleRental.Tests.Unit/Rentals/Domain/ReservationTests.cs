using Shouldly;
using VehicleRental.Common.ErrorHandling;
using VehicleRental.Rentals.Domain.Reservations;

namespace VehicleTests.Tests.Unit.Rentals.Domain;

public class ReservationTests
{
    private static readonly DateTimeOffset Now = new(2023, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset StartDate = new(2023, 1, 2, 0, 0, 0, TimeSpan.Zero);
    private static readonly ReservationDurationInSeconds Duration = new(3600);
    private static readonly DateTimeOffset WindowEnd = StartDate.AddHours(1);

    [Fact]
    public void CreateNew_ShouldThrowException_WhenStartDateIsInThePast()
    {
        Should.Throw<ArgumentException>(() =>
            Reservation.CreateNew(Guid.NewGuid(), StartDate, Duration, StartDate.AddDays(1), Guid.NewGuid()));
    }

    [Fact]
    public void CreateNew_ShouldCreateActiveReservation_WhenValidParametersAreProvided()
    {
        // Arrange
        var vehicleId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        // Act
        var reservation = Reservation.CreateNew(vehicleId, StartDate, Duration, Now, userId);

        // Assert
        reservation.VehicleId.ShouldBe(vehicleId);
        reservation.StartDate.ShouldBe(StartDate);
        reservation.Duration.ShouldBe(Duration);
        reservation.UserId.ShouldBe(userId);
        reservation.Status.ShouldBe(ReservationStatus.Active);
        reservation.CurrentVehicleId.ShouldBe(vehicleId);
    }

    [Fact]
    public void IsExpired_JustBeforeWindowEnd_ShouldBeFalse()
    {
        var reservation = CreateReservation();

        reservation.IsExpired(WindowEnd.AddTicks(-1)).ShouldBeFalse();
    }

    [Fact]
    public void IsExpired_AtWindowEnd_ShouldBeTrue()
    {
        var reservation = CreateReservation();

        reservation.IsExpired(WindowEnd).ShouldBeTrue();
    }

    [Fact]
    public void Cancel_ShouldMarkReservationCancelledAndDetachIt()
    {
        // Arrange
        var reservation = CreateReservation();
        var cancelledAt = Now.AddMinutes(5);

        // Act
        reservation.Cancel(cancelledAt);

        // Assert
        reservation.Status.ShouldBe(ReservationStatus.Cancelled);
        reservation.CancelledAt.ShouldBe(cancelledAt);
        reservation.UpdatedAt.ShouldBe(cancelledAt);
        reservation.CurrentVehicleId.ShouldBeNull();
    }

    [Fact]
    public void Expire_ShouldMarkReservationExpiredAndDetachIt()
    {
        // Arrange
        var reservation = CreateReservation();

        // Act
        reservation.Expire(WindowEnd);

        // Assert
        reservation.Status.ShouldBe(ReservationStatus.Expired);
        reservation.CancelledAt.ShouldBeNull();
        reservation.UpdatedAt.ShouldBe(WindowEnd);
        reservation.CurrentVehicleId.ShouldBeNull();
    }

    [Fact]
    public void Fulfil_ShouldMarkReservationFulfilledAndDetachIt()
    {
        // Arrange
        var reservation = CreateReservation();
        var fulfilledAt = Now.AddMinutes(5);

        // Act
        reservation.Fulfil(fulfilledAt);

        // Assert
        reservation.Status.ShouldBe(ReservationStatus.Fulfilled);
        reservation.CancelledAt.ShouldBeNull();
        reservation.UpdatedAt.ShouldBe(fulfilledAt);
        reservation.CurrentVehicleId.ShouldBeNull();
    }

    [Fact]
    public void Cancel_WhenReservationIsNotActive_ShouldThrowBusinessRuleValidationException()
    {
        var reservation = CreateReservation();
        reservation.Expire(WindowEnd);

        Should.Throw<BusinessRuleValidationException>(() => reservation.Cancel(WindowEnd));
        reservation.Status.ShouldBe(ReservationStatus.Expired);
    }

    private static Reservation CreateReservation()
    {
        return Reservation.CreateNew(Guid.NewGuid(), StartDate, Duration, Now, Guid.NewGuid());
    }
}

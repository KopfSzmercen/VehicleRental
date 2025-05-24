using VehicleRental.Rentals.Domain.Reservations;

namespace VehicleTests.Tests.Unit.Rentals.Domain;

public class ReservationTests
{
    [Fact]
    public void CreateNew_ShouldThrowException_WhenStartDateIsInThePast()
    {
        // Arrange
        var vehicleId = Guid.NewGuid();
        var startDate = new DateTimeOffset(2023, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var duration = new ReservationDurationInSeconds(3600);
        var now = new DateTimeOffset(2023, 1, 2, 0, 0, 0, TimeSpan.Zero);
        var userId = Guid.NewGuid();

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            Reservation.CreateNew(vehicleId, startDate, duration, now, userId));
    }

    [Fact]
    public void CreateNew_ShouldCreateReservation_WhenValidParametersAreProvided()
    {
        // Arrange
        var vehicleId = Guid.NewGuid();
        var startDate = new DateTimeOffset(2023, 1, 2, 0, 0, 0, TimeSpan.Zero);
        var duration = new ReservationDurationInSeconds(3600);
        var now = new DateTimeOffset(2023, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var userId = Guid.NewGuid();

        // Act
        var reservation = Reservation.CreateNew(vehicleId, startDate, duration, now, userId);

        // Assert
        Assert.NotNull(reservation);
        Assert.Equal(vehicleId, reservation.VehicleId);
        Assert.Equal(startDate, reservation.StartDate);
        Assert.Equal(duration, reservation.Duration);
        Assert.Equal(userId, reservation.UserId);
        Assert.True(reservation.IsActive);
        Assert.Equal(vehicleId, reservation.CurrentVehicleId);
    }

    [Fact]
    public void Cancel_ShouldCancelReservation_WhenCalled()
    {
        // Arrange
        var vehicleId = Guid.NewGuid();
        var startDate = new DateTimeOffset(2023, 1, 2, 0, 0, 0, TimeSpan.Zero);
        var duration = new ReservationDurationInSeconds(3600);
        var now = new DateTimeOffset(2023, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var userId = Guid.NewGuid();
        var reservation = Reservation.CreateNew(vehicleId, startDate, duration, now, userId);

        // Act
        reservation.Cancel(now);

        // Assert
        Assert.False(reservation.IsActive);
        Assert.NotNull(reservation.CancelledAt);
        Assert.Null(reservation.CurrentVehicleId);
    }

    [Fact]
    public void Invalidate_ShouldInvalidateReservation_WhenCalled()
    {
        // Arrange
        var vehicleId = Guid.NewGuid();
        var startDate = new DateTimeOffset(2023, 1, 2, 0, 0, 0, TimeSpan.Zero);
        var duration = new ReservationDurationInSeconds(3600);
        var now = new DateTimeOffset(2023, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var userId = Guid.NewGuid();
        var reservation = Reservation.CreateNew(vehicleId, startDate, duration, now, userId);

        // Act
        reservation.Invalidate(now);

        // Assert
        Assert.False(reservation.IsActive);
        Assert.Null(reservation.CurrentVehicleId);
    }
}
using VehicleRental.Rentals.Domain;
using VehicleRental.Rentals.Domain.Reservations;

namespace VehicleTests.Tests.Unit.Rentals.Domain;

public class ReservationDurationInSecondsTests
{
    [Fact]
    public void Constructor_ShouldThrowException_WhenValueIsNegative()
    {
        // Arrange
        const int negativeValue = -1;

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ReservationDurationInSeconds(negativeValue));
    }

    [Fact]
    public void Constructor_ShouldThrowException_WhenValueIsLessThan60()
    {
        // Arrange
        const int lessThan60 = 59;

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ReservationDurationInSeconds(lessThan60));
    }

    [Fact]
    public void Constructor_ShouldThrowException_WhenValueIsGreaterThan86400()
    {
        // Arrange
        const int greaterThan86400 = 86401;

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ReservationDurationInSeconds(greaterThan86400));
    }
}
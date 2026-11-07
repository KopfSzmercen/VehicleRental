using Shouldly;
using VehicleRental.Common.ErrorHandling;
using VehicleRental.Rentals.Domain;

namespace VehicleTests.Tests.Unit.Rentals.Domain;

public class RentalTests
{
    [Fact]
    public void Constructor_ShouldThrowException_WhenRentalPeriodIsInvalid()
    {
        // Arrange
        var startDate = new DateTimeOffset(2023, 10, 1, 0, 0, 0, TimeSpan.Zero);
        var endDate = new DateTimeOffset(2023, 10, 1, 0, 0, 0, TimeSpan.Zero);
        var now = new DateTimeOffset(2023, 10, 1, 0, 0, 0, TimeSpan.Zero);


        var vehicleId = Guid.NewGuid();
        var customerId = Guid.NewGuid();

        var userWalletBalance = new Money(1000, Currency.USD);

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            Rental.CreateNew(vehicleId, customerId, startDate, endDate, userWalletBalance, now));
    }

    [Fact]
    public void Constructor_ShouldThrowException_WhenStartDateIsInThePast()
    {
        // Arrange
        var startDate = new DateTimeOffset(2023, 10, 1, 0, 0, 0, TimeSpan.Zero);
        var endDate = new DateTimeOffset(2023, 10, 2, 0, 0, 0, TimeSpan.Zero);
        var now = new DateTimeOffset(2023, 10, 3, 0, 0, 0, TimeSpan.Zero);

        var vehicleId = Guid.NewGuid();
        var customerId = Guid.NewGuid();

        var userWalletBalance = new Money(1000, Currency.USD);

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            Rental.CreateNew(vehicleId, customerId, startDate, endDate, userWalletBalance, now));
    }

    [Fact]
    public void Constructor_ShouldThrowException_WhenRentalDurationIsTooShort()
    {
        // Arrange
        var startDate = new DateTimeOffset(2023, 10, 1, 0, 0, 10, TimeSpan.Zero);
        var endDate = new DateTimeOffset(2023, 10, 1, 0, 0, 20, TimeSpan.Zero);
        var now = new DateTimeOffset(2023, 10, 1, 0, 0, 0, TimeSpan.Zero);

        var vehicleId = Guid.NewGuid();
        var customerId = Guid.NewGuid();

        var userWalletBalance = new Money(1000, Currency.USD);

        // Act & Assert
        Assert.Throws<BusinessRuleValidationException>(() =>
            Rental.CreateNew(vehicleId, customerId, startDate, endDate, userWalletBalance, now));
    }

    [Fact]
    public void CreateNew_ShouldAccept_WhenWalletBalanceEqualsPrice()
    {
        // Arrange
        var now = new DateTimeOffset(2023, 10, 1, 0, 0, 0, TimeSpan.Zero);
        var startDate = now.AddMinutes(5);
        var endDate = startDate.AddHours(24);

        // Act
        var rental = Rental.CreateNew(
            Guid.NewGuid(), Guid.NewGuid(), startDate, endDate, new Money(240, Currency.USD), now);

        // Assert
        rental.Status.ShouldBe(RentalStatus.Active);
    }

    [Fact]
    public void CreateNew_ShouldThrowException_WhenWalletBalanceIsLowerThanPrice()
    {
        // Arrange
        var now = new DateTimeOffset(2023, 10, 1, 0, 0, 0, TimeSpan.Zero);
        var startDate = now.AddMinutes(5);
        var endDate = startDate.AddHours(24);

        // Act & Assert
        Should.Throw<BusinessRuleValidationException>(() =>
                Rental.CreateNew(
                    Guid.NewGuid(), Guid.NewGuid(), startDate, endDate, new Money(239, Currency.USD), now))
            .Message.ShouldBe("Insufficient wallet balance.");
    }

    [Fact]
    public void CreateNew_ShouldReportMinimumDuration_BeforeInsufficientWalletBalance()
    {
        // Arrange
        var now = new DateTimeOffset(2023, 10, 1, 0, 0, 0, TimeSpan.Zero);
        var startDate = now.AddMinutes(5);
        var endDate = startDate.AddSeconds(59);

        // Act & Assert
        Should.Throw<BusinessRuleValidationException>(() =>
                Rental.CreateNew(
                    Guid.NewGuid(), Guid.NewGuid(), startDate, endDate, new Money(0, Currency.USD), now))
            .Message.ShouldBe("Rental duration must be at least 60 seconds.");
    }

    [Theory]
    [InlineData(60, 10)]
    [InlineData(3600, 10)]
    [InlineData(3660, 20)]
    [InlineData(86400, 240)]
    public void CreateNew_ShouldPriceRentalAtTenUsdPerStartedHour(int durationInSeconds, int expectedPrice)
    {
        // Arrange
        var now = new DateTimeOffset(2023, 10, 1, 0, 0, 0, TimeSpan.Zero);
        var startDate = now.AddMinutes(5);
        var endDate = startDate.AddSeconds(durationInSeconds);

        // Act
        var rental = Rental.CreateNew(
            Guid.NewGuid(), Guid.NewGuid(), startDate, endDate, new Money(1000, Currency.USD), now);

        // Assert
        rental.Price.ShouldBe(new Money(expectedPrice, Currency.USD));
    }

    [Fact]
    public void Complete_ShouldThrowException_WhenRentalIsAlreadyCompleted()
    {
        // Arrange
        var startDate = new DateTimeOffset(2023, 10, 1, 0, 0, 0, TimeSpan.Zero);
        var endDate = new DateTimeOffset(2023, 10, 2, 0, 0, 0, TimeSpan.Zero);
        var now = new DateTimeOffset(2023, 10, 1, 0, 0, 0, TimeSpan.Zero);

        var vehicleId = Guid.NewGuid();
        var customerId = Guid.NewGuid();

        var userWalletBalance = new Money(1000, Currency.USD);

        var rental = Rental.CreateNew(vehicleId, customerId, startDate, endDate, userWalletBalance, now);
        rental.Complete(now);

        // Act & Assert
        Assert.Throws<BusinessRuleValidationException>(() => rental.Complete(now));
    }

    [Fact]
    public void Complete_ShouldSetCompletedAt_WhenCalled()
    {
        // Arrange
        var startDate = new DateTimeOffset(2023, 10, 1, 0, 0, 0, TimeSpan.Zero);
        var endDate = new DateTimeOffset(2023, 10, 2, 0, 0, 0, TimeSpan.Zero);
        var now = new DateTimeOffset(2023, 10, 1, 0, 0, 0, TimeSpan.Zero);

        var vehicleId = Guid.NewGuid();
        var customerId = Guid.NewGuid();

        var userWalletBalance = new Money(1000, Currency.USD);

        var rental = Rental.CreateNew(vehicleId, customerId, startDate, endDate, userWalletBalance, now);

        // Act
        rental.Complete(now);

        // Assert
        Assert.NotNull(rental.CompletedAt);
        rental.CurrentVehicleId.ShouldBeNull();
        rental.Status.ShouldBe(RentalStatus.Completed);
    }

    [Fact]
    public void Cancel_ShouldSetCancelledAt_WhenCalled()
    {
        // Arrange
        var startDate = new DateTimeOffset(2023, 10, 1, 0, 0, 0, TimeSpan.Zero);
        var endDate = new DateTimeOffset(2023, 10, 2, 0, 0, 0, TimeSpan.Zero);
        var now = new DateTimeOffset(2023, 10, 1, 0, 0, 0, TimeSpan.Zero);

        var vehicleId = Guid.NewGuid();
        var customerId = Guid.NewGuid();

        var userWalletBalance = new Money(1000, Currency.USD);

        var rental = Rental.CreateNew(vehicleId, customerId, startDate, endDate, userWalletBalance, now);

        // Act
        rental.Cancel(now);

        // Assert
        Assert.NotNull(rental.CancelledAt);
        rental.Status.ShouldBe(RentalStatus.Cancelled);
        rental.CurrentVehicleId.ShouldBeNull();
    }

    [Fact]
    public void CreateNew_ShouldSetStatusToActive_WhenCalled()
    {
        // Arrange
        var startDate = new DateTimeOffset(2023, 10, 1, 0, 0, 0, TimeSpan.Zero);
        var endDate = new DateTimeOffset(2023, 10, 2, 0, 0, 0, TimeSpan.Zero);
        var now = new DateTimeOffset(2023, 10, 1, 0, 0, 0, TimeSpan.Zero);

        var vehicleId = Guid.NewGuid();
        var customerId = Guid.NewGuid();

        var userWalletBalance = new Money(1000, Currency.USD);

        // Act
        var rental = Rental.CreateNew(vehicleId, customerId, startDate, endDate, userWalletBalance, now);

        // Assert
        rental.Status.ShouldBe(RentalStatus.Active);
        rental.VehicleId.ShouldBe(vehicleId);
        rental.CustomerId.ShouldBe(customerId);
        rental.StartDate.ShouldBe(startDate);
        rental.EndDate.ShouldBe(endDate);
        rental.CreatedAt.ShouldBe(now);
        rental.CurrentVehicleId.ShouldBe(vehicleId);
    }
}
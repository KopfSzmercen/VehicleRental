using Shouldly;
using VehicleRental.Common.ErrorHandling;
using VehicleRental.Rentals.Domain;
using VehicleRental.Rentals.Domain.Payments;

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
            Rental.CreateNew(vehicleId, customerId, startDate, endDate, userWalletBalance, customerHasUnpaidRentals: false, now));
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
            Rental.CreateNew(vehicleId, customerId, startDate, endDate, userWalletBalance, customerHasUnpaidRentals: false, now));
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
            Rental.CreateNew(vehicleId, customerId, startDate, endDate, userWalletBalance, customerHasUnpaidRentals: false, now));
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
            Guid.NewGuid(), Guid.NewGuid(), startDate, endDate, new Money(240, Currency.USD), customerHasUnpaidRentals: false, now);

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
                    Guid.NewGuid(), Guid.NewGuid(), startDate, endDate, new Money(239, Currency.USD), customerHasUnpaidRentals: false, now))
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
                    Guid.NewGuid(), Guid.NewGuid(), startDate, endDate, new Money(0, Currency.USD), customerHasUnpaidRentals: false, now))
            .Message.ShouldBe("Rental duration must be at least 60 seconds.");
    }

    [Fact]
    public void CreateNew_ShouldThrowException_WhenCustomerHasUnpaidRentals()
    {
        // Arrange
        var now = new DateTimeOffset(2023, 10, 1, 0, 0, 0, TimeSpan.Zero);
        var startDate = now.AddMinutes(5);
        var endDate = startDate.AddHours(1);

        // Act & Assert
        Should.Throw<BusinessRuleValidationException>(() =>
                Rental.CreateNew(
                    Guid.NewGuid(), Guid.NewGuid(), startDate, endDate, new Money(1000, Currency.USD),
                    customerHasUnpaidRentals: true, now))
            .Message.ShouldBe("Customer has unpaid rentals.");
    }

    [Fact]
    public void CreateNew_ShouldReportUnpaidRentals_BeforeInsufficientWalletBalance()
    {
        // Arrange
        var now = new DateTimeOffset(2023, 10, 1, 0, 0, 0, TimeSpan.Zero);
        var startDate = now.AddMinutes(5);
        var endDate = startDate.AddHours(1);

        // Act & Assert
        Should.Throw<BusinessRuleValidationException>(() =>
                Rental.CreateNew(
                    Guid.NewGuid(), Guid.NewGuid(), startDate, endDate, new Money(0, Currency.USD),
                    customerHasUnpaidRentals: true, now))
            .Message.ShouldBe("Customer has unpaid rentals.");
    }

    [Fact]
    public void CreateNew_ShouldReportMinimumDuration_BeforeUnpaidRentals()
    {
        // Arrange
        var now = new DateTimeOffset(2023, 10, 1, 0, 0, 0, TimeSpan.Zero);
        var startDate = now.AddMinutes(5);
        var endDate = startDate.AddSeconds(59);

        // Act & Assert
        Should.Throw<BusinessRuleValidationException>(() =>
                Rental.CreateNew(
                    Guid.NewGuid(), Guid.NewGuid(), startDate, endDate, new Money(1000, Currency.USD),
                    customerHasUnpaidRentals: true, now))
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
            Guid.NewGuid(), Guid.NewGuid(), startDate, endDate, new Money(1000, Currency.USD), customerHasUnpaidRentals: false, now);

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

        var rental = Rental.CreateNew(vehicleId, customerId, startDate, endDate, userWalletBalance, customerHasUnpaidRentals: false, now);
        rental.Complete(now, ChargeResult.Succeeded);

        // Act & Assert
        Assert.Throws<BusinessRuleValidationException>(() => rental.Complete(now, ChargeResult.Succeeded));
    }

    [Fact]
    public void Complete_ShouldCompleteAsPaidAndDetachFromVehicle_WhenChargeSucceeded()
    {
        // Arrange
        var rental = ActiveRental();
        var finishedAt = CreatedAt.AddHours(3);

        // Act
        rental.Complete(finishedAt, ChargeResult.Succeeded);

        // Assert
        rental.Status.ShouldBe(RentalStatus.Completed);
        rental.CompletedAt.ShouldBe(finishedAt);
        rental.PaymentStatus.ShouldBe(PaymentStatus.Paid);
        rental.PaidAt.ShouldBe(finishedAt);
        rental.CurrentVehicleId.ShouldBeNull();
    }

    [Fact]
    public void Complete_ShouldCompleteAsUnpaidAndDetachFromVehicle_WhenChargeDeclined()
    {
        // Arrange
        var rental = ActiveRental();
        var finishedAt = CreatedAt.AddHours(3);

        // Act
        rental.Complete(finishedAt, ChargeResult.Declined);

        // Assert
        rental.Status.ShouldBe(RentalStatus.Completed);
        rental.CompletedAt.ShouldBe(finishedAt);
        rental.PaymentStatus.ShouldBe(PaymentStatus.Unpaid);
        rental.PaidAt.ShouldBeNull();
        rental.CurrentVehicleId.ShouldBeNull();
    }

    [Fact]
    public void HasOutstandingPayment_ShouldBeTrue_WhenCompletedAndUnpaid()
    {
        var rental = ActiveRental();

        rental.Complete(CreatedAt.AddHours(3), ChargeResult.Declined);

        rental.HasOutstandingPayment.ShouldBeTrue();
    }

    [Fact]
    public void HasOutstandingPayment_ShouldBeFalse_WhenCompletedAndPaid()
    {
        var rental = ActiveRental();

        rental.Complete(CreatedAt.AddHours(3), ChargeResult.Succeeded);

        rental.HasOutstandingPayment.ShouldBeFalse();
    }

    [Fact]
    public void HasOutstandingPayment_ShouldBeFalse_WhenActive()
    {
        var rental = ActiveRental();

        rental.HasOutstandingPayment.ShouldBeFalse();
    }

    [Fact]
    public void HasOutstandingPayment_ShouldBeFalse_WhenCancelled()
    {
        var rental = ActiveRental();

        rental.Cancel(CreatedAt.AddMinutes(30));

        rental.HasOutstandingPayment.ShouldBeFalse();
        rental.PaymentStatus.ShouldBeNull();
    }

    [Fact]
    public void SettlePayment_ShouldMarkRentalPaid_WhenUnpaidAndChargeSucceeded()
    {
        // Arrange
        var rental = ActiveRental();
        rental.Complete(CreatedAt.AddHours(3), ChargeResult.Declined);
        var settledAt = CreatedAt.AddDays(2);

        // Act
        rental.SettlePayment(ChargeResult.Succeeded, settledAt);

        // Assert
        rental.Status.ShouldBe(RentalStatus.Completed);
        rental.PaymentStatus.ShouldBe(PaymentStatus.Paid);
        rental.PaidAt.ShouldBe(settledAt);
        rental.HasOutstandingPayment.ShouldBeFalse();
    }

    [Fact]
    public void SettlePayment_ShouldThrowAndLeaveRentalUnpaid_WhenUnpaidAndChargeDeclined()
    {
        // Arrange
        var rental = ActiveRental();
        rental.Complete(CreatedAt.AddHours(3), ChargeResult.Declined);

        // Act & Assert
        Should.Throw<BusinessRuleValidationException>(() =>
                rental.SettlePayment(ChargeResult.Declined, CreatedAt.AddDays(2)))
            .Message.ShouldBe("Payment declined.");

        rental.PaymentStatus.ShouldBe(PaymentStatus.Unpaid);
        rental.PaidAt.ShouldBeNull();
        rental.HasOutstandingPayment.ShouldBeTrue();
    }

    [Fact]
    public void SettlePayment_ShouldThrowNoOutstandingPayment_WhenAlreadyPaid()
    {
        // Arrange
        var rental = ActiveRental();
        var completedAt = CreatedAt.AddHours(3);
        rental.Complete(completedAt, ChargeResult.Succeeded);

        // Act & Assert
        Should.Throw<BusinessRuleValidationException>(() =>
                rental.SettlePayment(ChargeResult.Succeeded, CreatedAt.AddDays(2)))
            .Message.ShouldBe("Rental has no outstanding payment.");

        rental.PaidAt.ShouldBe(completedAt);
    }

    [Fact]
    public void SettlePayment_ShouldThrowNoOutstandingPayment_WhenActive()
    {
        // Arrange
        var rental = ActiveRental();

        // Act & Assert
        Should.Throw<BusinessRuleValidationException>(() =>
                rental.SettlePayment(ChargeResult.Succeeded, CreatedAt.AddDays(2)))
            .Message.ShouldBe("Rental has no outstanding payment.");

        rental.Status.ShouldBe(RentalStatus.Active);
        rental.PaymentStatus.ShouldBeNull();
    }

    private static readonly DateTimeOffset CreatedAt = new(2023, 10, 1, 0, 0, 0, TimeSpan.Zero);

    private static Rental ActiveRental()
    {
        return Rental.CreateNew(
            Guid.NewGuid(),
            Guid.NewGuid(),
            CreatedAt.AddHours(1),
            CreatedAt.AddHours(2),
            new Money(1000, Currency.USD),
            customerHasUnpaidRentals: false,
            CreatedAt
        );
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

        var rental = Rental.CreateNew(vehicleId, customerId, startDate, endDate, userWalletBalance, customerHasUnpaidRentals: false, now);

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
        var rental = Rental.CreateNew(vehicleId, customerId, startDate, endDate, userWalletBalance, customerHasUnpaidRentals: false, now);

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
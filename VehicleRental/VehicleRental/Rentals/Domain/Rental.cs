using VehicleRental.Common.ErrorHandling;
using VehicleRental.Rentals.Domain.Payments;

namespace VehicleRental.Rentals.Domain;

internal sealed class Rental
{
    private const int MinimumRentalDurationInSeconds = 60;
    private const int PricePerStartedHourInUsd = 10;

    private Rental()
    {
    }

    public Guid Id { get; private init; }

    public Guid? CurrentVehicleId { get; private set; }

    public Guid VehicleId { get; private init; }

    public Guid CustomerId { get; private init; }

    public DateTimeOffset StartDate { get; private init; }

    public DateTimeOffset EndDate { get; private init; }

    public DateTimeOffset CreatedAt { get; private init; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public DateTimeOffset? CancelledAt { get; private set; }

    public RentalStatus Status { get; private set; }

    public Money Price { get; private init; } = null!;

    public PaymentStatus? PaymentStatus { get; private set; }

    public DateTimeOffset? PaidAt { get; private set; }

    // Outstanding payment ("należność"): the price of a completed rental whose charge was declined.
    public bool HasOutstandingPayment =>
        Status == RentalStatus.Completed && PaymentStatus == Domain.PaymentStatus.Unpaid;

    public static Rental CreateNew(
        Guid vehicleId,
        Guid customerId,
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        Money walletBalance,
        DateTimeOffset now
    )
    {
        if (startDate >= endDate)
            throw new ArgumentException("Start date must be before end date.");

        if (startDate < now)
            throw new ArgumentException("Start date must be in the future.");

        if (endDate - startDate < TimeSpan.FromSeconds(MinimumRentalDurationInSeconds))
            throw new BusinessRuleValidationException(
                $"Rental duration must be at least {MinimumRentalDurationInSeconds} seconds.");

        var price = PriceFor(startDate, endDate);

        if (walletBalance < price)
            throw new BusinessRuleValidationException("Insufficient wallet balance.");

        return new Rental
        {
            Id = Guid.NewGuid(),
            VehicleId = vehicleId,
            CustomerId = customerId,
            StartDate = startDate,
            EndDate = endDate,
            CreatedAt = now,
            Status = RentalStatus.Active,
            CurrentVehicleId = vehicleId,
            Price = price
        };
    }

    // 10 USD per started hour of the booked duration [startDate, endDate).
    private static Money PriceFor(DateTimeOffset startDate, DateTimeOffset endDate)
    {
        var durationInTicks = (endDate - startDate).Ticks;
        var startedHours = (durationInTicks + TimeSpan.TicksPerHour - 1) / TimeSpan.TicksPerHour;

        return new Money((int)startedHours * PricePerStartedHourInUsd, Currency.USD);
    }

    public void Complete(DateTimeOffset now, ChargeResult chargeResult)
    {
        if (Status != RentalStatus.Active)
            throw new BusinessRuleValidationException("Rental is not active.");

        Status = RentalStatus.Completed;

        if (chargeResult == ChargeResult.Succeeded)
        {
            PaymentStatus = Domain.PaymentStatus.Paid;
            PaidAt = now;
        }
        else
        {
            PaymentStatus = Domain.PaymentStatus.Unpaid;
        }

        CompletedAt = now;
        CurrentVehicleId = null;
    }

    public void Cancel(DateTimeOffset now)
    {
        if (Status != RentalStatus.Active)
            throw new BusinessRuleValidationException("Rental is not active.");

        Status = RentalStatus.Cancelled;

        CancelledAt = now;
        CurrentVehicleId = null;
    }
}
using VehicleRental.Common.ErrorHandling;

namespace VehicleRental.Rentals.Domain;

internal sealed class Rental
{
    private const int MinimumRentalDurationInSeconds = 60;
    private const int MinimumWalletBalance = 10;

    private Rental()
    {
    }

    public Guid Id { get; private init; }

    public Guid VehicleId { get; private init; }

    public Guid CustomerId { get; private init; }

    public DateTimeOffset StartDate { get; private init; }

    public DateTimeOffset EndDate { get; private init; }

    public DateTimeOffset CreatedAt { get; private init; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public DateTimeOffset? CancelledAt { get; private set; }

    public RentalStatus Status { get; private set; }

    public static Rental CreateNew(
        Guid vehicleId,
        Guid customerId,
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        Money userWalletBalance,
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

        if (userWalletBalance.Amount < MinimumWalletBalance)
            throw new BusinessRuleValidationException($"User wallet balance must be at least {MinimumWalletBalance}.");

        return new Rental
        {
            Id = Guid.NewGuid(),
            VehicleId = vehicleId,
            CustomerId = customerId,
            StartDate = startDate,
            EndDate = endDate,
            CreatedAt = now,
            Status = RentalStatus.Active
        };
    }

    public void Complete(DateTimeOffset now)
    {
        if (Status != RentalStatus.Active)
            throw new BusinessRuleValidationException("Rental is not active.");

        Status = RentalStatus.Completed;

        CompletedAt = now;
    }

    public void Cancel(DateTimeOffset now)
    {
        if (Status != RentalStatus.Active)
            throw new BusinessRuleValidationException("Rental is not active.");

        Status = RentalStatus.Cancelled;

        CancelledAt = now;
    }
}
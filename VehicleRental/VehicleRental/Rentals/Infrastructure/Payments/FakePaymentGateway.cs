using System.Collections.Concurrent;
using VehicleRental.Rentals.Domain;
using VehicleRental.Rentals.Domain.Payments;

namespace VehicleRental.Rentals.Infrastructure.Payments;

// The only production implementation of the payment gateway: a mock of the external system.
// Every call draws a fresh random wallet balance between 0 and 200 USD. It never throws.
internal sealed class FakePaymentGateway : IPaymentGateway
{
    private const int MaxBalanceInUsd = 200;

    private readonly ConcurrentDictionary<Guid, SuccessfulCharge> _successfulCharges = new();

    public Task<Money> GetBalanceAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(DrawBalance());
    }

    public Task<ChargeResult> ChargeAsync(
        Guid customerId,
        Money amount,
        Guid idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        if (_successfulCharges.ContainsKey(idempotencyKey))
            return Task.FromResult(ChargeResult.Succeeded);

        if (DrawBalance() < amount)
            return Task.FromResult(ChargeResult.Declined);

        _successfulCharges.TryAdd(idempotencyKey, new SuccessfulCharge(customerId, amount));

        return Task.FromResult(ChargeResult.Succeeded);
    }

    private static Money DrawBalance()
    {
        return new Money(Random.Shared.Next(0, MaxBalanceInUsd + 1), Currency.USD);
    }

    private sealed record SuccessfulCharge(Guid CustomerId, Money Amount);
}

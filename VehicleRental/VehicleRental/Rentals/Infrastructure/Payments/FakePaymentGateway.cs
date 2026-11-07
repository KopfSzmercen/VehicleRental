using System.Collections.Concurrent;
using VehicleRental.Rentals.Domain;
using VehicleRental.Rentals.Domain.Payments;

namespace VehicleRental.Rentals.Infrastructure.Payments;

// The only production implementation of the payment gateway: a mock of the external system.
// Every call draws a fresh random wallet balance between 0 and 200 USD. It never throws.
internal sealed class FakePaymentGateway : IPaymentGateway
{
    private const int MaxBalanceInUsd = 200;

    private readonly ConcurrentDictionary<Guid, IdempotencyKey> _idempotencyKeys = new();

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
        var key = _idempotencyKeys.GetOrAdd(idempotencyKey, _ => new IdempotencyKey());

        // Check, charge and record atomically per key, so concurrent calls with the same key charge once.
        lock (key)
        {
            if (key.HasSucceeded)
                return Task.FromResult(ChargeResult.Succeeded);

            // A declined charge leaves the key unused, so it can be retried.
            if (DrawBalance() < amount)
                return Task.FromResult(ChargeResult.Declined);

            key.HasSucceeded = true;

            return Task.FromResult(ChargeResult.Succeeded);
        }
    }

    private static Money DrawBalance()
    {
        return new Money(Random.Shared.Next(0, MaxBalanceInUsd + 1), Currency.USD);
    }

    private sealed class IdempotencyKey
    {
        public bool HasSucceeded { get; set; }
    }
}

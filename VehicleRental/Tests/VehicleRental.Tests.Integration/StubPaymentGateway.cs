using System.Collections.Concurrent;
using VehicleRental.Common.ErrorHandling;
using VehicleRental.Rentals.Domain;
using VehicleRental.Rentals.Domain.Payments;

namespace VehicleRental.Tests.Integration;

// Deterministic, controllable replacement for FakePaymentGateway. Registered as a singleton by
// TestWebApplication and reset together with the database.
internal sealed class StubPaymentGateway : IPaymentGateway
{
    private static readonly Money DefaultBalance = new(1_000_000, Currency.USD);

    private readonly ConcurrentDictionary<Guid, IdempotencyKey> _idempotencyKeys = new();

    // Wallet balance returned by GetBalanceAsync. High by default, so it covers any rental.
    public Money Balance { get; set; } = DefaultBalance;

    // Result of a charge whose idempotency key hasn't succeeded yet.
    public ChargeResult ChargeResult { get; set; } = ChargeResult.Succeeded;

    // When true, every call throws PaymentGatewayUnavailableException.
    public bool IsUnavailable { get; set; }

    // Runs inside ChargeAsync, before the charge is decided. Use it to simulate a concurrent change.
    public Func<Task>? OnCharge { get; set; }

    // Every charge that actually charged the customer. Repeated keys and declined charges aren't recorded.
    public IReadOnlyCollection<RecordedCharge> Charges =>
        _idempotencyKeys.Values
            .Select(key => key.Charge)
            .OfType<RecordedCharge>()
            .ToList();

    public Task<Money> GetBalanceAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        if (IsUnavailable) throw new PaymentGatewayUnavailableException();

        return Task.FromResult(Balance);
    }

    public async Task<ChargeResult> ChargeAsync(
        Guid customerId,
        Money amount,
        Guid idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        if (IsUnavailable) throw new PaymentGatewayUnavailableException();

        if (OnCharge is not null) await OnCharge();

        var key = _idempotencyKeys.GetOrAdd(idempotencyKey, _ => new IdempotencyKey());

        // Check, charge and record atomically per key, so concurrent calls with the same key charge once.
        lock (key)
        {
            if (key.Charge is not null) return ChargeResult.Succeeded;

            if (ChargeResult == ChargeResult.Declined) return ChargeResult.Declined;

            key.Charge = new RecordedCharge(customerId, amount, idempotencyKey);

            return ChargeResult.Succeeded;
        }
    }

    public void Reset()
    {
        Balance = DefaultBalance;
        ChargeResult = ChargeResult.Succeeded;
        IsUnavailable = false;
        OnCharge = null;
        _idempotencyKeys.Clear();
    }

    internal sealed record RecordedCharge(Guid CustomerId, Money Amount, Guid IdempotencyKey);

    private sealed class IdempotencyKey
    {
        public RecordedCharge? Charge { get; set; }
    }
}

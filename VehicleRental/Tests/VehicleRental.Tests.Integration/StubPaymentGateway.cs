using System.Collections.Concurrent;
using VehicleRental.Rentals.Domain;
using VehicleRental.Rentals.Domain.Payments;

namespace VehicleRental.Tests.Integration;

// Deterministic, controllable replacement for FakePaymentGateway. Registered as a singleton by
// TestWebApplication and reset together with the database.
internal sealed class StubPaymentGateway : IPaymentGateway
{
    private static readonly Money DefaultBalance = new(1_000_000, Currency.USD);

    private readonly ConcurrentDictionary<Guid, RecordedCharge> _charges = new();

    // Wallet balance returned by GetBalanceAsync. High by default, so it covers any rental.
    public Money Balance { get; set; } = DefaultBalance;

    // Result of a charge whose idempotency key hasn't succeeded yet.
    public ChargeResult ChargeResult { get; set; } = ChargeResult.Succeeded;

    // When true, every call throws PaymentGatewayUnavailableException.
    public bool IsUnavailable { get; set; }

    // Runs inside ChargeAsync, before the charge is decided. Use it to simulate a concurrent change.
    public Func<Task>? OnCharge { get; set; }

    // Every charge that actually charged the customer. Repeated keys and declined charges aren't recorded.
    public IReadOnlyCollection<RecordedCharge> Charges => _charges.Values.ToList();

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

        if (_charges.ContainsKey(idempotencyKey)) return ChargeResult.Succeeded;

        if (ChargeResult == ChargeResult.Declined) return ChargeResult.Declined;

        _charges.TryAdd(idempotencyKey, new RecordedCharge(customerId, amount, idempotencyKey));

        return ChargeResult.Succeeded;
    }

    public void Reset()
    {
        Balance = DefaultBalance;
        ChargeResult = ChargeResult.Succeeded;
        IsUnavailable = false;
        OnCharge = null;
        _charges.Clear();
    }

    internal sealed record RecordedCharge(Guid CustomerId, Money Amount, Guid IdempotencyKey);
}

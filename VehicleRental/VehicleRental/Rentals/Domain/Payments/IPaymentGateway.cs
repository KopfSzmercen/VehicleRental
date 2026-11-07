namespace VehicleRental.Rentals.Domain.Payments;

// Port to the external payment gateway, owned by the Rentals module and expressed in its own types,
// so the external system's model never reaches the domain.
// Implementations throw PaymentGatewayUnavailableException when the gateway can't be reached.
internal interface IPaymentGateway
{
    Task<Money> GetBalanceAsync(Guid customerId, CancellationToken cancellationToken = default);

    // A repeated call with an idempotency key that already succeeded returns Succeeded without charging again.
    // A declined charge doesn't use up the key, so it can be retried.
    Task<ChargeResult> ChargeAsync(
        Guid customerId,
        Money amount,
        Guid idempotencyKey,
        CancellationToken cancellationToken = default);
}

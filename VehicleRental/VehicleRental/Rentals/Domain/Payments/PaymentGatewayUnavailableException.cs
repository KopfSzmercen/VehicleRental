namespace VehicleRental.Rentals.Domain.Payments;

internal sealed class PaymentGatewayUnavailableException() : Exception("Payment gateway is unavailable.");

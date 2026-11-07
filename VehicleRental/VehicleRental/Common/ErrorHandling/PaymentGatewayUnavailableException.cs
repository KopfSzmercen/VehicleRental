namespace VehicleRental.Common.ErrorHandling;

internal sealed class PaymentGatewayUnavailableException() : Exception("Payment gateway is unavailable.");

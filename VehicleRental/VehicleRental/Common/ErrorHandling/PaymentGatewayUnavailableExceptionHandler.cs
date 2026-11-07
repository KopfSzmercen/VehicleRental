using Microsoft.AspNetCore.Diagnostics;

namespace VehicleRental.Common.ErrorHandling;

internal sealed class PaymentGatewayUnavailableExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not PaymentGatewayUnavailableException) return false;

        httpContext.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        await httpContext.Response.WriteAsJsonAsync("Payment gateway is unavailable.", cancellationToken);

        return true;
    }
}

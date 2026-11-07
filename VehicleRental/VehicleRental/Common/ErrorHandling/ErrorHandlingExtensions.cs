namespace VehicleRental.Common.ErrorHandling;

internal static class ErrorHandlingExtensions
{
    public static IServiceCollection AddErrorHandling(this IServiceCollection services)
    {
        services.AddExceptionHandler<BusinessRuleValidationExceptionHandler>();
        services.AddProblemDetails();

        return services;
    }

    public static WebApplication UseErrorHandling(this WebApplication app)
    {
        app.UseExceptionHandler();
        return app;
    }
}

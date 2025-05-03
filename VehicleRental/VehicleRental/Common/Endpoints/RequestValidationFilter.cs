using FluentValidation;

namespace VehicleRental.Common.Endpoints;

public class RequestValidationFilter<TRequest> : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var validator = context.HttpContext.RequestServices.GetService<IValidator<TRequest>>();

        if (validator is null) return await next(context);

        var request = context.Arguments.OfType<TRequest>().First();
        var validationResult = await validator.ValidateAsync(request, context.HttpContext.RequestAborted);
        if (!validationResult.IsValid) return TypedResults.ValidationProblem(validationResult.ToDictionary());

        return await next(context);
    }
}
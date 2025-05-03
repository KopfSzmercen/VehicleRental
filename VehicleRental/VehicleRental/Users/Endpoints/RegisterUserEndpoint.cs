using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using VehicleRental.Common.Endpoints;
using VehicleRental.Users.Domain;

namespace VehicleRental.Users.Endpoints;

internal class RegisterUserEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("", Handle)
            .WithRequestValidation<Request>()
            .WithSummary("Register a new user");
    }

    private static async Task<Results<Ok<Guid>, BadRequest<string>>> Handle(
        [FromServices] UserManager<User> userManager,
        [FromBody] Request request,
        CancellationToken cancellationToken)
    {
        var user = User.CreateNormalUser(request.Email);

        var result = await userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            var errors = result.Errors.ToDictionary(e => e.Code, e => e.Description);
            return TypedResults.BadRequest(string.Join(",", errors.Values.ToList()));
        }

        await userManager.AddToRolesAsync(user, [UserRole.User]);

        await userManager.AddClaimsAsync(user, [
            new Claim("UserId", user.Id.ToString()),
            new Claim("SendEmails", true.ToString())
        ]);

        return TypedResults.Ok(user.Id);
    }

    internal sealed record Request
    {
        public string Email { get; init; } = null!;
        public string Password { get; init; } = null!;
    }

    internal sealed class RequestValidator : AbstractValidator<Request>
    {
        public RequestValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .EmailAddress();

            RuleFor(x => x.Password)
                .NotEmpty();
        }
    }
}
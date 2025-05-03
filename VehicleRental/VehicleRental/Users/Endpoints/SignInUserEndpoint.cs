using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using VehicleRental.Common.Endpoints;
using VehicleRental.Users.Domain;
using VehicleRental.Users.Infrastructure.Tokens;

namespace VehicleRental.Users.Endpoints;

internal sealed class SignInUserndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("sign-in", Handle)
            .WithRequestValidation<RequestValidator>()
            .WithSummary("Sign in user and return JWT token");
    }

    private static async Task<Results<
        BadRequest<string>,
        Ok<Response>
    >> Handle(
        [FromServices] SignInManager<User> signInManager,
        [FromServices] UserManager<User> userManager,
        [FromServices] ITokensManager tokensManager,
        [FromBody] Reuqest request)
    {
        var user = await userManager.FindByEmailAsync(request.Email);

        if (user is null)
            return TypedResults.BadRequest("User not found");

        var signInResult = await signInManager
            .PasswordSignInAsync(user, request.Password, false, false);

        if (signInResult.IsLockedOut)
            return TypedResults.BadRequest("User is locked out");

        if (!signInResult.Succeeded)
            return TypedResults.BadRequest("Invalid password");

        var userRoles = await userManager.GetRolesAsync(user);

        var userClaims = await userManager.GetClaimsAsync(user);

        foreach (var role in userRoles) userClaims.Add(new Claim(ClaimTypes.Role, role));

        var token = tokensManager.CreateToken(
            user.Id,
            userRoles.ToList(),
            [
                ..userClaims.ToList()
            ]
        );

        return TypedResults.Ok(new Response(token.AccessToken, token.RefreshToken));
    }

    public sealed record Response(string Token, string RefreshToken);

    public sealed record Reuqest
    {
        public string Email { get; init; } = null!;
        public string Password { get; init; } = null!;
    }

    private sealed class RequestValidator : AbstractValidator<Reuqest>
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
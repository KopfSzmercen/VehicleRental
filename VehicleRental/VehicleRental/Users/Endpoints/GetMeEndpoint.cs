using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using VehicleRental.Common.Endpoints;
using VehicleRental.Users.Domain;

namespace VehicleRental.Users.Endpoints;

internal class GetMeEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("me", Handle)
            .RequireAuthorization()
            .WithSummary("Gets the current user information");
    }

    private static async Task<Ok<Response>> Handle(
        [FromServices] UserManager<User> userManager,
        [FromServices] IHttpContextAccessor httpContextAccessor
    )
    {
        var userId = httpContextAccessor.HttpContext?.User.FindFirst("UserId")?.Value!;

        var user = await userManager.FindByIdAsync(userId);

        var response = new Response(user!.Id, user.UserName!, user.Email!);

        return TypedResults.Ok(response);
    }

    internal sealed record Response(Guid Id, string Username, string Email);
}
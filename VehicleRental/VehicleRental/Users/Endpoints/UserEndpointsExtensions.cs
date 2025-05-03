using VehicleRental.Common.Endpoints;

namespace VehicleRental.Users.Endpoints;

internal static class UsersEndpointsExtensions
{
    public const string BaseUrl = "users";
    public const string GroupTag = "users";

    public static void AddUsersEndpoint(this WebApplication webApplication)
    {
        var group = webApplication
            .MapGroup(BaseUrl)
            .WithTags(GroupTag);

        group
            .MapEndpoint<RegisterUserEndpoint>()
            .MapEndpoint<SignInUserndpoint>()
            .MapEndpoint<GetMeEndpoint>();
    }
}
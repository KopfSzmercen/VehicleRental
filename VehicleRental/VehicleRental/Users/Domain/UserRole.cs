using Microsoft.AspNetCore.Identity;

namespace VehicleRental.Users.Domain;

internal class UserRole(string name) : IdentityRole<Guid>(name)
{
    private const string Admin = "Admin";
    public const string User = "User";

    public string Name { get; } = name;

    public static IReadOnlyList<string> AvailableRoles =>
    [
        Admin,
        User
    ];
}
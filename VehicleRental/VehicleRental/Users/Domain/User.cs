using Microsoft.AspNetCore.Identity;

namespace VehicleRental.Users.Domain;

internal sealed class User : IdentityUser<Guid>
{
    private User()
    {
    }

    public static User CreateNormalUser(string email)
    {
        return new User
        {
            Email = email,
            UserName = email,
            Id = Guid.NewGuid()
        };
    }
}
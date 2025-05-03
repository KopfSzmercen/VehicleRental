using System.Security.Claims;

namespace VehicleRental.Users.Infrastructure.Tokens;

public interface ITokensManager
{
    JsonWebToken CreateToken(Guid userId, List<string> roles, List<Claim>? claims = null);
}
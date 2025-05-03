namespace VehicleRental.Users.Infrastructure.Tokens;

public sealed class JsonWebToken
{
    public required string AccessToken { get; set; }

    public required string RefreshToken { get; set; }

    public long Expiry { get; set; }

    public Guid UserId { get; set; }
}
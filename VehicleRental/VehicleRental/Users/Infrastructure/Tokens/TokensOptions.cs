using System.ComponentModel.DataAnnotations;

namespace VehicleRental.Users.Infrastructure.Tokens;

public sealed class TokensOptions
{
    public const string SectionName = "Tokens";

    [Required] public string Secret { get; init; } = null!;

    [Required] public string Audience { get; init; } = null!;

    [Required] public string Issuer { get; init; } = null!;
}
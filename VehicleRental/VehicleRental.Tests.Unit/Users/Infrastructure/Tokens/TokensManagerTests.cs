using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using Shouldly;
using VehicleRental.Users.Infrastructure.Tokens;

namespace VehicleTests.Tests.Unit.Users.Infrastructure.Tokens;

public class TokensManagerTests
{
    [Fact]
    public void ItShouldGenerateJwt()
    {
        //Arrange
        var tokensOptions = new TokensOptions
        {
            Audience = "Audience",
            Issuer = "Issuer",
            Secret = "12345678901234567890abcdefghijklmnopqu"
        };

        var sut = new TokensManager(Options.Create(tokensOptions));

        //Act
        var token = sut.CreateToken(
            Guid.NewGuid(),
            ["Admin", "User"],
            [new Claim("Test", "Test")]
        );

        //Assert
        token.ShouldNotBeNull();
        token.AccessToken.ShouldNotBeNullOrEmpty();
        token.RefreshToken.ShouldNotBeNullOrEmpty();

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token.AccessToken);
        jwtToken.ShouldNotBeNull();

        var claims = jwtToken.Claims.ToList();
        claims.ShouldNotBeNull();
        claims.ShouldContain(c => c.Type == ClaimTypes.Role && c.Value == "Admin");
    }
}
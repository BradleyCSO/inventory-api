using InventoryAPI.Models;
using InventoryAPI.Responses;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace InventoryAPI.Services;

public interface ITokenService
{
    /// <summary>
    /// Generates a refresh token that lasts 7 days, persists to db and access token (lasts 1 day) for a user
    /// </summary>
    /// <param name="userId">Stored user id to generate token for</param>
    /// <returns>Generated refresh and access token</returns>
    Task<AuthenticatedUserLoginResponse> GenerateTokens(int userId);

    /// <summary>
    /// Creates access token provided a userId and a value for the Jwt:Secret key from the config
    /// </summary>
    /// <param name="userId">User id to create access token for</param>
    /// <returns>Json Web Token (JWT)</returns>
    /// <exception cref="InvalidOperationException">Throws if the secret key is missing</exception>
    public string CreateAccessToken(int userId);
}

public class TokenService(IRefreshTokenService refreshTokenService, IConfiguration configuration) : ITokenService
{
    public async Task<AuthenticatedUserLoginResponse> GenerateTokens(int userId)
    {
        // Store the refresh token in db and associate it with the user
        var refreshToken = new RefreshToken
        {
            UserId = userId,
            Token = Guid.NewGuid().ToString(),
            Expiration = DateTime.UtcNow.AddDays(7)
        };

        // Insert or update this user's (who we know to be authenticated) refresh token
        await refreshTokenService.InsertUserRefreshTokenAsync(refreshToken);

        return new AuthenticatedUserLoginResponse
        {
            UserId = userId,
            AccessToken = CreateAccessToken(userId),
            RefreshToken = refreshToken.Token,
            RefreshTokenExpiration = refreshToken.Expiration
        };
    }

    public string CreateAccessToken(int userId)
    {
        string? secretKey = configuration["Jwt:SecretKey"] ?? throw new InvalidOperationException("Secret key is missing.");

        var securityTokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity([new Claim("id", userId.ToString())]),
            Expires = DateTime.UtcNow.AddMinutes(60),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secretKey)),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var tokenHandler = new JwtSecurityTokenHandler();

        return tokenHandler.WriteToken(tokenHandler.CreateToken(securityTokenDescriptor));
    }
}
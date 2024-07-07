using InventoryAPI.DbContext;
using InventoryAPI.Models;

namespace InventoryAPI.Services;

public interface IRefreshTokenService
{
    /// <summary>
    /// Inserts user refresh token into refresh token table
    /// </summary>
    /// <param name="refreshToken">Refresh token to insert</param>
    Task InsertUserRefreshTokenAsync(RefreshToken refreshToken);
}

public class RefreshTokenService(ApplicationDbContext context, ILogger<RefreshTokenService> logger) : IRefreshTokenService
{
    public async Task InsertUserRefreshTokenAsync(RefreshToken refreshToken)
    {
        try
        {
            context.RefreshTokens.Add(refreshToken);
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error inserting user refresh token for user with id {UserId} into user_refresh tokens table", refreshToken.UserId);
        }
    }
}
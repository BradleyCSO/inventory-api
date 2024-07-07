using InventoryAPI.DbContext;
using InventoryAPI.Models;
using InventoryAPI.Requests;
using InventoryAPI.Responses;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace InventoryAPI.Services;

public interface IUserService
{
    /// <summary>
    /// Creates a user and adds them to the database, hashing the password before persisting
    /// </summary>
    /// <param name="createUserRequest">User to create</param>
    /// <returns>Id of created user</returns>
    Task<int?> CreateUserAsync(CreateUserRequest createUserRequest);

    /// <summary>
    /// Authenticates a user provided a valid AuthenticationRequest (from JSON payload)
    /// </summary>
    /// <param name="authenticationRequest">Request to authenticate</param>
    /// <returns>Authenticated response containing the user and the issued tokens. </returns>
    Task<AuthenticatedUserLoginResponse?> LogInAsync(AuthenticationRequest authenticationRequest);

    /// <summary>
    /// Queries the refresh tokens table to determine whether the provided refresh token exists
    /// </summary>
    /// <remarks>
    /// Not in use by this API but could be consumed by a client to refresh token 
    /// and ensure user is logged in, without explicitly logging in before it expires
    /// </remarks>
    /// <param name="refreshToken">Refresh token to validate</param>
    /// <returns>True if it does, else false</returns>
    Task<bool> IsRefreshTokenValidAsync(string refreshToken);

    /// <summary>
    /// Gets user by id, querying users table
    /// </summary>
    /// <param name="id">Id to lookup users table with</param>
    /// <returns>A <see cref="User"/> if one could be found, else null</returns>
    Task<User?> GetUserByIdAsync(int? id);
}

public class UserService(ApplicationDbContext context, ILogger<UserService> logger, 
    ITokenService tokenService, IPasswordHasher<AuthenticationRequest> passwordHasher) : IUserService
{
    public async Task<int?> CreateUserAsync(CreateUserRequest createUserRequest)
    {
        try
        {
            createUserRequest.Password = passwordHasher.HashPassword(createUserRequest, createUserRequest.Password);

            var user = new User
            {
                FirstName = createUserRequest.FirstName,
                LastName = createUserRequest.LastName,
                Username = createUserRequest.Username,
                Password = createUserRequest.Password
            };

            context.Users.Add(user);

            await context.SaveChangesAsync();

            return user.Id;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException postgresEx && postgresEx.SqlState == "23505")
        {
            // Handle unique constraint violation (duplicate user)
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error inserting user data for {Username}", createUserRequest.Username);
        }

        return null;
    }

    public async Task<AuthenticatedUserLoginResponse?> LogInAsync(AuthenticationRequest authenticationRequest)
    {
        try
        {
            var user = await GetUserByUsernameAsync(authenticationRequest);

            if (user == null)
                return null;

            return await tokenService.GenerateTokens(user.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error with trying to login for {Username}", authenticationRequest.Username);
        }

        return null;
    }

    private async Task<User?> GetUserByUsernameAsync(AuthenticationRequest authenticationRequest)
    {
        try
        {
            var user = await context.Users.FirstOrDefaultAsync(u => u.Username == authenticationRequest.Username);

            if (user != null)
            {
                PasswordVerificationResult passwordVerificationResult =
                    passwordHasher.VerifyHashedPassword(authenticationRequest, user.Password, authenticationRequest.Password);

                if (passwordVerificationResult == PasswordVerificationResult.Success)
                    return user;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Couldn't find user with username {Username}", authenticationRequest.Username);
        }

        return null;
    }

    public async Task<bool> IsRefreshTokenValidAsync(string refreshToken) => await GetUserIdByRefreshTokenAsync(refreshToken) != 0;

    private async Task<int> GetUserIdByRefreshTokenAsync(string refreshToken)
    {
        try
        {
            var refreshTokenEntity = await context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

            if (refreshTokenEntity != null) return refreshTokenEntity.UserId;

        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Couldn't find user for token {RefreshToken}", refreshToken);
        }

        return 0;
    }

    public async Task<User?> GetUserByIdAsync(int? id)
    {
        if (id == 0) return null;

        try
        {
            var user = await context.Users.FirstOrDefaultAsync(u => u.Id == id);

            if (user != null) return user;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Couldn't find user with id {Id}", id);
        }

        return null;
    }
}
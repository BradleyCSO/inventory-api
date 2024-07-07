using InventoryAPI.Services;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace InventoryAPI.Middleware
{
    /// <summary>
    /// Intercepts all HTTP requests: responsible for validating a user's authorisation header to determine whether they can
    /// make authenticated requests
    /// </summary>
    /// <param name="next">The delegate to invoke the next middleware in the pipeline</param>
    /// <param name="configuration">Key/value to get from configuration file</param>
    public class JwtMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        public async Task Invoke(HttpContext context)
        {
            string? token = context.Request.Headers.Authorization.FirstOrDefault()?.Split(" ").Last();

            if (token != null)
            {
                var userService = context.RequestServices.GetRequiredService<IUserService>();
                await ValidateTokenAsync(token, context, userService);
            }

            await next(context);
        }

        private async Task ValidateTokenAsync(string token, HttpContext context, IUserService userService)
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            var jwtToken = await tokenHandler.ValidateTokenAsync(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(configuration["Jwt:SecretKey"] ?? throw new InvalidOperationException())),
                ValidateIssuer = false,
                ValidateAudience = false,
                ClockSkew = TimeSpan.Zero // Expires at token expiration time
            });

            context.Items["User"] = await userService.GetUserByIdAsync(Convert.ToInt32(jwtToken?.Claims?["id"])); // Attach user to context
        }
    }
}
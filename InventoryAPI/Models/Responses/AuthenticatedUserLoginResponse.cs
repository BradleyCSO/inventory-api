namespace InventoryAPI.Responses;

public class AuthenticatedUserLoginResponse
{
    public required int? UserId { get; set; }
    public required string AccessToken { get; set; }
    public required string RefreshToken { get; set; }
    public required DateTime RefreshTokenExpiration { get; set; }
}
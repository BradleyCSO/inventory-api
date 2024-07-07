namespace InventoryAPI.Models;

public class RefreshToken
{
    public int Id { get; set; }
    public required int UserId { get; set; }
    public required string Token { get; set; }
    public required DateTime Expiration { get; set; }
}
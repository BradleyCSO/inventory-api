namespace InventoryAPI.Requests;

public class AuthenticationRequest
{
    public required string Username { get; set; }
    public required string Password { get; set; }    
}
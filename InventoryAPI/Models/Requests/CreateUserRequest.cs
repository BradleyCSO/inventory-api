namespace InventoryAPI.Requests;

public class CreateUserRequest : AuthenticationRequest
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
}
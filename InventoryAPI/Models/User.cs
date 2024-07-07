namespace InventoryAPI.Models;

public class User
{
    public int Id { get; set; }
    public required string Username { get; set; }
    public required string Password { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }

    // Navigation property to represent the relationship
    public ICollection<Inventory> Inventory { get; set; } = [];
}
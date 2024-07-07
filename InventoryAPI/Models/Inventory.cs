namespace InventoryAPI.Models;

public class Inventory
{
    public int UserId { get; set; }
    public User? User { get; set; } // Navigation property for the User
    public Guid ItemId { get; set; }
    public Item? Item { get; set; } // Navigation property for the Item
    public int Quantity { get; set; }
}
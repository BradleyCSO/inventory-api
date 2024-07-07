namespace InventoryAPI.Models;

public class Item
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property to represent the relationship
    public ICollection<Inventory> Inventory { get; set; } = [];
}
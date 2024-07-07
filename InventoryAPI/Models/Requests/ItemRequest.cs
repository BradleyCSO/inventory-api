namespace InventoryAPI.Models.Requests;

public class ItemRequest
{
    public required string Name { get; set; }
    public string? Description { get; set; }
}
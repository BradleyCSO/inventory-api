namespace InventoryAPI.Models.Responses;

public class UserInventoryResponse
{
    public Guid ItemId { get; set; }
    public int Quantity { get; set; }
}
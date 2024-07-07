namespace InventoryAPI.Models.Requests;

public class ItemSubtractRequest
{
    public Guid ItemId { get; set; }
    public int Quantity { get; set; }
}
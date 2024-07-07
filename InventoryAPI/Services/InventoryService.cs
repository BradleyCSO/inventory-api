using InventoryAPI.DbContext;
using InventoryAPI.Models;
using InventoryAPI.Models.Requests;
using InventoryAPI.Models.Responses;
using Microsoft.EntityFrameworkCore;

namespace InventoryAPI.Services;

public interface IInventoryService
{
    /// <summary>
    /// Add a single item to a user's inventory provided a <see cref="ItemRequest"/>
    /// If item doesn't exist provided this request, create it and assign it to a user's inventory/>
    /// </summary>
    /// <remarks>Wasn't sure whether to seed the database with some initial items here,
    /// then just assign those items to user's inventory if it exists, otherwise do nothing</remarks>
    /// <param name="userId">User's inventory to modify</param>
    /// <param name="itemRequest"></param>
    /// <returns>Boolean task: true if item creation and updating of user's inventory persisted to the database</returns>
    Task<bool> AddItemAsync(int userId, ItemRequest itemRequest);

    /// <summary>
    /// Atomic bulk insert of items to a user's inventory. All or nothing: if one item fails to create, fail the entire transaction (rollback)
    /// </summary>
    /// <remarks>
    /// Considered individual transactions: i.e. if one item/inventory is valid for <see cref="ItemRequest"/>, persist it. 
    /// But after further thought I went with rolling back the entire batch if any fails. One advantage with this approach I thought of was less database transactions:
    /// ensuring consistent states which seems logical for an inventory system, cons are it's quite punitive for a single failure
    /// </remarks>
    /// <param name="userId">User's inventory to modify</param>
    /// <param name="itemRequests">Array of items to add</param>
    /// <returns>True if all items were added to database, false if one item failed (causing rollback of entire transaction)</returns>
    Task<bool> AddItemsAsync(int userId, ItemRequest[] itemRequests);

    /// <summary>
    /// Subtracts an item from a user's inventory: where a user can have 0 of an item
    /// </summary>
    /// <param name="userId">User's inventory to subtract from</param>
    /// <param name="itemSubtractRequest">Item id to subtract from this user's inventory</param>
    /// <returns></returns>
    Task<IEnumerable<UserInventoryResponse>> SubtractItemAsync(int userId, ItemSubtractRequest itemSubtractRequest);

    /// <summary>
    /// Gets array of inventory items for a given userId
    /// </summary>
    /// <remarks>
    /// Pagination would be nice here i.e. say if we have a 'storage' of items which can encompass 100s of items
    /// We wouldn't fetch these all at once in a realistic scenario
    /// </remarks>
    /// <param name="userId"></param>
    /// <returns>Array of <see cref="UserInventoryResponse"/>items</returns>
    Task<IEnumerable<UserInventoryResponse>> GetUserInventoryAsync(int userId);
}

public class InventoryService(ApplicationDbContext context, ILogger<InventoryService> logger) : IInventoryService
{
    public async Task<bool> AddItemAsync(int userId, ItemRequest itemRequest)
    {
        var item = await GetItemByNameAsync(itemRequest.Name);

        // If the item doesn't exist, create it
        if (item == null)
        {
            item = new Item
            {
                Id = Guid.NewGuid(),
                Name = itemRequest.Name,
                Description = itemRequest.Description,
            };

            context.Items.Add(item);
            await context.SaveChangesAsync();
        }

        // Find the inventory record for the user and item
        Inventory? inventory = await GetInventoryRecordForUserAndItemAsync(userId, item.Id);

        if (inventory == null)
        {
            // Add a new inventory record if it doesn't exist
            context.Inventory.Add(new Inventory
            {
                UserId = userId,
                ItemId = item.Id,
                Quantity = 1
            });
        }
        else
            inventory.Quantity += 1; // Increment the quantity of the existing inventory-item record

        return await context.SaveChangesAsync() > 0;
    }

    public async Task<bool> AddItemsAsync(int userId, ItemRequest[] itemRequests)
    {
        bool allSuccessful = true;

        await using var transaction = await context.Database.BeginTransactionAsync();

        foreach (var itemRequest in itemRequests)
        {
            try
            {
                var item = await GetItemByNameAsync(itemRequest.Name);

                if (item == null)
                {
                    item = new Item
                    {
                        Id = Guid.NewGuid(),
                        Name = itemRequest.Name,
                        Description = itemRequest.Description,
                    };

                    await context.Items.AddAsync(item);
                }

                Inventory? inventory = await GetInventoryRecordForUserAndItemAsync(userId, item.Id);

                if (inventory == null)
                {
                    await context.Inventory.AddAsync(new Inventory
                    {
                        UserId = userId,
                        ItemId = item.Id,
                        Quantity = 1
                    });
                }
                else
                {
                    inventory.Quantity += 1;
                }

                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error adding item '{ItemName}' for user with id {UserId}", itemRequest.Name, userId);

                // Mark as unsuccessful to ensure rollback, and break out of the loop 
                allSuccessful = false;
                break;
            }
        }

        if (allSuccessful)
            await transaction.CommitAsync();

        else
            await transaction.RollbackAsync();

        return allSuccessful;
    }

    private async Task<Item?> GetItemByNameAsync(string itemToLookup) =>
       await context.Items.FirstOrDefaultAsync(i => i.Name == itemToLookup);

    private async Task<Inventory?> GetInventoryRecordForUserAndItemAsync(int userIdToLookup, Guid itemIdToLookup) =>
         await context.Inventory.FirstOrDefaultAsync(i => i.UserId == userIdToLookup && i.ItemId == itemIdToLookup);

    public async Task<IEnumerable<UserInventoryResponse>> SubtractItemAsync(int userId, ItemSubtractRequest itemSubtractRequest)
    {
        var inventoryItem = await context.Inventory
            .FirstOrDefaultAsync(i => i.UserId == userId && i.ItemId == itemSubtractRequest.ItemId);

        // If we couldn't find an item to this user's inventory, just return their current inventory
        if (inventoryItem == null)
            return await GetUserInventoryAsync(userId); 

        // Else reduce this item's quantity for this user's inventory: ensure that it never goes below 0
        inventoryItem.Quantity = Math.Max(0, inventoryItem.Quantity - itemSubtractRequest.Quantity);

        await context.SaveChangesAsync();

        return await GetUserInventoryAsync(userId);
    }

    public async Task<IEnumerable<UserInventoryResponse>> GetUserInventoryAsync(int userId) =>
        await context.Inventory.Where(i => i.UserId == userId)
        .Select(i => new UserInventoryResponse()
        {
            ItemId = i.ItemId,
            Quantity = i.Quantity
        })
        .ToListAsync();
}

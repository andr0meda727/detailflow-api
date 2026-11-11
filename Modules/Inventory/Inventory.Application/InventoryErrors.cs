using Shared.Kernel.Results;

namespace Inventory.Application;

public static class InventoryErrors
{
    public static Error ItemNotFound(Guid id) =>
        Error.NotFound("INVENTORY_ITEM_NOT_FOUND", $"Inventory item {id} not found");

    public static Error CategoryNotFound(Guid id) =>
        Error.NotFound("INVENTORY_CATEGORY_NOT_FOUND", $"Inventory category {id} not found");
}

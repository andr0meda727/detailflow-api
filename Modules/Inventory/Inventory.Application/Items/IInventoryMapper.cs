using Inventory.Domain;

namespace Inventory.Application.Items;

public interface IInventoryMapper
{
    public InventoryResponseDto MapInventoryToResponse(InventoryItem inventory);
}
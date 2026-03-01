using Core.DTOs.Inventory;
using Core.Models;

namespace Core.Interfaces.Mappings;

public interface IInventoryMapper
{
    public InventoryResponseDto MapInventoryToResponse(Inventory inventory);
}
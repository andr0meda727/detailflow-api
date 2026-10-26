using Inventory.Domain;
using Riok.Mapperly.Abstractions;

namespace Inventory.Application.Items;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class InventoryMapper : IInventoryMapper
{
    public partial InventoryResponseDto MapInventoryToResponse(InventoryItem inventory);
}

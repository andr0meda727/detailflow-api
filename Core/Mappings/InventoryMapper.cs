using Core.DTOs.Inventory;
using Core.Interfaces.Mappings;
using Core.Models;
using Riok.Mapperly.Abstractions;

namespace Core.Mappings;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class InventoryMapper : IInventoryMapper
{
    public partial InventoryResponseDto MapInventoryToResponse(Inventory inventory);
}

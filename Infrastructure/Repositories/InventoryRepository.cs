using Core.Interfaces.Repositories;
using Core.Models;
using Infrastructure.Data;

namespace Infrastructure.Repositories;

public class InventoryRepository(ApplicationDbContext context)
    : Repository<Inventory>(context), IInventoryRepository
{
}

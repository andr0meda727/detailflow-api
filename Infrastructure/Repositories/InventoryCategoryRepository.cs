using Core.Interfaces.Repositories;
using Core.Models;
using Infrastructure.Data;

namespace Infrastructure.Repositories;

public class InventoryCategoryRepository(ApplicationDbContext context) : Repository<InventoryCategory>(context), IInventoryCategoryRepository
{

}
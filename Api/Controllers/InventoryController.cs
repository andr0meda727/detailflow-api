using Core.Models;
using Infrastructure.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InventoryController(IRepository<Inventory> inventoryRepo) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Inventory>>> Get()
    {
        var items = await inventoryRepo.GetAllAsync();
        return Ok(items);
    }
}
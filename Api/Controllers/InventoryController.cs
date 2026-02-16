using Core.Models;
using Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InventoryController(IRepository<Inventory> inventoryRepo) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Inventory>>> Get()
    {
        var items = await inventoryRepo.GetAllAsync();
        return Ok(items);
    }
}
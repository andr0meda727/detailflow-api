using Core.DTOs.Vehicles;
using Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class VehicleController(IVehiclesService vehiclesService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<VehicleInfoDto>>> GetAll(
        [FromQuery] Guid? clientId = null,
        [FromQuery] string? search = null)
    {
        var vehicles = await vehiclesService.GetAllAsync(clientId, search);
        return Ok(vehicles);
    }

    [HttpPost]
    public async Task<ActionResult<VehicleInfoDto>> Create([FromBody] CreateVehicleRequest vehicleData)
    {
        var addedVehicle = await vehiclesService.AddAsync(vehicleData);

        if (addedVehicle == null)
        {
            return BadRequest($"Client {vehicleData.ClientId} doesn't exist");
        }

        return Ok(addedVehicle);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await vehiclesService.DeleteAsync(id);

        if (!deleted) return NotFound($"Vehicle {id} doesn't exist");

        return NoContent();
    }
}

using Shared.Api.Errors;
using Shared.Kernel.Results;
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
            return this.ToProblem(Error.Validation("CLIENT_NOT_FOUND", $"Client {vehicleData.ClientId} not found"));
        }

        return Ok(addedVehicle);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<VehicleInfoDto>> Update(Guid id, [FromBody] CreateVehicleRequest dto)
    {
        var result = await vehiclesService.UpdateVehicleAsync(id, dto);

        if (result == null)
        {
            return this.ToProblem(Error.NotFound("VEHICLE_NOT_FOUND", $"Vehicle {id} not found"));
        }

        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await vehiclesService.DeleteAsync(id);

        if (!deleted) return this.ToProblem(Error.NotFound("VEHICLE_NOT_FOUND", $"Vehicle {id} not found"));

        return NoContent();
    }
}
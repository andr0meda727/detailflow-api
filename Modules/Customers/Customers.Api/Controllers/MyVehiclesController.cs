using Customers.Application;
using Customers.Application.Garage.AddVehicle;
using Customers.Application.Garage.DeleteVehicle;
using Customers.Application.Garage.GetMyVehicles;
using Customers.Application.Garage.UpdateVehicle;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Api.Errors;
using Shared.Api.Tenancy;

namespace Customers.Api.Controllers;

/// <summary>
/// The signed-in customer's garage. Only their own cars are visible; anyone else's ID is not found.
/// </summary>
[ApiController]
[Route("api/me/vehicles")]
[Authorize]
[AllowWithoutStudio]
public class MyVehiclesController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<GarageVehicleDto>>> GetAll(
        [FromServices] GetMyVehiclesHandler handler, CancellationToken cancellationToken)
    {
        return await handler.HandleAsync(cancellationToken);
    }

    [HttpPost]
    public async Task<ActionResult<GarageVehicleDto>> Add(
        GarageVehicleRequest request, [FromServices] AddVehicleHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);
        if (!result.Success) return this.ToProblem(result.Error!);

        return Created($"/api/me/vehicles/{result.Value!.Id}", result.Value);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<GarageVehicleDto>> Update(
        Guid id, GarageVehicleRequest request, [FromServices] UpdateVehicleHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, request, cancellationToken);

        return result.Success ? result.Value! : this.ToProblem(result.Error!);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(
        Guid id, [FromServices] DeleteVehicleHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, cancellationToken);

        return result.Success ? NoContent() : this.ToProblem(result.Error!);
    }
}

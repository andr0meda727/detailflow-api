using Crm.Application.Vehicles;
using Crm.Application.Vehicles.CreateVehicle;
using Crm.Application.Vehicles.DeleteVehicle;
using Crm.Application.Vehicles.GetVehicles;
using Crm.Application.Vehicles.UpdateVehicle;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Api.Errors;
using Shared.Kernel.Pagination;

namespace Crm.Api.Controllers;

// Singular route kept for existing clients of the API
[ApiController]
[Route("api/vehicle")]
[Authorize]
public class VehicleController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResponse<VehicleResponseDto>>> GetAll(
        [FromQuery] PageRequest paging, [FromQuery] Guid? clientId, [FromQuery] string? search,
        [FromServices] GetVehiclesHandler handler, CancellationToken cancellationToken)
    {
        return await handler.HandleAsync(paging, clientId, search, cancellationToken);
    }

    [HttpPost]
    public async Task<ActionResult<VehicleResponseDto>> Create(
        VehicleRequest request, [FromServices] CreateVehicleHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);

        return result.Success ? result.Value! : this.ToProblem(result.Error!);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<VehicleResponseDto>> Update(
        Guid id, VehicleRequest request, [FromServices] UpdateVehicleHandler handler, CancellationToken cancellationToken)
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

using Identity.Application;
using Identity.Application.Employees.ChangeEmployeeRole;
using Identity.Application.Employees.GetEmployees;
using Identity.Application.Employees.InviteEmployee;
using Identity.Application.Employees.RemoveEmployee;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Api.Errors;

namespace Identity.Api.Controllers;

/// <summary>
/// The studio's staff. Everything here is for the studio owner only.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = IdentityModule.OwnerOnlyPolicy)]
public class EmployeesController : ControllerBase
{
    [HttpPost("invite")]
    public async Task<IActionResult> InviteEmployee(
        InviteEmployeeRequest request, [FromServices] InviteEmployeeHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);

        return result.Success
            ? Ok(new { message = "Zaproszenie zostało wysłane pomyślnie!" })
            : this.ToProblem(result.Error!);
    }

    [HttpGet]
    public async Task<ActionResult<List<StudioEmployeeDto>>> GetAllEmployees(
        [FromServices] GetEmployeesHandler handler, CancellationToken cancellationToken)
    {
        return await handler.HandleAsync(cancellationToken);
    }

    [HttpDelete("{userId:guid}")]
    public async Task<IActionResult> RemoveEmployee(
        Guid userId, [FromServices] RemoveEmployeeHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(userId, cancellationToken);

        return result.Success ? NoContent() : this.ToProblem(result.Error!);
    }

    [HttpPatch("role")]
    public async Task<IActionResult> ChangeEmployeeRole(
        ChangeEmployeeRoleRequest request, [FromServices] ChangeEmployeeRoleHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);

        return result.Success
            ? Ok(new { message = "Rola pracownika została zmieniona." })
            : this.ToProblem(result.Error!);
    }
}

using Core.DTOs.Auth;
using Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EmployeesController(IEmployeesService employeesService) : ControllerBase
{

    [HttpPost("invite")]
    [Authorize(Policy = "OwnerOnly")]
    public async Task<IActionResult> InviteEmployee([FromBody] InviteEmployeeDto dto)
    {
        var result = await employeesService.InviteEmployeeAsync(dto);

        if (!result.Success)
            return Conflict(new { error = result.ErrorCode, message = result.ErrorMessage });

        return Ok(new { message = "Zaproszenie zostało wysłane pomyślnie!" });
    }
}
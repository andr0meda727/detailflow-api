using Core.DTOs.Employees;
using Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using InviteEmployeeDto = Core.DTOs.Auth.InviteEmployeeDto;

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

    /// <summary>
    /// Zwraca listę wszystkich pracowników studia. Dostępne tylko dla właściciela (Owner).
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "OwnerOnly")]
    public async Task<IActionResult> GetAllEmployees()
    {
        var employees = await employeesService.GetAllEmployeesAsync();
        return Ok(employees);
    }

    /// <summary>
    /// Usuwa pracownika ze studia. Dostępne tylko dla właściciela (Owner).
    /// </summary>
    [HttpDelete("{userId:guid}")]
    [Authorize(Policy = "OwnerOnly")]
    public async Task<IActionResult> RemoveEmployee(Guid userId)
    {
        var result = await employeesService.RemoveEmployeeAsync(userId);

        if (!result.Success)
            return result.ErrorCode == "NOT_FOUND"
                ? NotFound(new { error = result.ErrorCode, message = result.ErrorMessage })
                : Conflict(new { error = result.ErrorCode, message = result.ErrorMessage });

        return NoContent();
    }

    /// <summary>
    /// Zmienia rolę pracownika (np. na Manager lub Employee). Dostępne tylko dla właściciela (Owner).
    /// </summary>
    [HttpPatch("role")]
    [Authorize(Policy = "OwnerOnly")]
    public async Task<IActionResult> ChangeEmployeeRole([FromBody] ChangeEmployeeRoleDto dto)
    {
        var result = await employeesService.ChangeEmployeeRoleAsync(dto);

        if (!result.Success)
            return result.ErrorCode == "NOT_FOUND"
                ? NotFound(new { error = result.ErrorCode, message = result.ErrorMessage })
                : Conflict(new { error = result.ErrorCode, message = result.ErrorMessage });

        return Ok(new { message = "Rola pracownika została zmieniona." });
    }
}
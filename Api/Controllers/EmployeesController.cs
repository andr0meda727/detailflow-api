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
    [HttpPost("invite")] // Adres: /api/employees/invite
    public async Task<IActionResult> InviteEmployee([FromBody] InviteEmployeeDto dto)
    {
        await employeesService.InviteEmployeeAsync(dto);
        return Ok(new { message = "Zaproszenie zostało wysłane pomyślnie!" });
    }
}
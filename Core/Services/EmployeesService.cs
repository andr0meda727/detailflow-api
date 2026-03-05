using Core.DTOs.Auth;
using Core.Interfaces;
using Core.Interfaces.Services;

namespace Core.Services;

public class EmployeesService(
    IAuthService authService,
    ITenantContext tenantContext) : IEmployeesService
{
    public async Task<bool> InviteEmployeeAsync(InviteEmployeeDto dto)
    {
        await authService.InviteUserAsync(dto.Email, tenantContext.StudioId, dto.RoleId);

        return true;
    }
}
using Core.DTOs.Employees;
using Shared.Kernel.Results;
using InviteEmployeeDto = Core.DTOs.Auth.InviteEmployeeDto;

namespace Core.Interfaces.Services;

public interface IEmployeesService
{
    Task<Result<Guid>> InviteEmployeeAsync(InviteEmployeeDto dto);
    Task<List<StudioEmployeeDto>> GetAllEmployeesAsync();
    Task<Result> RemoveEmployeeAsync(Guid userId);
    Task<Result> ChangeEmployeeRoleAsync(ChangeEmployeeRoleDto dto);
}
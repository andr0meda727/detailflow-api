using Core.DTOs.Employees;
using Core.DTOs.Results;
using InviteEmployeeDto = Core.DTOs.Auth.InviteEmployeeDto;

namespace Core.Interfaces.Services;

public interface IEmployeesService
{
    Task<InviteResult> InviteEmployeeAsync(InviteEmployeeDto dto);
    Task<List<StudioEmployeeDto>> GetAllEmployeesAsync();
    Task<Result> RemoveEmployeeAsync(Guid userId);
    Task<Result> ChangeEmployeeRoleAsync(ChangeEmployeeRoleDto dto);
}
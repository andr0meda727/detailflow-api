using Core.DTOs.Results;
using InviteEmployeeDto = Core.DTOs.Auth.InviteEmployeeDto;

namespace Core.Interfaces.Services;

public interface IEmployeesService
{
    Task<InviteResult> InviteEmployeeAsync(InviteEmployeeDto dto);
}
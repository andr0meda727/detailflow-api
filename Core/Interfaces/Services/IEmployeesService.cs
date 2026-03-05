using Core.DTOs.Auth;

namespace Core.Interfaces.Services;

public interface IEmployeesService
{
    Task<bool> InviteEmployeeAsync(InviteEmployeeDto dto);
}
using Core.DTOs.Auth;
using Core.DTOs.Results;

namespace Core.Interfaces.Services;

public interface IOwnerRegistrationService
{
    Task<Result> RegisterAsync(RegisterOwnerDto dto);
}
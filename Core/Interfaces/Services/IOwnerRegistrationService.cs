using Core.DTOs.Auth;
using Shared.Kernel.Results;

namespace Core.Interfaces.Services;

public interface IOwnerRegistrationService
{
    Task<Result> RegisterAsync(RegisterOwnerDto dto);
}
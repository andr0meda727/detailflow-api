using Core.DTOs.Employees;
using Shared.Kernel.Results;

namespace Core.Interfaces.Services;

public interface IInvitationService
{
    Task<InvitationResponseDto?> GetByTokenAsync(Guid token);
    Task<Result> AcceptAsync(Guid token, Guid userId);
    Task<Result> DeclineAsync(Guid token, Guid userId);
}
using Core.Constants;
using Core.DTOs.Employees;
using Core.DTOs.Results;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Models;
using InviteEmployeeDto = Core.DTOs.Auth.InviteEmployeeDto;

namespace Core.Services;

public class EmployeesService(
    IInvitationSender invitationSender,
    ITenantContext tenantContext,
    IUserRepository userRepository,
    IStudioMemberRepository studioMemberRepository,
    IStudioInvitationRepository invitationRepository) : IEmployeesService
{
    public async Task<InviteResult> InviteEmployeeAsync(InviteEmployeeDto dto)
    {
        var studioId = tenantContext.StudioId ?? throw new InvalidOperationException("StudioId is null");
        var invitingUserId = tenantContext.UserId!.Value;

        // 1. Self-invite
        var invitingUser = await userRepository.GetByIdAsync(invitingUserId);
        if (invitingUser?.Email == dto.Email)
            return InviteResult.Fail("SELF_INVITE", "Nie możesz zaprosić samego siebie.");

        // 2. Już jest członkiem tego studia
        var existingMember = await studioMemberRepository
            .GetByUserEmailAndStudioAsync(dto.Email, studioId);

        if (existingMember is { IsActive: true })
            return InviteResult.Fail("ALREADY_MEMBER",
                existingMember.RoleId == dto.RoleId
                    ? "Ten użytkownik jest już pracownikiem tego studia."
                    : "Ten użytkownik jest już pracownikiem. Użyj opcji zmiany roli.");

        // 3. Czy jest już aktywne zaproszenie do tego studia?
        var existingInvitation = await invitationRepository
            .GetPendingByEmailAndStudioAsync(dto.Email, studioId);

        if (existingInvitation != null)
            return InviteResult.Fail("INVITATION_PENDING",
                "Zaproszenie dla tego użytkownika już zostało wysłane i oczekuje na akceptację.");

        // 4. Utwórz zaproszenie w tabeli — niezależnie czy user istnieje czy nie
        var invitation = await invitationRepository.AddAsync(new StudioInvitation
        {
            StudioId = studioId,
            Email = dto.Email,
            RoleId = dto.RoleId,
            Token = Guid.NewGuid(),
            Status = InvitationStatus.Pending,
            InvitedBy = invitingUserId,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        });

        var acceptUrl = $"http://localhost:3000/accept-invitation?token={invitation.Token}";

        var existingUser = await userRepository.GetUserByEmailAsync(dto.Email);
        await invitationSender.SendInvitationAsync(invitation, existingUser != null, studioId, acceptUrl);

        return InviteResult.Ok(invitation.Id);
    }

    public async Task<List<StudioEmployeeDto>> GetAllEmployeesAsync()
    {
        var studioId = tenantContext.StudioId ?? throw new InvalidOperationException("StudioId is null");

        var members = await studioMemberRepository.GetAllByStudioAsync(studioId);

        return members.Select(m => new StudioEmployeeDto(
            m.UserId,
            m.User.Email,
            m.User.FirstName,
            m.User.LastName,
            m.RoleId,
            m.Role.RoleName
        )).ToList();
    }

    public async Task<Result> RemoveEmployeeAsync(Guid userId)
    {
        var studioId = tenantContext.StudioId ?? throw new InvalidOperationException("StudioId is null");
        var requestingUserId = tenantContext.UserId!.Value;

        if (userId == requestingUserId)
            return Result.Fail("SELF_REMOVE", "Nie możesz zwolnić samego siebie.");

        var member = await studioMemberRepository.GetByUserIdAndStudioAsync(userId, studioId);

        if (member is null || !member.IsActive)
            return Result.Fail("NOT_FOUND", "Nie znaleziono aktywnego pracownika w tym studiu.");

        if (member.RoleId == Roles.Owner)
            return Result.Fail("CANNOT_REMOVE_OWNER", "Nie można usunąć właściciela studia.");

        member.IsActive = false;

        await studioMemberRepository.UpdateAsync(member);
        return Result.Ok();
    }

    public async Task<Result> ChangeEmployeeRoleAsync(ChangeEmployeeRoleDto dto)
    {
        var studioId = tenantContext.StudioId ?? throw new InvalidOperationException("StudioId is null");
        var requestingUserId = tenantContext.UserId!.Value;

        if (dto.UserId == requestingUserId)
            return Result.Fail("SELF_ROLE_CHANGE", "Nie możesz zmienić własnej roli.");

        var member = await studioMemberRepository.GetByUserIdAndStudioAsync(dto.UserId, studioId);
        if (member is null)
            return Result.Fail("NOT_FOUND", "Nie znaleziono pracownika w tym studiu.");

        if (member.RoleId == Roles.Owner)
            return Result.Fail("CANNOT_CHANGE_OWNER_ROLE", "Nie można zmienić roli właściciela.");

        var validRoles = new[] { Roles.Manager, Roles.Employee };
        if (!validRoles.Contains(dto.RoleId))
            return Result.Fail("INVALID_ROLE", "Podana rola jest nieprawidłowa.");

        member.RoleId = dto.RoleId;
        await studioMemberRepository.UpdateAsync(member);
        return Result.Ok();
    }
}
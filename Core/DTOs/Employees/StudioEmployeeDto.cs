namespace Core.DTOs.Employees;

public record StudioEmployeeDto(
    Guid UserId,
    string Email,
    string? FirstName,
    string? LastName,
    long RoleId,
    string RoleName
);

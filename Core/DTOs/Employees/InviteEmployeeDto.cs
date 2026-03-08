namespace Core.DTOs.Employees;

public class InviteEmployeeDto
{
    public string Email { get; set; } = string.Empty;
    public long RoleId { get; set; }
}
namespace Core.DTOs.Clients;

public record CreateClientDto(
    string Name,
    string? Email,
    string? Phone,
    string? Nip
);
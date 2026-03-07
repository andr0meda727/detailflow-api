namespace Core.Interfaces;

public interface ITenantContext
{
    Guid StudioId { get; }
    Guid? UserId { get; }
    void SetTenant(Guid studioId, Guid? userId);
    void SetUserOnly(Guid userId);
}
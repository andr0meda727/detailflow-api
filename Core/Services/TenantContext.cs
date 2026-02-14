using Core.Interfaces;

namespace Core.Services;

public class TenantContext : ITenantContext
{
    private Guid _studioId;
    public Guid? UserId { get; private set; }

    public Guid StudioId => _studioId != Guid.Empty 
        ? _studioId 
        : throw new InvalidOperationException("StudioId wasn't set. Check middleware.");


    public void SetTenant(Guid studioId, Guid? userId)
    {
        _studioId = studioId;
        UserId = userId;
    }
}

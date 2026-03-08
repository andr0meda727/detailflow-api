using Core.Interfaces;

namespace Core.Services;

public class TenantContext : ITenantContext
{
    private bool _isSet;
    public Guid? StudioId { get; private set; }
    public Guid? UserId { get; private set; }

    public void SetTenant(Guid studioId, Guid? userId)
    {
        if (_isSet) return;

        StudioId = studioId;
        UserId = userId;
        _isSet = true;
    }

    public void SetUserOnly(Guid userId)
    {
        if (_isSet) return;

        UserId = userId;
    }
}
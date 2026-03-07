using Core.Interfaces;

namespace Core.Services;

public class TenantContext : ITenantContext
{
    private Guid? _studioId;
    private bool _isSet;
    public Guid? UserId { get; private set; }

    public Guid StudioId => _studioId ?? throw new InvalidOperationException("StudioId wasn't set");

    public void SetTenant(Guid studioId, Guid? userId)
    {
        if (_isSet) return;

        _studioId = studioId;
        UserId = userId;
        _isSet = true;
    }

    public void SetUserOnly(Guid userId)
    {
        if (_isSet) return;

        UserId = userId;
    }
}
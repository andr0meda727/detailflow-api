using System.Security.Claims;
using Api.Middleware;
using Core.Constants;
using Core.Interfaces.Repositories;
using Core.Models;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using Shared.Infrastructure.Tenancy;

namespace Unit.Middleware;

[TestFixture]
public class TenantMiddlewareTests
{
    private IUserRepository _userRepository;
    private TenantContext _tenantContext;
    private bool _nextCalled;
    private TenantMiddleware _sut;

    [SetUp]
    public void SetUp()
    {
        _userRepository = Substitute.For<IUserRepository>();
        _tenantContext = new TenantContext();
        _nextCalled = false;
        _sut = new TenantMiddleware(_ =>
        {
            _nextCalled = true;
            return Task.CompletedTask;
        });
    }

    [Test]
    public async Task SingleActiveMembership_SetsTenant()
    {
        var (user, studioId) = UserWithSingleMembership(isActive: true);
        var context = AuthenticatedContext(user.Id);

        await _sut.InvokeAsync(context, _tenantContext, _userRepository);

        Assert.That(_tenantContext.StudioId, Is.EqualTo(studioId));
        Assert.That(_nextCalled, Is.True);
    }

    [Test]
    public async Task SingleInactiveMembership_DoesNotSetTenant()
    {
        var (user, _) = UserWithSingleMembership(isActive: false);
        var context = AuthenticatedContext(user.Id);

        await _sut.InvokeAsync(context, _tenantContext, _userRepository);

        Assert.That(_tenantContext.StudioId, Is.Null);
        Assert.That(_tenantContext.UserId, Is.EqualTo(user.Id));
    }

    [Test]
    public async Task InactiveMembership_RequestedByHeader_ReturnsForbidden()
    {
        var (user, studioId) = UserWithSingleMembership(isActive: false);
        var context = AuthenticatedContext(user.Id);
        context.Request.Headers["X-Studio-Id"] = studioId.ToString();

        await _sut.InvokeAsync(context, _tenantContext, _userRepository);

        Assert.That(context.Response.StatusCode, Is.EqualTo(StatusCodes.Status403Forbidden));
        Assert.That(_nextCalled, Is.False);
    }

    private (User User, Guid StudioId) UserWithSingleMembership(bool isActive)
    {
        var studioId = Guid.NewGuid();
        var user = new User { Id = Guid.NewGuid(), Email = "employee@test.pl" };
        user.StudioMembers.Add(new StudioMember
        {
            UserId = user.Id, StudioId = studioId, RoleId = Roles.Employee, IsActive = isActive
        });
        _userRepository.GetUserWithStudiosAsync(user.Id).Returns(user);
        return (user, studioId);
    }

    private static DefaultHttpContext AuthenticatedContext(Guid userId)
    {
        var identity = new ClaimsIdentity([new Claim("sub", userId.ToString())], authenticationType: "Test");
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
        context.Request.Path = "/api/bookings";
        return context;
    }
}

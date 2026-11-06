using Identity.Domain.Studios;
using Identity.Domain.Users;

namespace Unit.Modules.Identity;

[TestFixture]
public class UserAndRoleTests
{
    [TestCase("anna@example.com")]
    [TestCase("ANNA@Example.com")]
    [TestCase("  anna@example.com ")]
    public void HasEmail_IgnoresCaseAndSurroundingSpaces(string email)
    {
        var user = new User(Guid.NewGuid(), "anna@example.com", "Anna", null);

        Assert.That(user.HasEmail(email), Is.True);
    }

    [Test]
    public void HasEmail_IsFalseForAnotherAddress()
    {
        var user = new User(Guid.NewGuid(), "anna@example.com", "Anna", null);

        Assert.That(user.HasEmail("anna@example.org"), Is.False);
    }

    [TestCase(Roles.Manager, true)]
    [TestCase(Roles.Employee, true)]
    [TestCase(Roles.Owner, false)]
    [TestCase(0L, false)]
    [TestCase(99L, false)]
    public void OnlyManagerAndEmployee_CanBeHandedOut(long roleId, bool assignable)
    {
        Assert.That(Roles.IsAssignable(roleId), Is.EqualTo(assignable));
    }
}

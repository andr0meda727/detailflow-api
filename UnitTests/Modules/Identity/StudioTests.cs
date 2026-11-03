using Identity.Domain.Studios;

namespace Unit.Modules.Identity;

[TestFixture]
public class StudioTests
{
    private static readonly Guid Owner = Guid.NewGuid();

    private static Studio NewStudio() => Studio.Register(" Detail Studio ", null, " ", "Kraków", Owner);

    [Test]
    public void Register_MakesTheOwnerAnActiveMember()
    {
        var studio = NewStudio();

        var owner = studio.Members.Single();
        Assert.That(studio.Name, Is.EqualTo("Detail Studio"));
        Assert.That(studio.Email, Is.Null);
        Assert.That(owner, Has.Property(nameof(StudioMember.UserId)).EqualTo(Owner)
            .And.Property(nameof(StudioMember.RoleId)).EqualTo(Roles.Owner)
            .And.Property(nameof(StudioMember.IsActive)).True);
    }

    [Test]
    public void RemovedMember_IsBroughtBackWithTheNewRole()
    {
        var studio = NewStudio();
        var employee = Guid.NewGuid();
        studio.Admit(employee, Roles.Employee);
        studio.RemoveMember(employee, Owner);

        studio.Admit(employee, Roles.Manager);

        var member = studio.FindMember(employee)!;
        Assert.That(member.IsActive, Is.True);
        Assert.That(member.RoleId, Is.EqualTo(Roles.Manager));
        Assert.That(studio.Members, Has.Count.EqualTo(2));
    }

    [Test]
    public void Admit_CannotHandOutTheOwnerRole()
    {
        Assert.That(() => NewStudio().Admit(Guid.NewGuid(), Roles.Owner), Throws.InstanceOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void RemoveMember_RejectsSelfOwnerAndUnknownPeople()
    {
        var studio = NewStudio();
        var manager = Guid.NewGuid();
        studio.Admit(manager, Roles.Manager);

        Assert.Multiple(() =>
        {
            Assert.That(studio.RemoveMember(Owner, Owner).Error, Is.EqualTo(StudioErrors.SelfRemove));
            Assert.That(studio.RemoveMember(Owner, manager).Error, Is.EqualTo(StudioErrors.CannotRemoveOwner));
            Assert.That(studio.RemoveMember(Guid.NewGuid(), Owner).Error, Is.EqualTo(StudioErrors.MemberNotFound));
        });
    }

    [Test]
    public void RemoveMember_DeactivatesAndCannotRemoveTwice()
    {
        var studio = NewStudio();
        var employee = Guid.NewGuid();
        studio.Admit(employee, Roles.Employee);

        Assert.That(studio.RemoveMember(employee, Owner).Success, Is.True);
        Assert.That(studio.FindMember(employee)!.IsActive, Is.False);
        Assert.That(studio.RemoveMember(employee, Owner).Error, Is.EqualTo(StudioErrors.MemberNotFound));
    }

    [Test]
    public void ChangeRole_OnlyBetweenAssignableRoles()
    {
        var studio = NewStudio();
        var employee = Guid.NewGuid();
        studio.Admit(employee, Roles.Employee);

        Assert.Multiple(() =>
        {
            Assert.That(studio.ChangeRole(employee, Roles.Owner, Owner).Error, Is.EqualTo(StudioErrors.InvalidRole));
            Assert.That(studio.ChangeRole(Owner, Roles.Manager, employee).Error, Is.EqualTo(StudioErrors.CannotChangeOwnerRole));
            Assert.That(studio.ChangeRole(Owner, Roles.Manager, Owner).Error, Is.EqualTo(StudioErrors.SelfRoleChange));
            Assert.That(studio.ChangeRole(employee, Roles.Manager, Owner).Success, Is.True);
        });
        Assert.That(studio.FindMember(employee)!.RoleId, Is.EqualTo(Roles.Manager));
    }
}

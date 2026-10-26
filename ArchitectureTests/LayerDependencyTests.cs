using System.Reflection;
using NetArchTest.Rules;

namespace ArchitectureTests;

/// <summary>
/// Dependencies may only point inwards: Api → Infrastructure → Core → Shared.Kernel.
/// </summary>
[TestFixture]
public class LayerDependencyTests
{
    private static readonly Assembly SharedKernel = typeof(Shared.Kernel.Results.Result).Assembly;
    private static readonly Assembly SharedInfrastructure = typeof(Shared.Infrastructure.Persistence.TenantDbContext).Assembly;
    private static readonly Assembly Core = typeof(Core.Models.Booking).Assembly;
    private static readonly Assembly Infrastructure = typeof(Infrastructure.Data.ApplicationDbContext).Assembly;

    [Test]
    public void SharedKernel_HasNoDependenciesOnOtherLayersOrFrameworks()
    {
        AssertNoDependencies(SharedKernel,
            "Shared.Infrastructure", "Core", "Infrastructure", "Api", "Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore");
    }

    [Test]
    public void SharedInfrastructure_DoesNotDependOnApplicationLayers()
    {
        AssertNoDependencies(SharedInfrastructure, "Core", "Infrastructure", "Api");
    }

    [Test]
    public void Core_DoesNotDependOnInfrastructureOrWeb()
    {
        AssertNoDependencies(Core,
            "Infrastructure", "Shared.Infrastructure", "Api", "Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore");
    }

    [Test]
    public void Infrastructure_DoesNotDependOnApi()
    {
        AssertNoDependencies(Infrastructure, "Api", "Microsoft.AspNetCore");
    }

    private static void AssertNoDependencies(Assembly assembly, params string[] forbidden)
    {
        var result = Types.InAssembly(assembly)
            .ShouldNot()
            .HaveDependencyOnAny(forbidden)
            .GetResult();

        Assert.That(result.IsSuccessful, Is.True,
            $"{assembly.GetName().Name} must not depend on [{string.Join(", ", forbidden)}]. Offending types: " +
            string.Join(", ", result.FailingTypeNames ?? []));
    }
}

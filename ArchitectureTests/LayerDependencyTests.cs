using System.Reflection;
using NetArchTest.Rules;

namespace ArchitectureTests;

/// <summary>
/// Dependencies may only point inwards: the host and Infrastructure depend on the Shared projects, never the other way.
/// </summary>
[TestFixture]
public class LayerDependencyTests
{
    private static readonly Assembly SharedKernel = typeof(Shared.Kernel.Results.Result).Assembly;
    private static readonly Assembly SharedApplication = typeof(Shared.Application.Pagination.PagingExtensions).Assembly;
    private static readonly Assembly SharedInfrastructure = typeof(Shared.Infrastructure.Persistence.TenantDbContext).Assembly;
    private static readonly Assembly Infrastructure = typeof(Infrastructure.Data.ApplicationDbContext).Assembly;

    [Test]
    public void SharedKernel_HasNoDependenciesOnOtherLayersOrFrameworks()
    {
        AssertNoDependencies(SharedKernel,
            "Shared.Infrastructure", "Infrastructure", "Api", "Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore");
    }

    [Test]
    public void SharedApplication_DoesNotDependOnInfrastructureOrWeb()
    {
        AssertNoDependencies(SharedApplication,
            "Shared.Infrastructure", "Infrastructure", "Api", "Microsoft.AspNetCore", "Npgsql");
    }

    [Test]
    public void SharedInfrastructure_DoesNotDependOnApplicationLayers()
    {
        AssertNoDependencies(SharedInfrastructure, "Infrastructure", "Api");
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

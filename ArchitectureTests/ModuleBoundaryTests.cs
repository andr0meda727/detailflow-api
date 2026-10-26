using System.Reflection;
using NetArchTest.Rules;

namespace ArchitectureTests;

/// <summary>
/// Modules are isolated from the legacy Core/Infrastructure projects and from each other,
/// and keep their own layers pointing inwards: Api → Infrastructure → Application → Domain.
/// </summary>
[TestFixture]
public class ModuleBoundaryTests
{
    private static readonly string[] LegacyNamespaces = ["Core", "Infrastructure", "Api"];

    private static readonly Assembly[] InventoryAssemblies =
    [
        typeof(Inventory.Domain.InventoryItem).Assembly,
        typeof(Inventory.Application.Abstractions.IInventoryDbContext).Assembly,
        typeof(Inventory.Infrastructure.Persistence.InventoryDbContext).Assembly,
        typeof(Inventory.Api.InventoryModule).Assembly
    ];

    [Test]
    public void InventoryModule_DoesNotDependOnLegacyProjects()
    {
        var result = Types.InAssemblies(InventoryAssemblies)
            .ShouldNot().HaveDependencyOnAny(LegacyNamespaces)
            .GetResult();

        AssertSuccess(result, "The Inventory module must not use Core, Infrastructure or the Api host");
    }

    [Test]
    public void LegacyProjects_DoNotDependOnInventoryModule()
    {
        var legacy = new[]
        {
            typeof(Core.Models.Booking).Assembly,
            typeof(Infrastructure.Data.ApplicationDbContext).Assembly
        };

        var result = Types.InAssemblies(legacy)
            .ShouldNot().HaveDependencyOn("Inventory")
            .GetResult();

        AssertSuccess(result, "Core and Infrastructure must not reach into the Inventory module");
    }

    [Test]
    public void InventoryDomain_DependsOnNoOtherLayer()
    {
        var result = Types.InAssembly(InventoryAssemblies[0])
            .ShouldNot().HaveDependencyOnAny(
                "Inventory.Application", "Inventory.Infrastructure", "Inventory.Api",
                "Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore")
            .GetResult();

        AssertSuccess(result, "Inventory.Domain must stay free of other layers and frameworks");
    }

    [Test]
    public void InventoryApplication_DoesNotDependOnInfrastructureOrWeb()
    {
        var result = Types.InAssembly(InventoryAssemblies[1])
            .ShouldNot().HaveDependencyOnAny("Inventory.Infrastructure", "Inventory.Api", "Microsoft.AspNetCore", "Npgsql")
            .GetResult();

        AssertSuccess(result, "Inventory.Application must not use ASP.NET Core, the database provider or outer layers");
    }

    [Test]
    public void InventoryApplication_UsesOnlyEfCoreAbstractions()
    {
        // Handlers work with DbSet and LINQ (Microsoft.EntityFrameworkCore), but SQL and provider specifics
        // stay in Infrastructure. Namespaces overlap, so check the referenced assemblies instead.
        var referenced = InventoryAssemblies[1].GetReferencedAssemblies().Select(a => a.Name).ToList();

        Assert.That(referenced, Has.None.EqualTo("Microsoft.EntityFrameworkCore.Relational"));
        Assert.That(referenced, Has.None.StartsWith("Npgsql"));
    }

    [Test]
    public void LegacyProjects_UseCrmOnlyThroughItsContracts()
    {
        var legacy = new[]
        {
            typeof(Core.Models.Booking).Assembly,
            typeof(Infrastructure.Data.ApplicationDbContext).Assembly
        };

        var result = Types.InAssemblies(legacy)
            .ShouldNot().HaveDependencyOnAny("Crm.Domain", "Crm.Application", "Crm.Infrastructure", "Crm.Api")
            .GetResult();

        AssertSuccess(result, "Other parts of the system may only use Crm.Contracts");
    }

    [Test]
    public void CrmContracts_DependOnNothing()
    {
        // Contracts are what another service would consume, so they stay plain C# types
        var referenced = typeof(Crm.Contracts.IClientDirectory).Assembly.GetReferencedAssemblies()
            .Select(a => a.Name!)
            .Where(name => !name.StartsWith("System") && name != "netstandard" && name != "mscorlib")
            .ToList();

        Assert.That(referenced, Is.Empty);
    }

    private static void AssertSuccess(TestResult result, string rule)
    {
        Assert.That(result.IsSuccessful, Is.True,
            $"{rule}. Offending types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }
}

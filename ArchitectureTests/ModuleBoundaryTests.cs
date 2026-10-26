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
        typeof(Inventory.Application.Items.IInventoryService).Assembly,
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
            .ShouldNot().HaveDependencyOnAny(
                "Inventory.Infrastructure", "Inventory.Api", "Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore")
            .GetResult();

        AssertSuccess(result, "Inventory.Application must not use EF Core, ASP.NET Core or outer layers");
    }

    private static void AssertSuccess(TestResult result, string rule)
    {
        Assert.That(result.IsSuccessful, Is.True,
            $"{rule}. Offending types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }
}

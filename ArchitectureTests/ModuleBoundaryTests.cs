using System.Reflection;
using NetArchTest.Rules;

namespace ArchitectureTests;

/// <summary>
/// Modules are isolated from the legacy Core/Infrastructure projects and from each other,
/// and keep their own layers pointing inwards: Api → Infrastructure → Application → Domain.
/// The only way into a module from outside is its *.Contracts project.
/// </summary>
[TestFixture]
public class ModuleBoundaryTests
{
    public sealed record Module(string Name, Assembly Domain, Assembly Application, Assembly Infrastructure, Assembly Api)
    {
        public Assembly[] All => [Domain, Application, Infrastructure, Api];

        // Namespaces other code must not touch; *.Contracts is deliberately left out
        public string[] Internals => [$"{Name}.Domain", $"{Name}.Application", $"{Name}.Infrastructure", $"{Name}.Api"];

        public override string ToString() => Name;
    }

    public static readonly Module[] Modules =
    [
        new("Inventory",
            typeof(Inventory.Domain.InventoryItem).Assembly,
            typeof(Inventory.Application.Abstractions.IInventoryDbContext).Assembly,
            typeof(Inventory.Infrastructure.Persistence.InventoryDbContext).Assembly,
            typeof(Inventory.Api.InventoryModule).Assembly),
        new("Crm",
            typeof(Crm.Domain.Client).Assembly,
            typeof(Crm.Application.Abstractions.ICrmDbContext).Assembly,
            typeof(Crm.Infrastructure.Persistence.CrmDbContext).Assembly,
            typeof(Crm.Api.CrmModule).Assembly),
        new("Scheduling",
            typeof(Scheduling.Domain.Bookings.Booking).Assembly,
            typeof(Scheduling.Application.Abstractions.ISchedulingDbContext).Assembly,
            typeof(Scheduling.Infrastructure.Persistence.SchedulingDbContext).Assembly,
            typeof(Scheduling.Api.SchedulingModule).Assembly)
    ];

    private static readonly Assembly[] LegacyAssemblies =
    [
        typeof(Core.Models.Booking).Assembly,
        typeof(Infrastructure.Data.ApplicationDbContext).Assembly
    ];

    private static readonly string[] LegacyNamespaces = ["Core", "Infrastructure", "Api"];

    [TestCaseSource(nameof(Modules))]
    public void Module_DoesNotDependOnLegacyProjects(Module module)
    {
        var result = Types.InAssemblies(module.All)
            .ShouldNot().HaveDependencyOnAny(LegacyNamespaces)
            .GetResult();

        AssertSuccess(result, $"The {module} module must not use Core, Infrastructure or the Api host");
    }

    [TestCaseSource(nameof(Modules))]
    public void LegacyProjects_UseModuleOnlyThroughItsContracts(Module module)
    {
        var result = Types.InAssemblies(LegacyAssemblies)
            .ShouldNot().HaveDependencyOnAny(module.Internals)
            .GetResult();

        AssertSuccess(result, $"Core and Infrastructure may only use {module}.Contracts");
    }

    [TestCaseSource(nameof(Modules))]
    public void Module_UsesOtherModulesOnlyThroughTheirContracts(Module module)
    {
        var otherInternals = Modules.Where(m => m != module).SelectMany(m => m.Internals).ToArray();

        var result = Types.InAssemblies(module.All)
            .ShouldNot().HaveDependencyOnAny(otherInternals)
            .GetResult();

        AssertSuccess(result, $"The {module} module may only use other modules' *.Contracts");
    }

    [TestCaseSource(nameof(Modules))]
    public void Domain_DependsOnNoOtherLayer(Module module)
    {
        var result = Types.InAssembly(module.Domain)
            .ShouldNot().HaveDependencyOnAny(
                $"{module.Name}.Application", $"{module.Name}.Infrastructure", $"{module.Name}.Api",
                "Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore")
            .GetResult();

        AssertSuccess(result, $"{module}.Domain must stay free of other layers and frameworks");
    }

    [TestCaseSource(nameof(Modules))]
    public void Application_DoesNotDependOnInfrastructureOrWeb(Module module)
    {
        var result = Types.InAssembly(module.Application)
            .ShouldNot().HaveDependencyOnAny(
                $"{module.Name}.Infrastructure", $"{module.Name}.Api", "Microsoft.AspNetCore", "Npgsql")
            .GetResult();

        AssertSuccess(result, $"{module}.Application must not use ASP.NET Core, the database provider or outer layers");
    }

    [TestCaseSource(nameof(Modules))]
    public void Application_UsesOnlyEfCoreAbstractions(Module module)
    {
        // Handlers work with DbSet and LINQ (Microsoft.EntityFrameworkCore), but SQL and provider specifics
        // stay in Infrastructure. Namespaces overlap, so check the referenced assemblies instead.
        var referenced = module.Application.GetReferencedAssemblies().Select(a => a.Name).ToList();

        Assert.That(referenced, Has.None.EqualTo("Microsoft.EntityFrameworkCore.Relational"));
        Assert.That(referenced, Has.None.StartsWith("Npgsql"));
    }

    private static readonly Assembly[] ContractAssemblies =
    [
        typeof(Crm.Contracts.IClientDirectory).Assembly,
        typeof(Scheduling.Contracts.IBookingReportSource).Assembly
    ];

    [TestCaseSource(nameof(ContractAssemblies))]
    public void Contracts_DependOnNothing(Assembly contracts)
    {
        // Contracts are what another service would consume, so they stay plain C# types
        var referenced = contracts.GetReferencedAssemblies()
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

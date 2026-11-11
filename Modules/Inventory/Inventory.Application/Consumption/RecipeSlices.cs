using FluentValidation;
using Inventory.Application.Abstractions;
using Inventory.Domain.Consumption;
using Microsoft.EntityFrameworkCore;
using Scheduling.Contracts;
using Shared.Kernel.Results;
using Shared.Kernel.Tenancy;

namespace Inventory.Application.Consumption;

public sealed record RecipeLineDto(Guid ItemId, decimal QuantityForMedium);

public sealed record RecipeRequest(List<RecipeLineDto> Lines);

/// <summary>
/// A service's materials for a Medium car, with each item's name and unit for display.
/// </summary>
public sealed record RecipeDto(Guid ServiceId, List<RecipeLineViewDto> Lines);

public sealed record RecipeLineViewDto(Guid ItemId, string ItemName, string Unit, decimal QuantityForMedium);

public sealed record SizeFactorsDto(decimal Small, decimal Medium, decimal Large, decimal Xl)
{
    public static SizeFactorsDto From(MaterialSizeFactors f) => new(f.Small, f.Medium, f.Large, f.Xl);
}

public class RecipeRequestValidator : AbstractValidator<RecipeRequest>
{
    public RecipeRequestValidator(IInventoryDbContext db)
    {
        RuleFor(x => x.Lines).Cascade(CascadeMode.Stop).NotNull().WithMessage("Podaj listę materiałów.")
            .Must(lines => lines.Count <= ServiceRecipe.MaxLines)
            .WithMessage($"Usługa może zużywać najwyżej {ServiceRecipe.MaxLines} materiałów.")
            .Must(lines => lines.Select(l => l.ItemId).Distinct().Count() == lines.Count)
            .WithMessage("Każdy materiał można podać tylko raz.")
            // Items are tenant-filtered, so another studio's materials count as missing
            .MustAsync(async (lines, ct) =>
            {
                var ids = lines.Select(l => l.ItemId).Distinct().ToList();
                return await db.Items.CountAsync(i => ids.Contains(i.Id), ct) == ids.Count;
            })
            .WithMessage("Któryś z materiałów nie istnieje.");

        RuleForEach(x => x.Lines).ChildRules(line =>
            line.RuleFor(l => l.QuantityForMedium)
                .GreaterThan(0).WithMessage("Ilość musi być większa od zera.")
                .Must(q => decimal.Round(q, 3) == q).WithMessage("Ilość może mieć najwyżej trzy miejsca po przecinku."));
    }
}

public class SizeFactorsValidator : AbstractValidator<SizeFactorsDto>
{
    public SizeFactorsValidator()
    {
        RuleFor(x => new[] { x.Small, x.Medium, x.Large, x.Xl })
            .Must(all => all.All(f => f is >= MaterialSizeFactors.Min and <= MaterialSizeFactors.Max))
            .WithName("Factors")
            .WithMessage($"Współczynniki muszą wynosić od {MaterialSizeFactors.Min} do {MaterialSizeFactors.Max}.");
    }
}

public static class ConsumptionErrors
{
    public static readonly Error ServiceNotFound =
        Error.NotFound("SERVICE_NOT_FOUND", "Nie znaleziono usługi.");

    public static readonly Error ConsumptionNotFound =
        Error.NotFound("CONSUMPTION_NOT_FOUND", "Nie ma jeszcze zużycia dla tej wizyty (czy została zakończona?).");

    public static readonly Error UnknownLine = Error.Validation(
        "CONSUMPTION_LINE_NOT_FOUND", "Wizyta nie zużywała tego materiału w tej usłudze.");
}

public sealed class GetRecipeHandler(IInventoryDbContext db)
{
    public async Task<RecipeDto> HandleAsync(Guid serviceId, CancellationToken cancellationToken)
    {
        var recipe = await db.Recipes.AsNoTracking().FirstOrDefaultAsync(r => r.ServiceId == serviceId, cancellationToken);
        return await ToDtoAsync(db, serviceId, recipe, cancellationToken);
    }

    internal static async Task<RecipeDto> ToDtoAsync(
        IInventoryDbContext db, Guid serviceId, ServiceRecipe? recipe, CancellationToken cancellationToken)
    {
        var lines = recipe?.Lines.ToList() ?? [];
        var ids = lines.Select(l => l.ItemId).ToList();
        var items = await db.Items.AsNoTracking().Where(i => ids.Contains(i.Id)).ToDictionaryAsync(i => i.Id, cancellationToken);

        return new RecipeDto(serviceId, lines
            .Where(l => items.ContainsKey(l.ItemId))
            .Select(l => new RecipeLineViewDto(l.ItemId, items[l.ItemId].Name, items[l.ItemId].Unit, l.QuantityForMedium))
            .OrderBy(l => l.ItemName).ToList());
    }
}

public sealed class SetRecipeHandler(IInventoryDbContext db, ITenantContext tenant, IServiceCatalogSource catalog)
{
    /// <summary>
    /// Replaces what a service uses. The service must be one of the studio's active services (asked of Scheduling).
    /// </summary>
    public async Task<Result<RecipeDto>> HandleAsync(Guid serviceId, RecipeRequest request, CancellationToken cancellationToken)
    {
        var studioId = tenant.StudioId ?? throw new InvalidOperationException("A recipe needs a resolved studio.");
        var services = await catalog.GetCatalogAsync(studioId, cancellationToken);
        if (services.Services.All(s => s.ServiceId != serviceId)) return ConsumptionErrors.ServiceNotFound;

        var recipe = await db.Recipes.FirstOrDefaultAsync(r => r.ServiceId == serviceId, cancellationToken);
        if (recipe == null)
        {
            recipe = ServiceRecipe.For(serviceId);
            db.Recipes.Add(recipe);
        }

        recipe.SetLines(request.Lines.Select(l => new RecipeLine(l.ItemId, l.QuantityForMedium)).ToList());
        await db.SaveChangesAsync(cancellationToken);

        return await GetRecipeHandler.ToDtoAsync(db, serviceId, recipe, cancellationToken);
    }
}

public sealed class GetSizeFactorsHandler(IInventoryDbContext db)
{
    public async Task<SizeFactorsDto> HandleAsync(CancellationToken cancellationToken) =>
        SizeFactorsDto.From(await db.SizeFactors.AsNoTracking().FirstOrDefaultAsync(cancellationToken) ?? MaterialSizeFactors.Default());
}

public sealed class SetSizeFactorsHandler(IInventoryDbContext db)
{
    public async Task<SizeFactorsDto> HandleAsync(SizeFactorsDto request, CancellationToken cancellationToken)
    {
        var factors = await db.SizeFactors.FirstOrDefaultAsync(cancellationToken);
        if (factors == null)
        {
            factors = MaterialSizeFactors.Default();
            db.SizeFactors.Add(factors);
        }

        factors.Set(request.Small, request.Medium, request.Large, request.Xl);
        await db.SaveChangesAsync(cancellationToken);
        return SizeFactorsDto.From(factors);
    }
}

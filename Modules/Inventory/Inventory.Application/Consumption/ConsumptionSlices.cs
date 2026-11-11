using FluentValidation;
using Inventory.Application.Abstractions;
using Inventory.Domain.Consumption;
using Microsoft.EntityFrameworkCore;
using Scheduling.Contracts;
using Shared.Kernel.Results;
using Shared.Kernel.Tenancy;
using Shared.Kernel.Vehicles;

namespace Inventory.Application.Consumption;

public sealed record ConsumptionLineDto(
    Guid ServiceId, Guid ItemId, string ItemName, string Unit, decimal BaseQuantity, decimal EstimatedQuantity,
    decimal? ActualQuantity);

public sealed record ConsumptionDto(Guid BookingId, VehicleSize VehicleSize, DateTime CompletedAt, List<ConsumptionLineDto> Lines);

public sealed record ActualLineDto(Guid ServiceId, Guid ItemId, decimal ActualQuantity);

public sealed record CorrectConsumptionRequest(List<ActualLineDto> Lines);

/// <summary>
/// How good the estimates for one material in one service are: the learned factor, from how many visits, and the
/// mean absolute percentage error of the estimates staff corrected.
/// </summary>
public sealed record AccuracyDto(
    Guid ServiceId, Guid ItemId, string ItemName, decimal Factor, bool FactorApplied, int Samples, decimal? MeanAbsolutePercentageError);

/// <summary>
/// One material's outlook. <see cref="AverageDailyUse"/> is over the last 30 days; <see cref="PlannedUse"/> is what the
/// visits already booked in the next two weeks will take; <see cref="RunsOutAt"/> is the start of the first booked
/// visit stock won't cover, if any.
/// </summary>
public sealed record ForecastDto(
    Guid ItemId, string Name, string Unit, decimal Stock, decimal? MinStock, decimal AverageDailyUse, decimal? DaysLeft,
    decimal PlannedUse, decimal StockAfterPlanned, DateTime? RunsOutAt, bool IsLowStock);

public class CorrectConsumptionRequestValidator : AbstractValidator<CorrectConsumptionRequest>
{
    public CorrectConsumptionRequestValidator()
    {
        RuleFor(x => x.Lines).NotEmpty().WithMessage("Podaj przynajmniej jeden materiał.");
        RuleForEach(x => x.Lines).ChildRules(line =>
            line.RuleFor(l => l.ActualQuantity)
                .GreaterThanOrEqualTo(0).WithMessage("Zużycie nie może być ujemne.")
                .Must(q => decimal.Round(q, 3) == q).WithMessage("Ilość może mieć najwyżej trzy miejsca po przecinku."));
    }
}

public sealed class GetConsumptionHandler(IInventoryDbContext db)
{
    public async Task<Result<ConsumptionDto>> HandleAsync(Guid bookingId, CancellationToken cancellationToken)
    {
        // Tenant-filtered, so another studio's visit is not found
        var consumption = await db.Consumptions.AsNoTracking().FirstOrDefaultAsync(c => c.BookingId == bookingId, cancellationToken);
        if (consumption == null) return ConsumptionErrors.ConsumptionNotFound;

        return await ToDtoAsync(db, consumption, cancellationToken);
    }

    internal static async Task<ConsumptionDto> ToDtoAsync(
        IInventoryDbContext db, BookingConsumption consumption, CancellationToken cancellationToken)
    {
        var ids = consumption.Lines.Select(l => l.ItemId).Distinct().ToList();
        var items = await db.Items.AsNoTracking().Where(i => ids.Contains(i.Id)).ToDictionaryAsync(i => i.Id, cancellationToken);

        return new ConsumptionDto(consumption.BookingId, consumption.VehicleSize, consumption.CompletedAt, consumption.Lines
            .Where(l => items.ContainsKey(l.ItemId))
            .Select(l => new ConsumptionLineDto(
                l.ServiceId, l.ItemId, items[l.ItemId].Name, items[l.ItemId].Unit, l.BaseQuantity, l.EstimatedQuantity, l.ActualQuantity))
            .OrderBy(l => l.ItemName).ThenBy(l => l.ServiceId)
            .ToList());
    }
}

public sealed class CorrectConsumptionHandler(IInventoryDbContext db, ITenantContext tenant)
{
    /// <summary>
    /// Staff say what the visit really used. Stock moves by the difference from what was deducted so far, and each
    /// line's first actual amount teaches the calibration for that service and material (later edits only fix stock,
    /// so one visit is never learned twice).
    /// </summary>
    public async Task<Result<ConsumptionDto>> HandleAsync(
        Guid bookingId, CorrectConsumptionRequest request, CancellationToken cancellationToken)
    {
        var consumption = await db.Consumptions.FirstOrDefaultAsync(c => c.BookingId == bookingId, cancellationToken);
        if (consumption == null) return ConsumptionErrors.ConsumptionNotFound;
        if (request.Lines.Any(l => !consumption.Has(l.ServiceId, l.ItemId))) return ConsumptionErrors.UnknownLine;

        var before = consumption.UsedPerItem();
        var serviceIds = request.Lines.Select(l => l.ServiceId).Distinct().ToList();
        var calibrations = await db.Calibrations.Where(c => serviceIds.Contains(c.ServiceId)).ToListAsync(cancellationToken);

        foreach (var actual in request.Lines.DistinctBy(l => (l.ServiceId, l.ItemId)))
        {
            var line = consumption.Lines.Single(l => l.ServiceId == actual.ServiceId && l.ItemId == actual.ItemId);
            if (!consumption.SetActual(actual.ServiceId, actual.ItemId, actual.ActualQuantity)) continue;
            if (line.BaseQuantity <= 0) continue;

            var calibration = calibrations.FirstOrDefault(c => c.ServiceId == actual.ServiceId && c.ItemId == actual.ItemId);
            if (calibration == null)
            {
                calibration = ConsumptionCalibration.Start(consumption.StudioId, actual.ServiceId, actual.ItemId);
                calibrations.Add(calibration);
                db.Calibrations.Add(calibration);
            }

            calibration.Learn(line.BaseQuantity, line.EstimatedQuantity, actual.ActualQuantity);
        }

        var after = consumption.UsedPerItem();
        var itemIds = after.Keys.ToList();
        var items = await db.Items.Where(i => itemIds.Contains(i.Id)).ToListAsync(cancellationToken);
        foreach (var item in items) item.Correct(after[item.Id] - before.GetValueOrDefault(item.Id), bookingId, tenant.UserId);

        await db.SaveChangesAsync(cancellationToken);
        return await GetConsumptionHandler.ToDtoAsync(db, consumption, cancellationToken);
    }
}

public sealed class GetAccuracyHandler(IInventoryDbContext db)
{
    public async Task<List<AccuracyDto>> HandleAsync(CancellationToken cancellationToken)
    {
        var calibrations = await db.Calibrations.AsNoTracking().ToListAsync(cancellationToken);
        var ids = calibrations.Select(c => c.ItemId).Distinct().ToList();
        var names = await db.Items.AsNoTracking().Where(i => ids.Contains(i.Id)).ToDictionaryAsync(i => i.Id, i => i.Name, cancellationToken);

        return calibrations
            .Where(c => names.ContainsKey(c.ItemId))
            .Select(c => new AccuracyDto(
                c.ServiceId, c.ItemId, names[c.ItemId], c.Factor, c.SampleCount >= ConsumptionCalibration.WarmUpSamples,
                c.SampleCount, c.MeanAbsolutePercentageError is { } mape ? Math.Round(mape, 4) : null))
            .OrderBy(a => a.ItemName).ThenBy(a => a.ServiceId)
            .ToList();
    }
}

public sealed class GetForecastHandler(
    IInventoryDbContext db, ITenantContext tenant, IUpcomingBookingsSource upcoming, TimeProvider clock)
{
    public const int HistoryDays = 30;
    public const int PlannedDays = 14;

    /// <summary>
    /// For every material: how fast it goes (from the ledger) and whether the visits already booked will run it out.
    /// </summary>
    public async Task<List<ForecastDto>> HandleAsync(CancellationToken cancellationToken)
    {
        var studioId = tenant.StudioId ?? throw new InvalidOperationException("The forecast needs a resolved studio.");
        var now = clock.GetUtcNow().UtcDateTime;
        var since = now.AddDays(-HistoryDays);

        var items = await db.Items.AsNoTracking().OrderBy(i => i.Name).ThenBy(i => i.Id).ToListAsync(cancellationToken);

        // Consumption is negative and its corrections go either way; together they are what visits really took
        var used = await db.Movements.AsNoTracking()
            .Where(m => m.CreatedAt >= since
                        && (m.Type == Domain.Stock.StockMovementType.Consumption || m.Type == Domain.Stock.StockMovementType.Correction))
            .GroupBy(m => m.ItemId)
            .Select(g => new { ItemId = g.Key, Used = -g.Sum(m => m.Quantity) })
            .ToDictionaryAsync(x => x.ItemId, x => x.Used, cancellationToken);

        var planned = await PlannedAsync(studioId, now, cancellationToken);

        return items.Select(item =>
        {
            var daily = Math.Round(Math.Max(0, used.GetValueOrDefault(item.Id)) / HistoryDays, 3);
            var bookings = planned.GetValueOrDefault(item.Id) ?? [];
            var plannedUse = bookings.Sum(b => b.Quantity);

            DateTime? runsOutAt = null;
            var left = item.Stock;
            foreach (var (at, quantity) in bookings)
            {
                left -= quantity;
                if (left < 0) { runsOutAt = at; break; }
            }

            return new ForecastDto(
                item.Id, item.Name, item.Unit, item.Stock, item.MinStock, daily,
                daily > 0 ? Math.Round(Math.Max(0, item.Stock) / daily, 1) : null,
                plannedUse, item.Stock - plannedUse, runsOutAt, item.IsLowStock);
        }).ToList();
    }

    // Per item, the booked visits of the next two weeks that use it, in time order, with how much each will take
    private async Task<Dictionary<Guid, List<(DateTime At, decimal Quantity)>>> PlannedAsync(
        Guid studioId, DateTime now, CancellationToken cancellationToken)
    {
        var bookings = await upcoming.GetUpcomingAsync(studioId, now, now.AddDays(PlannedDays), cancellationToken);
        var result = new Dictionary<Guid, List<(DateTime, decimal)>>();
        if (bookings.Count == 0) return result;

        // Loaded once for all the bookings (tenant-filtered: this studio's)
        var recipes = await db.Recipes.AsNoTracking().ToListAsync(cancellationToken);
        var factors = await db.SizeFactors.AsNoTracking().FirstOrDefaultAsync(cancellationToken) ?? MaterialSizeFactors.Default();
        var calibrations = await db.Calibrations.AsNoTracking().ToListAsync(cancellationToken);

        foreach (var booking in bookings.OrderBy(b => b.ScheduledAt))
        {
            var estimate = ConsumptionEstimator.Estimate(
                recipes.Where(r => booking.ServiceIds.Contains(r.ServiceId)), VehicleSizes.Parse(booking.VehicleSize),
                factors, calibrations);

            foreach (var perItem in estimate.GroupBy(l => l.ItemId))
            {
                if (!result.TryGetValue(perItem.Key, out var list)) result[perItem.Key] = list = [];
                list.Add((booking.ScheduledAt, perItem.Sum(l => l.EstimatedQuantity)));
            }
        }

        return result;
    }
}

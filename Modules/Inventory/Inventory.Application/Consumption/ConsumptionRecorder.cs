using Inventory.Application.Abstractions;
using Inventory.Domain.Consumption;
using Microsoft.EntityFrameworkCore;
using Shared.Kernel.Vehicles;

namespace Inventory.Application.Consumption;

/// <summary>
/// Turns a completed visit into material use: estimates it from the services' recipes for the car's size and what
/// the studio has learned, records it once per booking, and takes it off stock. It runs on the bus without a tenant,
/// so it reads through the *OfAnyStudio sets and matches the studio from the event every time.
/// </summary>
public sealed class ConsumptionRecorder(IInventoryDbContext db)
{
    /// <summary>
    /// Returns false when the booking was already recorded (the event came again), changing nothing.
    /// </summary>
    public async Task<bool> RecordAsync(
        Guid studioId, Guid bookingId, VehicleSize size, IReadOnlyCollection<Guid> serviceIds, DateTime completedAt,
        CancellationToken cancellationToken)
    {
        if (await db.ConsumptionsOfAnyStudio.AnyAsync(c => c.StudioId == studioId && c.BookingId == bookingId, cancellationToken))
            return false;

        var estimate = await EstimateAsync(db, studioId, size, serviceIds, cancellationToken);
        var consumption = BookingConsumption.Record(studioId, bookingId, size, completedAt, estimate);
        db.Consumptions.Add(consumption);

        var used = consumption.UsedPerItem();
        var ids = used.Keys.ToList();
        var items = await db.ItemsOfAnyStudio
            .Where(i => i.StudioId == studioId && ids.Contains(i.Id))
            .ToListAsync(cancellationToken);
        foreach (var item in items) item.Consume(used[item.Id], bookingId);

        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <summary>
    /// What a visit would use in this studio, line by line. Also used for the forecast of booked visits.
    /// </summary>
    public static async Task<IReadOnlyList<EstimatedLine>> EstimateAsync(
        IInventoryDbContext db, Guid studioId, VehicleSize size, IReadOnlyCollection<Guid> serviceIds,
        CancellationToken cancellationToken)
    {
        var ids = serviceIds.Distinct().ToList();
        var recipes = await db.RecipesOfAnyStudio.AsNoTracking()
            .Where(r => r.StudioId == studioId && ids.Contains(r.ServiceId))
            .ToListAsync(cancellationToken);
        var factors = await db.SizeFactorsOfAnyStudio.AsNoTracking()
            .FirstOrDefaultAsync(f => f.StudioId == studioId, cancellationToken) ?? MaterialSizeFactors.Default();
        var calibrations = await db.CalibrationsOfAnyStudio.AsNoTracking()
            .Where(c => c.StudioId == studioId && ids.Contains(c.ServiceId))
            .ToListAsync(cancellationToken);

        return ConsumptionEstimator.Estimate(recipes, size, factors, calibrations);
    }
}

using Scheduling.Application.Abstractions;
using Scheduling.Domain.Planning;
using Shared.Kernel.Results;

namespace Scheduling.Application.Planning.Schedule;

// The schedule is one small aggregate per studio, so its four use cases share a file.

public sealed class GetScheduleHandler(ISchedulingDbContext db)
{
    /// <summary>
    /// The studio's schedule, or the defaults (no opening hours, so planning is off) when it never set one.
    /// </summary>
    public async Task<ScheduleDto> HandleAsync(CancellationToken cancellationToken)
    {
        var schedule = await db.FindOrCreateScheduleAsync(addIfMissing: false, cancellationToken);
        return await db.ToDtoAsync(schedule, cancellationToken);
    }
}

public sealed class UpdateScheduleHandler(ISchedulingDbContext db)
{
    public async Task<Result<ScheduleDto>> HandleAsync(ScheduleRequest request, CancellationToken cancellationToken)
    {
        var schedule = await db.FindOrCreateScheduleAsync(addIfMissing: true, cancellationToken);

        var hours = schedule.SetOpeningHours(
            request.OpeningHours.Select(p => new OpeningPeriod(p.Day, p.Opens, p.Closes)).ToList());
        if (!hours.Success) return hours.Error!;

        var rules = schedule.SetRules(
            request.BufferMinutes, request.SlotStepMinutes, request.MinLeadHours,
            request.DurationMultipliers?.ToDomain() ?? schedule.Multipliers);
        if (!rules.Success) return rules.Error!;

        await db.SaveChangesAsync(cancellationToken);
        return await db.ToDtoAsync(schedule, cancellationToken);
    }
}

public sealed class CloseDayHandler(ISchedulingDbContext db)
{
    /// <summary>
    /// Closes the studio for a local day (a holiday). Existing bookings that day stay; staff move them themselves.
    /// </summary>
    public async Task<ScheduleDto> HandleAsync(ClosureRequest request, CancellationToken cancellationToken)
    {
        var schedule = await db.FindOrCreateScheduleAsync(addIfMissing: true, cancellationToken);

        schedule.Close(request.Date, request.Reason);
        await db.SaveChangesAsync(cancellationToken);

        return await db.ToDtoAsync(schedule, cancellationToken);
    }
}

public sealed class ReopenDayHandler(ISchedulingDbContext db)
{
    public static readonly Error NotClosed = Error.NotFound("CLOSURE_NOT_FOUND", "Ten dzień nie jest oznaczony jako zamknięty.");

    public async Task<Result> HandleAsync(DateOnly date, CancellationToken cancellationToken)
    {
        var schedule = await db.FindOrCreateScheduleAsync(addIfMissing: false, cancellationToken);
        if (!schedule.Reopen(date)) return NotClosed;

        await db.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }
}

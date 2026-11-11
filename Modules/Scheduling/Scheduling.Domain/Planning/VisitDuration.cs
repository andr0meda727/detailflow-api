using Shared.Kernel.Vehicles;

namespace Scheduling.Domain.Planning;

public static class VisitDuration
{
    /// <summary>
    /// How long a visit occupies a bay: the services' times for a Medium car, added up, scaled for the car's size and
    /// rounded up to the studio's grid, so visits start and end on it. The cleanup buffer is not included; the planner
    /// keeps it free around every visit.
    /// </summary>
    public static TimeSpan For(IEnumerable<int> serviceMinutes, VehicleSize size, StudioSchedule schedule) =>
        For(serviceMinutes, schedule.MultiplierFor(size), schedule.SlotStepMinutes);

    public static TimeSpan For(IEnumerable<int> serviceMinutes, decimal multiplier, int stepMinutes)
    {
        if (stepMinutes <= 0) throw new ArgumentOutOfRangeException(nameof(stepMinutes), stepMinutes, "The grid must be positive.");

        var scaled = serviceMinutes.Sum() * multiplier;
        var steps = Math.Max(1, (int)Math.Ceiling(scaled / stepMinutes));
        return TimeSpan.FromMinutes(steps * stepMinutes);
    }
}

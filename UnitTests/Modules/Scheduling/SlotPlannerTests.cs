using Scheduling.Domain.Planning;
using Shared.Kernel.Time;
using Shared.Kernel.Vehicles;

namespace Unit.Modules.Scheduling;

[TestFixture]
public class SlotPlannerTests
{
    private static readonly TimeZoneInfo Warsaw = TimeZones.Find("Europe/Warsaw");
    private static readonly DateOnly Monday = new(2026, 11, 16);
    private static readonly Guid BayA = new("aaaaaaaa-0000-0000-0000-000000000000");
    private static readonly Guid BayB = new("bbbbbbbb-0000-0000-0000-000000000000");

    private static DateTime At(DateOnly day, int hour, int minute = 0) =>
        TimeZones.ToUtc(day.ToDateTime(new TimeOnly(hour, minute)), Warsaw);

    private static string Local(DateTime utc) => TimeZones.ToLocal(utc, Warsaw).ToString("HH:mm");

    private static StudioSchedule Schedule(params (DayOfWeek Day, int Opens, int Closes)[] periods)
    {
        var schedule = StudioSchedule.Create();
        schedule.SetOpeningHours(periods.Select(p => new OpeningPeriod(p.Day, new TimeOnly(p.Opens, 0), new TimeOnly(p.Closes, 0))).ToList());
        return schedule;
    }

    private static PlanningInput Input(
        StudioSchedule schedule, TimeSpan duration, DateOnly? day = null, Guid[]? bays = null, BusyBay[]? busy = null,
        DateTime? earliest = null, DateTime? preferred = null, TimeSpan? minUsefulGap = null, double wasteWeight = 0.5) =>
        new(schedule, Warsaw, day ?? Monday, day ?? Monday, bays ?? [BayA], busy ?? [], duration,
            earliest ?? DateTime.MinValue, preferred, minUsefulGap, wasteWeight);

    private static BusyBay Busy(Guid bay, DateOnly day, int fromHour, int toHour) => new(bay, new TimeRange(At(day, fromHour), At(day, toHour)));

    [Test]
    public void EmptyBay_OffersEveryGridStart_WhereTheWholeVisitFits()
    {
        var slots = SlotPlanner.FindSlots(Input(Schedule((DayOfWeek.Monday, 8, 16)), TimeSpan.FromHours(2)));

        Assert.That(slots, Has.Count.EqualTo(25));
        Assert.That(Local(slots[0].Range.Start), Is.EqualTo("08:00"));
        Assert.That(Local(slots[^1].Range.Start), Is.EqualTo("14:00"));
        Assert.That(Local(slots[^1].Range.End), Is.EqualTo("16:00"));
    }

    [Test]
    public void Booking_KeepsItsBufferFree_OnBothSides()
    {
        var slots = SlotPlanner.FindSlots(Input(
            Schedule((DayOfWeek.Monday, 8, 16)), TimeSpan.FromHours(2), busy: [Busy(BayA, Monday, 10, 12)]));

        // 08:00-10:00 would touch the booking with no cleanup time; after it, the buffer pushes the start to 12:15
        Assert.That(slots.Select(s => Local(s.Range.Start)), Does.Not.Contain("08:00"));
        Assert.That(Local(slots[0].Range.Start), Is.EqualTo("12:15"));
        Assert.That(slots.All(s => !s.Range.Overlaps(new TimeRange(At(Monday, 9, 45), At(Monday, 12, 15)))), Is.True);
    }

    [Test]
    public void Visit_NeverSpansTheLunchBreak()
    {
        var schedule = Schedule((DayOfWeek.Monday, 8, 12), (DayOfWeek.Monday, 13, 17));

        var slots = SlotPlanner.FindSlots(Input(schedule, TimeSpan.FromHours(3)));

        Assert.That(slots.Select(s => Local(s.Range.Start)),
            Is.EqualTo(new[] { "08:00", "08:15", "08:30", "08:45", "09:00", "13:00", "13:15", "13:30", "13:45", "14:00" }));
    }

    [Test]
    public void ClosedDay_OffersNothing()
    {
        var schedule = Schedule((DayOfWeek.Monday, 8, 16));
        schedule.Close(Monday, "Święto");

        Assert.That(SlotPlanner.FindSlots(Input(schedule, TimeSpan.FromHours(1))), Is.Empty);
    }

    [Test]
    public void DayWithoutHours_OffersNothing()
    {
        Assert.That(SlotPlanner.FindSlots(Input(Schedule((DayOfWeek.Tuesday, 8, 16)), TimeSpan.FromHours(1))), Is.Empty);
    }

    [Test]
    public void SpringForward_TheNightIsAnHourShorter()
    {
        // 29 March 2026: in Warsaw the clocks jump from 02:00 to 03:00, so 01:00-05:00 is three real hours
        var day = new DateOnly(2026, 3, 29);
        var schedule = Schedule((DayOfWeek.Sunday, 1, 5));

        Assert.That(SlotPlanner.FindSlots(Input(schedule, TimeSpan.FromHours(4), day)), Is.Empty);
        var slots = SlotPlanner.FindSlots(Input(schedule, TimeSpan.FromHours(3), day));
        Assert.That(slots, Has.Count.EqualTo(1));
        Assert.That(slots[0].Range.Start, Is.EqualTo(new DateTime(2026, 3, 29, 0, 0, 0, DateTimeKind.Utc)));
    }

    [Test]
    public void FallBack_TheNightIsAnHourLonger()
    {
        // 25 October 2026: 03:00 becomes 02:00 again, so 01:00-05:00 is five real hours
        var day = new DateOnly(2026, 10, 25);
        var slots = SlotPlanner.FindSlots(Input(Schedule((DayOfWeek.Sunday, 1, 5)), TimeSpan.FromHours(5), day));

        Assert.That(slots, Has.Count.EqualTo(1));
        Assert.That(slots[0].Range.Length, Is.EqualTo(TimeSpan.FromHours(5)));
    }

    [Test]
    public void BestFit_PutsTheVisitWhereItLeavesTheLeastTime()
    {
        // Bay A is booked from 12:00, so a visit ending at its buffer fills A's morning exactly; B is empty all day
        var slots = SlotPlanner.FindSlots(Input(
            Schedule((DayOfWeek.Monday, 8, 16)), TimeSpan.FromHours(2), bays: [BayA, BayB], busy: [Busy(BayA, Monday, 12, 16)]));

        var tight = slots.Single(s => Local(s.Range.Start) == "09:45");
        Assert.That(tight.BayId, Is.EqualTo(BayA));
        Assert.That(tight.OtherBayIds, Is.EqualTo(new[] { BayB }));
        Assert.That(slots.Single(s => Local(s.Range.Start) == "13:00").BayId, Is.EqualTo(BayB));
    }

    [Test]
    public void BestFit_AvoidsLeavingAGapNoServiceCouldUse()
    {
        // At 10:00, A (free 08:00-11:45) would have less time left over, but its last 15 minutes before the booking
        // could hold no service. B (free from 10:00 on) takes the visit with nothing wasted
        var bFreeFromTen = new BusyBay(BayB, new TimeRange(At(Monday, 8), At(Monday, 9, 45)));
        var slots = SlotPlanner.FindSlots(Input(
            Schedule((DayOfWeek.Monday, 8, 16)), TimeSpan.FromHours(1.5), bays: [BayA, BayB],
            busy: [Busy(BayA, Monday, 12, 16), bFreeFromTen], minUsefulGap: TimeSpan.FromHours(1)));

        var slot = slots.Single(s => Local(s.Range.Start) == "10:00");
        Assert.That(slot.BayId, Is.EqualTo(BayB));
        Assert.That(slot.WastedMinutes, Is.Zero);
        Assert.That(slot.OtherBayIds, Is.EqualTo(new[] { BayA }));
    }

    [Test]
    public void PreferredTime_ComesFirst_ThenTheNearestOnes()
    {
        var slots = SlotPlanner.FindSlots(Input(
            Schedule((DayOfWeek.Monday, 8, 16)), TimeSpan.FromHours(2), preferred: At(Monday, 10)));

        Assert.That(slots.Take(3).Select(s => Local(s.Range.Start)), Is.EqualTo(new[] { "10:00", "09:45", "10:15" }));
        Assert.That(slots[0].Cost, Is.Zero);
    }

    [Test]
    public void PreferredTime_ThatIsTaken_SuggestsTheClosestFreeTimes()
    {
        var slots = SlotPlanner.FindSlots(Input(
            Schedule((DayOfWeek.Monday, 8, 16)), TimeSpan.FromHours(2), busy: [Busy(BayA, Monday, 10, 12)],
            preferred: At(Monday, 10)));

        Assert.That(Local(slots[0].Range.Start), Is.EqualTo("12:15"));
    }

    [Test]
    public void WasteWeight_CanOutweighASmallShift()
    {
        // Only bay A, free 08:00-16:00. Starting at the preferred 08:15 wastes the first 15 minutes; at weight 2 that
        // costs 30, more than moving the visit 15 minutes earlier
        var slots = SlotPlanner.FindSlots(Input(
            Schedule((DayOfWeek.Monday, 8, 16)), TimeSpan.FromHours(2), preferred: At(Monday, 8, 15),
            minUsefulGap: TimeSpan.FromHours(1), wasteWeight: 2));

        Assert.That(Local(slots[0].Range.Start), Is.EqualTo("08:00"));
    }

    [Test]
    public void EarliestStart_DropsSlotsBeforeIt_AndStaysOnTheGrid()
    {
        var slots = SlotPlanner.FindSlots(Input(
            Schedule((DayOfWeek.Monday, 8, 16)), TimeSpan.FromHours(1), earliest: At(Monday, 11, 7).AddSeconds(30)));

        Assert.That(Local(slots[0].Range.Start), Is.EqualTo("11:15"));
    }

    [Test]
    public void SearchesSeveralDays_ButNotMoreThanTwoWeeks()
    {
        var schedule = Schedule((DayOfWeek.Monday, 8, 9), (DayOfWeek.Tuesday, 8, 9));
        var twoDays = Input(schedule, TimeSpan.FromHours(1)) with { To = Monday.AddDays(1) };

        Assert.That(SlotPlanner.FindSlots(twoDays), Has.Count.EqualTo(2));
        Assert.That(() => SlotPlanner.FindSlots(twoDays with { To = Monday.AddDays(14) }), Throws.InstanceOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void Check_TellsWhetherAStartFits_AndOnWhichBay()
    {
        var input = Input(Schedule((DayOfWeek.Monday, 8, 16)), TimeSpan.FromHours(2), bays: [BayA, BayB], busy: [Busy(BayA, Monday, 10, 12)]);

        Assert.That(SlotPlanner.Check(input, At(Monday, 10))?.BayId, Is.EqualTo(BayB));
        Assert.That(SlotPlanner.Check(input, At(Monday, 10), BayA), Is.Null);
        Assert.That(SlotPlanner.Check(input, At(Monday, 15)), Is.Null, "would run past closing");
        Assert.That(SlotPlanner.Check(input, At(Monday, 10, 5)), Is.Null, "off the grid");
    }

    [Test]
    public void IsBayFree_IgnoresOpeningHours_ButKeepsTheBuffer()
    {
        BusyBay[] busy = [Busy(BayA, Monday, 10, 12)];
        var buffer = TimeSpan.FromMinutes(15);

        Assert.That(SlotPlanner.IsBayFree(busy, BayA, new TimeRange(At(Monday, 20), At(Monday, 22)), buffer), Is.True);
        Assert.That(SlotPlanner.IsBayFree(busy, BayA, new TimeRange(At(Monday, 12, 5), At(Monday, 13)), buffer), Is.False);
        Assert.That(SlotPlanner.IsBayFree(busy, BayB, new TimeRange(At(Monday, 10), At(Monday, 12)), buffer), Is.True);
    }

    [Test]
    public void TimeRange_SubtractAndMerge()
    {
        var day = new TimeRange(At(Monday, 8), At(Monday, 16));
        var holes = new[]
        {
            new TimeRange(At(Monday, 11), At(Monday, 12)), new TimeRange(At(Monday, 9), At(Monday, 10)),
            new TimeRange(At(Monday, 9, 30), At(Monday, 10, 30)), new TimeRange(At(Monday, 15), At(Monday, 17))
        };

        var free = TimeRange.Subtract([day], holes);

        Assert.That(free.Select(r => $"{Local(r.Start)}-{Local(r.End)}"),
            Is.EqualTo(new[] { "08:00-09:00", "10:30-11:00", "12:00-15:00" }));
        Assert.That(TimeRange.Merge(holes), Has.Count.EqualTo(3));
    }

    [TestCase(new[] { 60, 30 }, VehicleSize.Large, 120)]
    [TestCase(new[] { 60 }, VehicleSize.Small, 60)]
    [TestCase(new[] { 60 }, VehicleSize.Medium, 60)]
    [TestCase(new[] { 90 }, VehicleSize.XL, 135)]
    [TestCase(new int[0], VehicleSize.Medium, 15)]
    public void VisitDuration_ScalesBySizeAndRoundsUpToTheGrid(int[] minutes, VehicleSize size, int expected)
    {
        Assert.That(VisitDuration.For(minutes, size, StudioSchedule.Create()), Is.EqualTo(TimeSpan.FromMinutes(expected)));
    }

    [Test]
    public void TimeZones_ToUtc_MapsASkippedTimeToTheJump()
    {
        var skipped = new DateTime(2026, 3, 29, 2, 30, 0);

        Assert.That(TimeZones.ToUtc(skipped, Warsaw), Is.EqualTo(new DateTime(2026, 3, 29, 1, 0, 0, DateTimeKind.Utc)));
    }
}

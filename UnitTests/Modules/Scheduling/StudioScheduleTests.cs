using Scheduling.Domain.Planning;
using Shared.Kernel.Vehicles;

namespace Unit.Modules.Scheduling;

[TestFixture]
public class StudioScheduleTests
{
    private static OpeningPeriod Period(DayOfWeek day, int opens, int closes) =>
        new(day, new TimeOnly(opens, 0), new TimeOnly(closes, 0));

    [Test]
    public void Create_HasNoHoursAndTheDefaultRules()
    {
        var schedule = StudioSchedule.Create();

        Assert.Multiple(() =>
        {
            Assert.That(schedule.HasOpeningHours, Is.False);
            Assert.That(schedule.BufferMinutes, Is.EqualTo(15));
            Assert.That(schedule.SlotStepMinutes, Is.EqualTo(15));
            Assert.That(schedule.MultiplierFor(VehicleSize.Medium), Is.EqualTo(1m));
            Assert.That(schedule.MultiplierFor(VehicleSize.XL), Is.EqualTo(1.5m));
        });
    }

    [Test]
    public void SetOpeningHours_AllowsABreak_AndSortsThePeriods()
    {
        var schedule = StudioSchedule.Create();

        var result = schedule.SetOpeningHours(
            [Period(DayOfWeek.Monday, 13, 17), Period(DayOfWeek.Monday, 8, 12), Period(DayOfWeek.Sunday, 10, 14)]);

        Assert.That(result.Success, Is.True);
        Assert.That(schedule.OpeningHours.Select(p => (p.Day, p.Opens.Hour)),
            Is.EqualTo(new[] { (DayOfWeek.Sunday, 10), (DayOfWeek.Monday, 8), (DayOfWeek.Monday, 13) }));
    }

    [Test]
    public void SetOpeningHours_AcceptsTouchingPeriods()
    {
        var result = StudioSchedule.Create().SetOpeningHours([Period(DayOfWeek.Friday, 8, 12), Period(DayOfWeek.Friday, 12, 16)]);

        Assert.That(result.Success, Is.True);
    }

    [Test]
    public void SetOpeningHours_RejectsOverlaps_AndKeepsTheOldHours()
    {
        var schedule = StudioSchedule.Create();
        schedule.SetOpeningHours([Period(DayOfWeek.Monday, 8, 16)]);

        var result = schedule.SetOpeningHours([Period(DayOfWeek.Monday, 8, 12), Period(DayOfWeek.Monday, 11, 16)]);

        Assert.That(result.Error?.Code, Is.EqualTo("OVERLAPPING_OPENING_PERIODS"));
        Assert.That(schedule.OpeningHours, Has.Count.EqualTo(1));
    }

    [Test]
    public void SetOpeningHours_RejectsAPeriodThatEndsBeforeItStarts()
    {
        var result = StudioSchedule.Create().SetOpeningHours([Period(DayOfWeek.Monday, 16, 8)]);

        Assert.That(result.Error?.Code, Is.EqualTo("INVALID_OPENING_PERIOD"));
    }

    [Test]
    public void SetOpeningHours_RejectsTooManyPeriodsInADay()
    {
        var periods = Enumerable.Range(0, 5).Select(i => Period(DayOfWeek.Monday, 8 + 2 * i, 9 + 2 * i)).ToList();

        Assert.That(StudioSchedule.Create().SetOpeningHours(periods).Error?.Code, Is.EqualTo("TOO_MANY_OPENING_PERIODS"));
    }

    [TestCase(-1, 15, 12)]
    [TestCase(121, 15, 12)]
    [TestCase(15, 20, 12)]
    [TestCase(15, 15, -1)]
    public void SetRules_RejectsValuesOutOfRange(int buffer, int step, int lead)
    {
        var result = StudioSchedule.Create().SetRules(buffer, step, lead, new SizeMultipliers(1, 1, 1, 1));

        Assert.That(result.Error?.Code, Is.EqualTo("INVALID_PLANNING_RULES"));
    }

    [Test]
    public void SetRules_RejectsAMultiplierOutOfRange()
    {
        var result = StudioSchedule.Create().SetRules(15, 15, 12, new SizeMultipliers(0.1m, 1, 1, 1));

        Assert.That(result.Success, Is.False);
    }

    [Test]
    public void PeriodsOn_ReturnsThatWeekday_AndNothingOnAClosure()
    {
        var schedule = StudioSchedule.Create();
        schedule.SetOpeningHours([Period(DayOfWeek.Monday, 8, 16), Period(DayOfWeek.Tuesday, 9, 17)]);
        var monday = new DateOnly(2026, 11, 16);

        Assert.That(schedule.PeriodsOn(monday).Single().Opens.Hour, Is.EqualTo(8));

        schedule.Close(monday, " Święto ");
        Assert.That(schedule.PeriodsOn(monday), Is.Empty);
        Assert.That(schedule.Closures.Single().Reason, Is.EqualTo("Święto"));
        Assert.That(schedule.PeriodsOn(monday.AddDays(1)), Has.Count.EqualTo(1));
    }

    [Test]
    public void Close_TheSameDayTwice_KeepsOneClosureWithTheNewReason()
    {
        var schedule = StudioSchedule.Create();
        var day = new DateOnly(2026, 12, 24);

        schedule.Close(day, "Wigilia");
        schedule.Close(day, "Święta");

        Assert.That(schedule.Closures.Single().Reason, Is.EqualTo("Święta"));
        Assert.That(schedule.Reopen(day), Is.True);
        Assert.That(schedule.Reopen(day), Is.False);
    }

    [Test]
    public void Bay_IsCreatedActiveWithATrimmedName_AndCanBeDeactivated()
    {
        var bay = Bay.Create("  Stanowisko 1 ");

        Assert.That(bay.Name, Is.EqualTo("Stanowisko 1"));
        Assert.That(bay.IsActive, Is.True);

        bay.Deactivate();
        Assert.That(bay.IsActive, Is.False);
        Assert.That(() => Bay.Create(" "), Throws.ArgumentException);
    }
}

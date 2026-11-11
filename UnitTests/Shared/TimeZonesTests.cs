using Shared.Kernel.Time;

namespace Unit.Shared;

[TestFixture]
public class TimeZonesTests
{
    private static readonly TimeZoneInfo Warsaw = TimeZones.Find("Europe/Warsaw");

    private static DateTime Utc(int year, int month, int day, int hour, int minute = 0) =>
        new(year, month, day, hour, minute, 0, DateTimeKind.Utc);

    [TestCase("Europe/Warsaw", true)]
    [TestCase("America/New_York", true)]
    [TestCase("UTC", true)]
    [TestCase("Central European Standard Time", false)]
    [TestCase("Mars/Olympus", false)]
    [TestCase("", false)]
    [TestCase(null, false)]
    public void IsValid_AcceptsOnlyIanaNames(string? id, bool valid)
    {
        Assert.That(TimeZones.IsValid(id), Is.EqualTo(valid));
    }

    [Test]
    public void Find_WithoutAZone_UsesTheDefault()
    {
        Assert.That(TimeZones.Find(null).Id, Is.EqualTo(TimeZones.Default));
    }

    [Test]
    public void ToLocal_FollowsSummerAndWinterTime()
    {
        Assert.That(TimeZones.ToLocal(Utc(2026, 7, 1, 8), Warsaw), Is.EqualTo(new DateTime(2026, 7, 1, 10, 0, 0)));
        Assert.That(TimeZones.ToLocal(Utc(2026, 12, 1, 8), Warsaw), Is.EqualTo(new DateTime(2026, 12, 1, 9, 0, 0)));
    }

    [Test]
    public void DaysToUtc_CoversWholeLocalDaysAcrossTheClockChange()
    {
        // October 2026 in Warsaw: starts in summer time (UTC+2), the clocks go back on the 25th, ends in winter time (UTC+1)
        var (from, to) = TimeZones.DaysToUtc(new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 31), Warsaw);

        Assert.That(from, Is.EqualTo(Utc(2026, 9, 30, 22)));
        Assert.That(to, Is.EqualTo(Utc(2026, 10, 31, 23)));
        Assert.That(from.Kind, Is.EqualTo(DateTimeKind.Utc));
    }

    [Test]
    public void DaysToUtc_ForOneDay_IsThatLocalDay()
    {
        var (from, to) = TimeZones.DaysToUtc(new DateOnly(2026, 3, 29), new DateOnly(2026, 3, 29), Warsaw);

        // The day the clocks go forward has only 23 hours
        Assert.That(from, Is.EqualTo(Utc(2026, 3, 28, 23)));
        Assert.That(to - from, Is.EqualTo(TimeSpan.FromHours(23)));
    }

    [Test]
    public void StartOfDayUtc_WhenTheClocksSkipMidnight_StartsAtTheFirstRealTime()
    {
        // Some zones change at midnight: on 2026-09-06 this one jumps from 00:00 to 01:00
        var zone = ZoneChangingAtMidnight();

        var start = TimeZones.StartOfDayUtc(new DateOnly(2026, 9, 6), zone);

        // 01:00 local in summer time (UTC-3) is 04:00 UTC
        Assert.That(start, Is.EqualTo(Utc(2026, 9, 6, 4)));
    }

    [Test]
    public void StartOfDayUtc_WhenMidnightHappensTwice_StartsAtTheFirst()
    {
        // On 2026-04-05 this zone goes back from 01:00 to 00:00, so the 5th starts twice
        var zone = ZoneChangingAtMidnight();

        var start = TimeZones.StartOfDayUtc(new DateOnly(2026, 4, 5), zone);

        // The first midnight is still summer time (UTC-3): 03:00 UTC
        Assert.That(start, Is.EqualTo(Utc(2026, 4, 5, 3)));
    }

    // A zone at UTC-4 with summer time (UTC-3) from the first Sunday of September to the first Sunday of April,
    // starting at midnight (like Chile) and ending at 01:00
    private static TimeZoneInfo ZoneChangingAtMidnight()
    {
        var rule = TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(
            DateTime.MinValue.Date, DateTime.MaxValue.Date, TimeSpan.FromHours(1),
            TimeZoneInfo.TransitionTime.CreateFloatingDateRule(new DateTime(1, 1, 1, 0, 0, 0), 9, 1, DayOfWeek.Sunday),
            TimeZoneInfo.TransitionTime.CreateFloatingDateRule(new DateTime(1, 1, 1, 1, 0, 0), 4, 1, DayOfWeek.Sunday));

        return TimeZoneInfo.CreateCustomTimeZone("Test/Midnight", TimeSpan.FromHours(-4), "Test", "Test", "Test Summer", [rule]);
    }
}

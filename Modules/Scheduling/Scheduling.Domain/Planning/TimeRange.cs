namespace Scheduling.Domain.Planning;

/// <summary>
/// A half-open stretch of time [Start, End) in UTC. Two ranges that only touch don't overlap, so one visit can end at
/// 10:00 and the next start at 10:00.
/// </summary>
public readonly record struct TimeRange
{
    public TimeRange(DateTime start, DateTime end)
    {
        if (end < start) throw new ArgumentException("A range can't end before it starts.", nameof(end));

        Start = DateTime.SpecifyKind(start, DateTimeKind.Utc);
        End = DateTime.SpecifyKind(end, DateTimeKind.Utc);
    }

    public DateTime Start { get; }

    public DateTime End { get; }

    public TimeSpan Length => End - Start;

    public bool IsEmpty => End <= Start;

    public bool Overlaps(TimeRange other) => Start < other.End && other.Start < End;

    public bool Contains(TimeRange other) => Start <= other.Start && other.End <= End;

    public TimeRange Widen(TimeSpan by) => new(Start - by, End + by);

    /// <summary>
    /// Sorts the ranges and joins those that overlap or touch, so the result has no two ranges sharing a moment.
    /// </summary>
    public static List<TimeRange> Merge(IEnumerable<TimeRange> ranges)
    {
        var merged = new List<TimeRange>();
        foreach (var range in ranges.Where(r => !r.IsEmpty).OrderBy(r => r.Start))
        {
            if (merged.Count > 0 && range.Start <= merged[^1].End)
            {
                var last = merged[^1];
                merged[^1] = new TimeRange(last.Start, range.End > last.End ? range.End : last.End);
            }
            else
            {
                merged.Add(range);
            }
        }

        return merged;
    }

    /// <summary>
    /// What is left of <paramref name="from"/> after taking out <paramref name="remove"/>, in order. Both lists are
    /// swept once after sorting, so it is O((n + m) log(n + m)).
    /// </summary>
    public static List<TimeRange> Subtract(IEnumerable<TimeRange> from, IEnumerable<TimeRange> remove)
    {
        var holes = Merge(remove);
        var result = new List<TimeRange>();

        foreach (var range in Merge(from))
        {
            var cursor = range.Start;
            foreach (var hole in holes)
            {
                if (hole.End <= cursor) continue;
                if (hole.Start >= range.End) break;

                if (hole.Start > cursor) result.Add(new TimeRange(cursor, hole.Start));
                if (hole.End > cursor) cursor = hole.End;
                if (cursor >= range.End) break;
            }

            if (cursor < range.End) result.Add(new TimeRange(cursor, range.End));
        }

        return result;
    }
}

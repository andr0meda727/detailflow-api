namespace Scheduling.Domain.Bookings;

/// <summary>
/// Lookup row with the display name and colour of a status. The rows are fixed; behaviour keys off the IDs.
/// </summary>
public class BookingStatus
{
    // For EF Core
    private BookingStatus() { }

    public BookingStatus(Guid id, string name, string? color, int sortOrder)
    {
        Id = id;
        Name = name;
        Color = color;
        SortOrder = sortOrder;
    }

    public static readonly Guid Created = Guid.Parse("6aeadcad-a836-46fc-a445-3a3a832f49a4");
    public static readonly Guid Completed = Guid.Parse("6f4f4496-dd44-4ad8-8021-37ef6b3279dd");
    public static readonly Guid Cancelled = Guid.Parse("d89cf9d3-ee24-481a-ac76-c5cb224f1c1a");

    public static bool IsKnown(Guid id) => id == Created || id == Completed || id == Cancelled;

    public Guid Id { get; private set; }

    public string Name { get; private set; } = null!;

    public string? Color { get; private set; }

    public int SortOrder { get; private set; }
}

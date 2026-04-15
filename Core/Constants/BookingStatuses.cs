namespace Core.Constants;

public enum BookingStatuses
{
    Created,
    Completed,
    Cancelled
}

public static class BookingStatusIds
{
    public static readonly Guid Created = Guid.Parse("6aeadcad-a836-46fc-a445-3a3a832f49a4");
    public static readonly Guid Completed = Guid.Parse("6f4f4496-dd44-4ad8-8021-37ef6b3279dd");
    public static readonly Guid Cancelled = Guid.Parse("d89cf9d3-ee24-481a-ac76-c5cb224f1c1a");
}
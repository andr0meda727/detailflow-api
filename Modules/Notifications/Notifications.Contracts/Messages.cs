namespace Notifications.Contracts;

/// <summary>
/// Asks for a PDF of a studio's bookings on the local days <paramref name="DateFrom"/> to <paramref name="DateTo"/>
/// (both included) in the studio's <paramref name="TimeZone"/>, emailed to <paramref name="RequestedByEmail"/>.
/// Rendering and sending take a while, so it runs on the bus instead of in the request.
/// </summary>
public sealed record GenerateBookingsReport(
    Guid StudioId, string RequestedByEmail, DateOnly DateFrom, DateOnly DateTo, string TimeZone);

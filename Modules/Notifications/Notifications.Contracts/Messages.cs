namespace Notifications.Contracts;

/// <summary>
/// Asks for a PDF of a studio's bookings in a period, emailed to <paramref name="RequestedByEmail"/>.
/// Rendering and sending take a while, so it runs on the bus instead of in the request.
/// </summary>
public sealed record GenerateBookingsReport(Guid StudioId, string RequestedByEmail, DateTime DateFrom, DateTime DateTo);

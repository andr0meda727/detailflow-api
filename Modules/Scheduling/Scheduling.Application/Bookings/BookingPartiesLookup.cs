using Crm.Contracts;
using Scheduling.Domain.Bookings;
using Shared.Kernel.Vehicles;

namespace Scheduling.Application.Bookings;

internal sealed record FoundParties(BookingClient Client, BookingVehicle Vehicle);

internal static class BookingPartiesLookup
{
    // Translates the CRM contract into the booking's own value types
    public static async Task<FoundParties?> FindAsync(
        this IClientDirectory directory, BookingRequest request, CancellationToken cancellationToken)
    {
        var parties = await directory.FindBookingPartiesAsync(request.ClientId, request.VehicleId, cancellationToken);
        if (parties == null) return null;

        return new FoundParties(
            new BookingClient(parties.Client.Id, parties.Client.Name, parties.Client.Phone, parties.Client.Email),
            new BookingVehicle(
                parties.Vehicle.Id, parties.Vehicle.LicensePlate, parties.Vehicle.Brand, parties.Vehicle.Model,
                VehicleSizes.Parse(parties.Vehicle.Size)));
    }
}

using Shared.Kernel.Results;

namespace Crm.Application;

public static class CrmErrors
{
    public static Error ClientNotFound(Guid id) =>
        Error.NotFound("CLIENT_NOT_FOUND", $"Client {id} not found");

    public static Error VehicleNotFound(Guid id) =>
        Error.NotFound("VEHICLE_NOT_FOUND", $"Vehicle {id} not found");

    // A client referenced from a request body: a validation error rather than a missing resource
    public static Error UnknownClient(Guid id) =>
        Error.Validation("CLIENT_NOT_FOUND", $"Client {id} not found");

    public static Error PhoneTaken(string phone) =>
        Error.Conflict("CLIENT_PHONE_TAKEN", $"Another client of this studio already has phone number {phone}");
}

using Microsoft.EntityFrameworkCore;
using Scheduling.Application.Abstractions;
using Scheduling.Contracts;
using Scheduling.Domain.Requests;
using Shared.Kernel.Results;

namespace Scheduling.Application.Requests.Reject;

public sealed class RejectBookingRequestHandler(ISchedulingDbContext db, TimeProvider clock)
{
    public async Task<Result<BookingRequestDto>> HandleAsync(
        Guid id, RejectBookingRequest decision, CancellationToken cancellationToken)
    {
        // Tenant-filtered: another studio's request is not found
        var request = await db.BookingRequests.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (request == null) return RequestErrors.NotFound;

        var rejected = request.Reject(decision.Reason, clock.GetUtcNow().UtcDateTime);
        if (!rejected.Success) return rejected.Error!;

        db.Enqueue(new BookingRequestDecided(
            request.StudioId, request.Id, request.StudioName, request.CustomerName, request.CustomerEmail,
            Accepted: false, ScheduledAt: null, request.RejectionReason));
        await db.SaveChangesAsync(cancellationToken);

        return BookingRequestDto.From(request);
    }
}

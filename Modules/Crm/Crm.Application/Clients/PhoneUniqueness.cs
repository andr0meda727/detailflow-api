using Crm.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Crm.Application.Clients;

internal static class PhoneUniqueness
{
    // Checked up front so a duplicate is a 409 with a code, not a unique-index violation.
    // Deleted clients are filtered out, matching the partial unique index.
    public static Task<bool> IsTakenAsync(
        this ICrmDbContext db, string phone, Guid? exceptClientId, CancellationToken cancellationToken)
    {
        var normalized = phone.Trim();
        return db.Clients.AnyAsync(c => c.Phone == normalized && c.Id != exceptClientId, cancellationToken);
    }
}

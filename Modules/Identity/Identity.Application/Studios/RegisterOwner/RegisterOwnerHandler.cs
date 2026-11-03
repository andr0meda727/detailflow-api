using Identity.Application.Abstractions;
using Identity.Domain.Studios;
using Shared.Kernel.Results;

namespace Identity.Application.Studios.RegisterOwner;

public sealed class RegisterOwnerHandler(IIdentityDbContext db, IAuthProvider authProvider)
{
    public async Task<Result> HandleAsync(RegisterOwnerRequest request, CancellationToken cancellationToken)
    {
        if (await authProvider.UserExistsAsync(request.Email, cancellationToken))
        {
            return Error.Conflict("EMAIL_TAKEN", "Konto z tym adresem e-mail już istnieje.");
        }

        var userId = await authProvider.SignUpAsync(
            request.Email, request.Password, request.FirstName, request.LastName, cancellationToken);

        // The studio and its owner membership are saved in one transaction
        var studio = Studio.Register(
            request.Studio.Name, request.Studio.Phone, request.Studio.Email, request.Studio.Address, userId);
        db.Studios.Add(studio);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            // Don't leave a login behind that has no studio and can't register again
            await authProvider.DeleteUserAsync(userId, CancellationToken.None);
            throw;
        }

        return Result.Ok();
    }
}

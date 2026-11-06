using Shared.Kernel.Auditing;

namespace Customers.Domain;

/// <summary>
/// Data that belongs to one customer account (a Supabase user). The module's query filter shows only the
/// signed-in user's rows, and none without a user.
/// </summary>
public interface ICustomerOwned
{
    Guid UserId { get; }
}

/// <summary>
/// How a customer introduces themselves to studios: the name and phone that go on their booking requests.
/// One per account, keyed by the Supabase user ID; the email is the account's own.
/// </summary>
public class CustomerProfile : IAuditableEntity, ICustomerOwned
{
    // For EF Core
    private CustomerProfile() { }

    public Guid UserId { get; private set; }

    public string Name { get; private set; } = null!;

    public string Phone { get; private set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public static CustomerProfile Create(Guid userId, string name, string phone)
    {
        if (userId == Guid.Empty) throw new ArgumentException("A profile belongs to an account.", nameof(userId));

        var profile = new CustomerProfile { UserId = userId };
        profile.Update(name, phone);
        return profile;
    }

    public void Update(string name, string phone)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(phone);

        Name = name.Trim();
        Phone = phone.Trim();
    }
}

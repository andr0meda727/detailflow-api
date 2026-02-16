using Core.Interfaces;
using Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ITenantContext tenantContext) : DbContext(options)
{
    public DbSet<Booking> Bookings { get; set; }
    public DbSet<BookingService> BookingServices { get; set; }
    public DbSet<BookingStatus> BookingStatuses { get; set; }
    public DbSet<Client> Clients { get; set; }
    public DbSet<Inventory> Inventory { get; set; }
    public DbSet<InventoryCategory> InventoryCategories { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<Service> Services { get; set; }
    public DbSet<StripeEvent> StripeEvents { get; set; }
    public DbSet<Studio> Studios { get; set; }
    public DbSet<StudioSubscription> StudioSubscriptions { get; set; }
    public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Vehicle> Vehicles { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("d");

        modelBuilder.Entity<Booking>()
            .HasQueryFilter(b => b.StudioId == tenantContext.StudioId);

        // Table names MUST match actual database tables
        modelBuilder.Entity<Booking>().ToTable("booking");
        modelBuilder.Entity<BookingService>().ToTable("booking_service");
        modelBuilder.Entity<BookingStatus>().ToTable("booking_status");
        modelBuilder.Entity<Client>().ToTable("client");
        modelBuilder.Entity<Inventory>().ToTable("inventory");
        modelBuilder.Entity<InventoryCategory>().ToTable("inventory_category");
        modelBuilder.Entity<Payment>().ToTable("payment");
        modelBuilder.Entity<Service>().ToTable("service");
        modelBuilder.Entity<StripeEvent>().ToTable("stripe_event");
        modelBuilder.Entity<Studio>().ToTable("studio");
        modelBuilder.Entity<StudioSubscription>().ToTable("studio_subscription");
        modelBuilder.Entity<SubscriptionPlan>().ToTable("subscription_plan");
        modelBuilder.Entity<User>().ToTable(@"""user"""); // reserved keyword in Postgres
        modelBuilder.Entity<Vehicle>().ToTable("vehicle");

        // Primary keys (explicit)
        modelBuilder.Entity<Booking>().HasKey(x => x.Id);
        modelBuilder.Entity<BookingService>().HasKey(x => x.Id);
        modelBuilder.Entity<BookingStatus>().HasKey(x => x.Id);
        modelBuilder.Entity<Client>().HasKey(x => x.Id);
        modelBuilder.Entity<Inventory>().HasKey(x => x.Id);
        modelBuilder.Entity<InventoryCategory>().HasKey(x => x.Id);
        modelBuilder.Entity<Payment>().HasKey(x => x.Id);
        modelBuilder.Entity<Service>().HasKey(x => x.Id);
        modelBuilder.Entity<StripeEvent>().HasKey(x => x.Id);
        modelBuilder.Entity<Studio>().HasKey(x => x.Id);
        modelBuilder.Entity<StudioSubscription>().HasKey(x => x.Id);
        modelBuilder.Entity<SubscriptionPlan>().HasKey(x => x.Id);
        modelBuilder.Entity<User>().HasKey(x => x.Id);
        modelBuilder.Entity<Vehicle>().HasKey(x => x.Id);

        // Column names
        modelBuilder.Entity<Booking>(entity =>
        {
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.StudioId).HasColumnName("studio_id");
            entity.Property(e => e.VehicleId).HasColumnName("vehicle_id");
            entity.Property(e => e.ClientId).HasColumnName("client_id");
            entity.Property(e => e.ScheduledAt).HasColumnName("scheduled_at");
            entity.Property(e => e.CompletedAt).HasColumnName("completed_at");
            entity.Property(e => e.StatusId).HasColumnName("status_id");
            entity.Property(e => e.TotalPrice).HasColumnName("total_price");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        });

        modelBuilder.Entity<BookingService>(entity =>
        {
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BookingId).HasColumnName("booking_id");
            entity.Property(e => e.ServiceId).HasColumnName("service_id");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
        });

        modelBuilder.Entity<BookingStatus>(entity =>
        {
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Color).HasColumnName("color");
            entity.Property(e => e.SortOrder).HasColumnName("sort_order");
        });

        modelBuilder.Entity<Client>(entity =>
        {
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.StudioId).HasColumnName("studio_id");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Email).HasColumnName("email");
            entity.Property(e => e.Phone).HasColumnName("phone");
            entity.Property(e => e.Nip).HasColumnName("nip");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        });

        modelBuilder.Entity<Inventory>(entity =>
        {
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.StudioId).HasColumnName("studio_id");
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Unit).HasColumnName("unit");
            entity.Property(e => e.Stock).HasColumnName("stock");
            entity.Property(e => e.MinStock).HasColumnName("min_stock");
            entity.Property(e => e.Capacity).HasColumnName("capacity");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        });

        modelBuilder.Entity<InventoryCategory>(entity =>
        {
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.StudioId).HasColumnName("studio_id");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.StudioId).HasColumnName("studio_id");
            entity.Property(e => e.SubscriptionId).HasColumnName("subscription_id");
            entity.Property(e => e.StripePaymentIntentId).HasColumnName("stripe_payment_intent_id");
            entity.Property(e => e.StripeInvoiceId).HasColumnName("stripe_invoice_id");
            entity.Property(e => e.StripeChargeId).HasColumnName("stripe_charge_id");
            entity.Property(e => e.Amount).HasColumnName("amount");
            entity.Property(e => e.Currency).HasColumnName("currency");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.PaymentMethodType).HasColumnName("payment_method_type");
            entity.Property(e => e.FailureCode).HasColumnName("failure_code");
            entity.Property(e => e.FailureMessage).HasColumnName("failure_message");
            entity.Property(e => e.PaidAt).HasColumnName("paid_at");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
        });

        modelBuilder.Entity<Service>(entity =>
        {
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.StudioId).HasColumnName("studio_id");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        });

        modelBuilder.Entity<StripeEvent>(entity =>
        {
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.StripeEventId).HasColumnName("stripe_event_id");
            entity.Property(e => e.EventType).HasColumnName("event_type");
            entity.Property(e => e.StudioId).HasColumnName("studio_id");
            entity.Property(e => e.SubscriptionId).HasColumnName("subscription_id");
            entity.Property(e => e.Payload).HasColumnName("payload");
            entity.Property(e => e.Processed).HasColumnName("processed");
            entity.Property(e => e.ProcessedAt).HasColumnName("processed_at");
            entity.Property(e => e.ErrorMessage).HasColumnName("error_message");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");

            entity.Property(e => e.Payload).HasColumnType("jsonb");
        });

        modelBuilder.Entity<Studio>(entity =>
        {
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Phone).HasColumnName("phone");
            entity.Property(e => e.Email).HasColumnName("email");
            entity.Property(e => e.StripeCustomerId).HasColumnName("stripe_customer_id");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        });

        modelBuilder.Entity<StudioSubscription>(entity =>
        {
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.StudioId).HasColumnName("studio_id");
            entity.Property(e => e.PlanId).HasColumnName("plan_id");
            entity.Property(e => e.StripeSubscriptionId).HasColumnName("stripe_subscription_id");
            entity.Property(e => e.StripeStatus).HasColumnName("stripe_status");
            entity.Property(e => e.CurrentPeriodStart).HasColumnName("current_period_start");
            entity.Property(e => e.CurrentPeriodEnd).HasColumnName("current_period_end");
            entity.Property(e => e.CancelAtPeriodEnd).HasColumnName("cancel_at_period_end");
            entity.Property(e => e.CanceledAt).HasColumnName("canceled_at");
            entity.Property(e => e.EndedAt).HasColumnName("ended_at");
            entity.Property(e => e.TrialStart).HasColumnName("trial_start");
            entity.Property(e => e.TrialEnd).HasColumnName("trial_end");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        });

        modelBuilder.Entity<SubscriptionPlan>(entity =>
        {
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.DisplayName).HasColumnName("display_name");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.StripeProductId).HasColumnName("stripe_product_id");
            entity.Property(e => e.StripePriceId).HasColumnName("stripe_price_id");
            entity.Property(e => e.Amount).HasColumnName("amount");
            entity.Property(e => e.Currency).HasColumnName("currency");
            entity.Property(e => e.Interval).HasColumnName("interval");
            entity.Property(e => e.IntervalCount).HasColumnName("interval_count");
            entity.Property(e => e.MaxUsers).HasColumnName("max_users");
            entity.Property(e => e.MaxVehicles).HasColumnName("max_vehicles");
            entity.Property(e => e.MaxBookingsPerMonth).HasColumnName("max_bookings_per_month");
            entity.Property(e => e.Features).HasColumnName("features");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.Property(e => e.Features).HasColumnType("jsonb");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.StudioId).HasColumnName("studio_id");
            entity.Property(e => e.Email).HasColumnName("email");

            // Map hash to the existing DB column "password"
            entity.Property(e => e.PasswordHash).HasColumnName("password");

            entity.Property(e => e.FirstName).HasColumnName("first_name");
            entity.Property(e => e.LastName).HasColumnName("last_name");
            entity.Property(e => e.Role).HasColumnName("role");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.LastLoginAt).HasColumnName("last_login_at");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.Ignore(e => e.FullName);
        });

        modelBuilder.Entity<Vehicle>(entity =>
        {
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ClientId).HasColumnName("client_id");
            entity.Property(e => e.StudioId).HasColumnName("studio_id");
            entity.Property(e => e.Brand).HasColumnName("brand");
            entity.Property(e => e.Model).HasColumnName("model");
            entity.Property(e => e.LicensePlate).HasColumnName("license_plate");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        });

        // Relationships + Foreign Keys

        // Studio -> Users
        modelBuilder.Entity<User>()
            .HasOne(x => x.Studio)
            .WithMany(x => x.Users)
            .HasForeignKey(x => x.StudioId)
            .OnDelete(DeleteBehavior.Restrict);

        // Studio -> Clients
        modelBuilder.Entity<Client>()
            .HasOne(x => x.Studio)
            .WithMany(x => x.Clients)
            .HasForeignKey(x => x.StudioId)
            .OnDelete(DeleteBehavior.Restrict);

        // Studio -> Vehicles
        modelBuilder.Entity<Vehicle>()
            .HasOne(x => x.Studio)
            .WithMany(x => x.Vehicles)
            .HasForeignKey(x => x.StudioId)
            .OnDelete(DeleteBehavior.Restrict);

        // Vehicle -> Client
        modelBuilder.Entity<Vehicle>()
            .HasOne(x => x.Client)
            .WithMany(x => x.Vehicles)
            .HasForeignKey(x => x.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        // Studio -> Bookings
        modelBuilder.Entity<Booking>()
            .HasOne(x => x.Studio)
            .WithMany(x => x.Bookings)
            .HasForeignKey(x => x.StudioId)
            .OnDelete(DeleteBehavior.Restrict);

        // Booking -> Client
        modelBuilder.Entity<Booking>()
            .HasOne(x => x.Client)
            .WithMany(x => x.Bookings)
            .HasForeignKey(x => x.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        // Booking -> Vehicle
        modelBuilder.Entity<Booking>()
            .HasOne(x => x.Vehicle)
            .WithMany(x => x.Bookings)
            .HasForeignKey(x => x.VehicleId)
            .OnDelete(DeleteBehavior.Restrict);

        // Booking -> Status
        modelBuilder.Entity<Booking>()
            .HasOne(x => x.Status)
            .WithMany(x => x.Bookings)
            .HasForeignKey(x => x.StatusId)
            .OnDelete(DeleteBehavior.Restrict);

        // Booking -> CreatedBy (User)
        modelBuilder.Entity<Booking>()
            .HasOne(x => x.CreatedByUser)
            .WithMany(x => x.CreatedBookings)
            .HasForeignKey(x => x.CreatedBy)
            .OnDelete(DeleteBehavior.SetNull);

        // BookingService (join) -> Booking
        modelBuilder.Entity<BookingService>()
            .HasOne(x => x.Booking)
            .WithMany(x => x.BookingServices)
            .HasForeignKey(x => x.BookingId)
            .OnDelete(DeleteBehavior.Cascade);

        // BookingService (join) -> Service
        modelBuilder.Entity<BookingService>()
            .HasOne(x => x.Service)
            .WithMany(x => x.BookingServices)
            .HasForeignKey(x => x.ServiceId)
            .OnDelete(DeleteBehavior.Restrict);

        // Studio -> Service
        modelBuilder.Entity<Service>()
            .HasOne(x => x.Studio)
            .WithMany(x => x.Services)
            .HasForeignKey(x => x.StudioId)
            .OnDelete(DeleteBehavior.Restrict);

        // Studio -> InventoryCategory
        modelBuilder.Entity<InventoryCategory>()
            .HasOne(x => x.Studio)
            .WithMany(x => x.InventoryCategories)
            .HasForeignKey(x => x.StudioId)
            .OnDelete(DeleteBehavior.Restrict);

        // Studio -> Inventory
        modelBuilder.Entity<Inventory>()
            .HasOne(x => x.Studio)
            .WithMany(x => x.InventoryItems)
            .HasForeignKey(x => x.StudioId)
            .OnDelete(DeleteBehavior.Restrict);

        // Inventory -> Category
        modelBuilder.Entity<Inventory>()
            .HasOne(x => x.Category)
            .WithMany(x => x.InventoryItems)
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);

        // StudioSubscription -> Studio
        modelBuilder.Entity<StudioSubscription>()
            .HasOne(x => x.Studio)
            .WithMany(x => x.StudioSubscriptions)
            .HasForeignKey(x => x.StudioId)
            .OnDelete(DeleteBehavior.Restrict);

        // StudioSubscription -> Plan
        modelBuilder.Entity<StudioSubscription>()
            .HasOne(x => x.Plan)
            .WithMany(x => x.StudioSubscriptions)
            .HasForeignKey(x => x.PlanId)
            .OnDelete(DeleteBehavior.Restrict);

        // Payment -> Studio
        modelBuilder.Entity<Payment>()
            .HasOne(x => x.Studio)
            .WithMany(x => x.Payments)
            .HasForeignKey(x => x.StudioId)
            .OnDelete(DeleteBehavior.Restrict);

        // Payment -> StudioSubscription
        modelBuilder.Entity<Payment>()
            .HasOne(x => x.Subscription)
            .WithMany(x => x.Payments)
            .HasForeignKey(x => x.SubscriptionId)
            .OnDelete(DeleteBehavior.SetNull);

        // StripeEvent -> Studio
        modelBuilder.Entity<StripeEvent>()
            .HasOne(x => x.Studio)
            .WithMany(x => x.StripeEvents)
            .HasForeignKey(x => x.StudioId)
            .OnDelete(DeleteBehavior.SetNull);

        // StripeEvent -> StudioSubscription
        modelBuilder.Entity<StripeEvent>()
            .HasOne(x => x.Subscription)
            .WithMany(x => x.StripeEvents)
            .HasForeignKey(x => x.SubscriptionId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
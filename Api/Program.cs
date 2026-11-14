using System.Text.Json.Serialization;
using Api.Extensions;
using Api.Middleware;
using Crm.Api;
using Customers.Api;
using Identity.Api;
using Identity.Api.Tenancy;
using Infrastructure.Extensions;
using Inventory.Api;
using Marketplace.Api;
using Notifications.Api;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using Scheduling.Api;
using Shared.Infrastructure.Messaging;
using Shared.Kernel.Time;

var builder = WebApplication.CreateBuilder(args);

// Enums travel as their names ("Interior"), not numbers, so the API stays readable and reordering them is safe
builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Add Infrastructure (Database, Repositories)
builder.Services.AddInfrastructure(builder.Configuration);

// Add Application Services
builder.Services.AddApplicationServices();

// Modules
builder.Services.AddIdentityModule(builder.Configuration);
builder.Services.AddInventoryModule(builder.Configuration);
builder.Services.AddCrmModule(builder.Configuration);
builder.Services.AddSchedulingModule(builder.Configuration);
builder.Services.AddNotificationsModule();
builder.Services.AddMarketplaceModule(builder.Configuration);
builder.Services.AddCustomersModule(builder.Configuration);

// One bus for the host; every assembly with consumers is listed here
builder.Services.AddMessaging(builder.Configuration,
    IdentityModule.ConsumersAssembly, SchedulingModule.ConsumersAssembly, InventoryModule.ConsumersAssembly, NotificationsModule.ConsumersAssembly,
    MarketplaceModule.ConsumersAssembly);

builder.Services.AddProxySupport();
builder.Services.AddPublicRateLimiting(builder.Configuration);

builder.Services.AddOpenApi();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .WithOrigins(
                "https://app.detaileed.com",
                "http://localhost:3000"
            )
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// Fail at startup, not on the first report, if the OS has no time-zone data (Alpine images need tzdata)
TimeZones.Find(TimeZones.Default);

var app = builder.Build();

// First, so everything after it (rate limits, HTTPS redirection, logs) sees the client's real IP and scheme
app.UseForwardedHeaders();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}
else
{
    // app.UseHsts(); allow it later for prod
    // app.UseExceptionHandler();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseCors("Frontend");

app.UseRateLimiter();

// After the rate limiter, so cached answers still count; before authentication, so a hit skips the profile lookup
app.UseOutputCache();

app.UseAuthentication();

// Needs the routed endpoint (for [AllowWithoutStudio]) and the authenticated user
app.UseTenantContext();

app.UseAuthorization();

app.MapControllers();

app.Run();
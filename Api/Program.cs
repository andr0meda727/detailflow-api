using Api.Extensions;
using Api.Middleware;
using Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddControllers();

// Add Infrastructure (Database, Repositories)
builder.Services.AddInfrastructure(builder.Configuration);

// Add Application Services
builder.Services.AddApplicationServices();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Use Tenant Context Middleware (BEFORE routing and endpoints)
app.UseTenantContext();

app.MapControllers();

app.Run();
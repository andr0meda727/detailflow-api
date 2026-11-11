using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Scheduling.Api.Controllers;
using Scheduling.Application.Bookings;
using Scheduling.Application.Bookings.CancelBooking;
using Scheduling.Application.Bookings.CompleteBooking;
using Scheduling.Application.Bookings.CreateBooking;
using Scheduling.Application.Bookings.DeleteBooking;
using Scheduling.Application.Bookings.GetBooking;
using Scheduling.Application.Bookings.GetBookings;
using Scheduling.Application.Bookings.UpdateBooking;
using Scheduling.Application.Catalog.CreateService;
using Scheduling.Application.Catalog.DeleteService;
using Scheduling.Application.Catalog.GetServices;
using Scheduling.Application.Catalog.UpdateService;
using Scheduling.Application.Planning;
using Scheduling.Application.Planning.Availability;
using Scheduling.Application.Planning.DayBoard;
using Scheduling.Application.Planning.Bays.CreateBay;
using Scheduling.Application.Planning.Bays.DeactivateBay;
using Scheduling.Application.Planning.Bays.GetBays;
using Scheduling.Application.Planning.Bays.UpdateBay;
using Scheduling.Application.Planning.Schedule;
using Scheduling.Application.Requests.Accept;
using Scheduling.Application.Requests.Estimate;
using Scheduling.Application.Requests.GetMyRequests;
using Scheduling.Application.Requests.GetStudioRequests;
using Scheduling.Application.Requests.Reject;
using Scheduling.Application.Requests.Submit;
using Scheduling.Application.Requests.Withdraw;
using Scheduling.Infrastructure;
using Scheduling.Infrastructure.Consumers;

namespace Scheduling.Api;

/// <summary>
/// Composition root of the Scheduling module: the host calls <see cref="AddSchedulingModule"/>
/// and passes <see cref="ConsumersAssembly"/> to the message bus.
/// </summary>
public static class SchedulingModule
{
    public static Assembly ConsumersAssembly => typeof(BookingCopiesConsumer).Assembly;

    public static IServiceCollection AddSchedulingModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<GetBookingsHandler>();
        services.AddScoped<GetBookingHandler>();
        services.AddScoped<CreateBookingHandler>();
        services.AddScoped<UpdateBookingHandler>();
        services.AddScoped<DeleteBookingHandler>();
        services.AddScoped<CompleteBookingHandler>();
        services.AddScoped<CancelBookingHandler>();

        services.AddScoped<GetServicesHandler>();
        services.AddScoped<CreateServiceHandler>();
        services.AddScoped<UpdateServiceHandler>();
        services.AddScoped<DeleteServiceHandler>();

        services.AddScoped<BookingPlanner>();
        services.AddScoped<GetStaffAvailabilityHandler>();
        services.AddScoped<GetPublicAvailabilityHandler>();
        services.AddScoped<GetDayBoardHandler>();

        services.AddScoped<GetBaysHandler>();
        services.AddScoped<CreateBayHandler>();
        services.AddScoped<UpdateBayHandler>();
        services.AddScoped<DeactivateBayHandler>();

        services.AddScoped<GetScheduleHandler>();
        services.AddScoped<UpdateScheduleHandler>();
        services.AddScoped<CloseDayHandler>();
        services.AddScoped<ReopenDayHandler>();

        services.AddScoped<EstimateHandler>();
        services.AddScoped<SubmitBookingRequestHandler>();
        services.AddScoped<GetMyBookingRequestsHandler>();
        services.AddScoped<WithdrawBookingRequestHandler>();
        services.AddScoped<GetStudioBookingRequestsHandler>();
        services.AddScoped<AcceptBookingRequestHandler>();
        services.AddScoped<RejectBookingRequestHandler>();

        services.TryAddSingleton(TimeProvider.System);

        services.AddValidatorsFromAssemblyContaining<BookingRequestValidator>();

        services.AddSchedulingInfrastructure(configuration);

        services.AddControllers().AddApplicationPart(typeof(BookingsController).Assembly);

        return services;
    }
}

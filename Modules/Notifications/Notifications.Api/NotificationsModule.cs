using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Notifications.Api.Controllers;
using Notifications.Infrastructure.Consumers;
using Notifications.Infrastructure.Email;
using Notifications.Infrastructure.Reports;

namespace Notifications.Api;

/// <summary>
/// Composition root of the Notifications module: emails and the bookings PDF report. It keeps no data of its own;
/// it reacts to messages (EmployeeInvited, GenerateBookingsReport) and reads bookings through Scheduling.Contracts.
/// The host calls <see cref="AddNotificationsModule"/> and passes <see cref="ConsumersAssembly"/> to the message bus.
/// </summary>
public static class NotificationsModule
{
    public static Assembly ConsumersAssembly => typeof(BookingsReportConsumer).Assembly;

    public static IServiceCollection AddNotificationsModule(this IServiceCollection services)
    {
        services.AddScoped<IEmailSender, SmtpEmailSender>();
        services.AddSingleton<BookingsReportPdf>();
        services.TryAddSingleton(TimeProvider.System);

        services.AddValidatorsFromAssemblyContaining<BookingsReportRequestValidator>();

        services.AddControllers().AddApplicationPart(typeof(ReportsController).Assembly);

        return services;
    }
}

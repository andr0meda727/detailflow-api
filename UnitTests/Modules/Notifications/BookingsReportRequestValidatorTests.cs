using FluentValidation.TestHelper;
using Notifications.Api.Controllers;

namespace Unit.Modules.Notifications;

[TestFixture]
public class BookingsReportRequestValidatorTests
{
    private static readonly DateTime From = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private readonly BookingsReportRequestValidator _validator = new();

    [Test]
    public void Passes_ForASingleDay()
    {
        _validator.TestValidate(new BookingsReportRequest(From, From)).ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void Passes_ForAFullLeapYear()
    {
        _validator.TestValidate(new BookingsReportRequest(From, From.AddDays(366))).ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void Rejects_MoreThanAYear()
    {
        _validator.TestValidate(new BookingsReportRequest(From, From.AddDays(366).AddSeconds(1)))
            .ShouldHaveValidationErrorFor(x => x.DateTo);
    }

    [Test]
    public void Rejects_AnEndBeforeTheStart()
    {
        _validator.TestValidate(new BookingsReportRequest(From, From.AddDays(-1)))
            .ShouldHaveValidationErrorFor(x => x.DateTo);
    }

    [Test]
    public void Rejects_MissingDates()
    {
        var result = _validator.TestValidate(new BookingsReportRequest(default, default));

        result.ShouldHaveValidationErrorFor(x => x.DateFrom);
        result.ShouldHaveValidationErrorFor(x => x.DateTo);
    }
}

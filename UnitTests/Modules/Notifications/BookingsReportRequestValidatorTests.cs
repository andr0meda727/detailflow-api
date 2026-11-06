using FluentValidation.TestHelper;
using Notifications.Api.Controllers;

namespace Unit.Modules.Notifications;

[TestFixture]
public class BookingsReportRequestValidatorTests
{
    private static readonly DateOnly From = new(2028, 1, 1);
    private readonly BookingsReportRequestValidator _validator = new();

    [Test]
    public void Passes_ForASingleDay()
    {
        _validator.TestValidate(new BookingsReportRequest(From, From)).ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void Passes_ForAWholeLeapYear()
    {
        _validator.TestValidate(new BookingsReportRequest(From, new DateOnly(2028, 12, 31))).ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void Rejects_MoreThanAYear()
    {
        _validator.TestValidate(new BookingsReportRequest(From, new DateOnly(2029, 1, 1)))
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

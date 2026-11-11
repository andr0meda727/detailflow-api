using Shared.Api.Errors;
using Shared.Kernel.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Unit.Api;

[TestFixture]
public class ErrorResultExtensionsTests
{
    private ControllerBase _controller;

    [SetUp]
    public void SetUp()
    {
        // ControllerBase resolves its ProblemDetailsFactory from the request services
        var services = new ServiceCollection().AddLogging();
        services.AddControllers();

        _controller = new TestController
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { RequestServices = services.BuildServiceProvider() }
            }
        };
    }

    [TestCase(ErrorType.Validation, StatusCodes.Status400BadRequest)]
    [TestCase(ErrorType.NotFound, StatusCodes.Status404NotFound)]
    [TestCase(ErrorType.Conflict, StatusCodes.Status409Conflict)]
    [TestCase(ErrorType.Forbidden, StatusCodes.Status403Forbidden)]
    public void ToProblem_MapsErrorTypeToStatusCode(ErrorType type, int expectedStatus)
    {
        var result = _controller.ToProblem(new Error("SOME_CODE", "Some message", type));

        var problem = (ProblemDetails)result.Value!;
        Assert.That(result.StatusCode, Is.EqualTo(expectedStatus));
        Assert.That(problem.Status, Is.EqualTo(expectedStatus));
    }

    [Test]
    public void ToProblem_ExposesCodeAndMessage()
    {
        var result = _controller.ToProblem(Error.Conflict("EMAIL_TAKEN", "Email already used"));

        var problem = (ProblemDetails)result.Value!;
        Assert.That(problem.Extensions["code"], Is.EqualTo("EMAIL_TAKEN"));
        Assert.That(problem.Detail, Is.EqualTo("Email already used"));
    }

    private sealed class TestController : ControllerBase;
}

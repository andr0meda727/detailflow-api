using Core.DTOs.Results;
using Microsoft.AspNetCore.Mvc;

namespace Api.Extensions;

public static class ErrorResultExtensions
{
    /// <summary>
    /// Turns a business error into an RFC 7807 ProblemDetails response.
    /// The stable error code is exposed as the "code" extension member.
    /// </summary>
    public static ObjectResult ToProblem(this ControllerBase controller, Error error)
    {
        var statusCode = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status500InternalServerError
        };

        var problem = controller.ProblemDetailsFactory.CreateProblemDetails(
            controller.HttpContext, statusCode, detail: error.Message);
        problem.Extensions["code"] = error.Code;

        return new ObjectResult(problem) { StatusCode = statusCode };
    }
}

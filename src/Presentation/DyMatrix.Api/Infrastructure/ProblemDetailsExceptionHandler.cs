// Look at: https://github.com/jasontaylordev/CleanArchitecture/blob/main/src/Web/Infrastructure/ProblemDetailsExceptionHandler.cs

using DyMatrix.Application.Common.Exceptions;
using DyMatrix.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;

namespace DyMatrix.Api.Infrastructure;

/// <summary>
/// Converts well-known application exceptions into RFC 9110-compliant <see cref="ProblemDetails"/> responses,
/// mapping <see cref="AppValidationException"/> → 400, <see cref="NotFoundException"/> → 404,
/// Unrecognised exceptions are not handled and fall through to the default middleware.
/// </summary>
public class ProblemDetailsExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (statusCode, problemDetails) = exception switch
        {
            AppValidationException ve => (StatusCodes.Status400BadRequest, new ValidationProblemDetails(ve.Errors)
            {
                Status = StatusCodes.Status400BadRequest,
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1"
            }),
            NotFoundException ne => (StatusCodes.Status404NotFound, new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.5",
                Title = "The specified resource was not found.",
                Detail = ne.Message
            }),
            RateLimitExceededException re => (StatusCodes.Status429TooManyRequests, new ProblemDetails
            {
                Status = StatusCodes.Status429TooManyRequests,
                Type = "https://tools.ietf.org/html/rfc6585#section-4",
                Title = "Too Many Requests",
                Detail = re.Message
            }),
            _ => (-1, null)
        };

        if (problemDetails is null) return false;

        if (statusCode == StatusCodes.Status429TooManyRequests)
            httpContext.Response.Headers.RetryAfter = "60";

        httpContext.Response.StatusCode = statusCode;

        // Use runtime type so ValidationProblemDetails.Errors is included in serialization
        await httpContext.Response.WriteAsJsonAsync(problemDetails, problemDetails.GetType(), cancellationToken);
        return true;
    }
}
using System.Diagnostics;
using Cis.Application.Common.Exceptions;
using Cis.Contracts;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace Cis.Api.Errors;

internal sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var problemDetails = CreateProblemDetails(httpContext, exception);

        if (problemDetails.Status >= StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception while processing request {TraceId}", httpContext.TraceIdentifier);
        }
        else
        {
            _logger.LogWarning(exception, "Handled exception while processing request {TraceId}", httpContext.TraceIdentifier);
        }

        httpContext.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;
        httpContext.Response.ContentType = "application/problem+json";
        var json = JsonConvert.SerializeObject(problemDetails);
        await httpContext.Response.WriteAsync(json, cancellationToken);

        return true;
    }

    private static ProblemDetails CreateProblemDetails(HttpContext httpContext, Exception exception)
    {
        var (status, title, code, detail) = exception switch
        {
            ValidationException validationFailure => (
                StatusCodes.Status400BadRequest,
                "Validation failed",
                ApiErrorCodes.ValidationFailed,
                validationFailure.Message),
            AuthenticationFailedException authenticationFailedException => (
                StatusCodes.Status401Unauthorized,
                "Authentication failed",
                ApiErrorCodes.Unauthorized,
                authenticationFailedException.Message),
            NotFoundException notFoundException => (
                StatusCodes.Status404NotFound,
                "Resource not found",
                ApiErrorCodes.NotFound,
                notFoundException.Message),
            ConflictException conflictException => (
                StatusCodes.Status409Conflict,
                "Conflict",
                ApiErrorCodes.Conflict,
                conflictException.Message),
            UnauthorizedAccessException unauthorizedAccessException => (
                StatusCodes.Status403Forbidden,
                "Forbidden",
                ApiErrorCodes.Forbidden,
                unauthorizedAccessException.Message),
            DbUpdateConcurrencyException => (
                StatusCodes.Status409Conflict,
                "Concurrency conflict",
                ApiErrorCodes.Conflict,
                "The record was changed by another process. Reload it and try again."),
            _ => (
                StatusCodes.Status500InternalServerError,
                "Unexpected error",
                ApiErrorCodes.UnexpectedError,
                "An unexpected error occurred.")
        };

        var problemDetails = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path,
            Type = $"https://httpstatuses.com/{status}"
        };

        problemDetails.Extensions["code"] = code;
        problemDetails.Extensions["traceId"] = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        if (exception is ValidationException validationException)
        {
            problemDetails.Extensions["errors"] = validationException.Errors;
        }

        return problemDetails;
    }
}

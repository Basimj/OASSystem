using System.Diagnostics;
using OAS.Application.Common.Exceptions;
using OAS.Application.Identity.Exceptions;
using OAS.Contracts.Common.Errors;
using OAS.Domain.Exceptions;


namespace OAS.API.Middleware;

public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    private static readonly Action<ILogger, string, Exception?> LogUnhandledFailure = LoggerMessage.Define<string>(
        LogLevel.Error,
        new EventId(3001, nameof(ExceptionHandlingMiddleware)),
        "Unhandled request failure {TraceId}");

    private static readonly Action<ILogger, int, string, Exception?> LogRequestFailure = LoggerMessage.Define<int, string>(
        LogLevel.Warning,
        new EventId(3002, nameof(ExceptionHandlingMiddleware)),
        "Request failed with {Status} {TraceId}");

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            var (status, code, message, errors) = Map(exception);
            if (status >= StatusCodes.Status500InternalServerError)
                LogUnhandledFailure(logger, context.TraceIdentifier, exception);
            else
                LogRequestFailure(logger, status, context.TraceIdentifier, exception);

            context.Response.StatusCode = status;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsJsonAsync(new ApiError
            {
                Status = status,
                Code = code,
                Message = message,
                TraceId = Activity.Current?.Id ?? context.TraceIdentifier,
                Errors = errors
            }, context.RequestAborted);
        }
    }

    private static (int Status, string Code, string Message, IReadOnlyDictionary<string,string[]> Errors) Map(Exception ex) => ex switch
    {
        RequestValidationException validation => (400, "validation_failed", validation.Message, validation.Errors),
        AuthenticationFailedException => (401, "identity_invalid_credentials", "Authentication failed.", EmptyErrors),
        IdentityConflictException conflict => (409, conflict.Code, "Identity request conflicts with existing data.", EmptyErrors),
        ConflictException conflict =>(409, conflict.Code, conflict.Message, EmptyErrors),
        NotFoundException => (404, "not_found", ex.Message, EmptyErrors),
        ForbiddenException => (403, "forbidden", ex.Message, EmptyErrors),
        ConcurrencyException => (409, "concurrency_conflict", ex.Message, EmptyErrors),
        DomainException => (422, "business_rule_failed", ex.Message, EmptyErrors),
        _ => (500, "unexpected_error", "An unexpected server error occurred.", EmptyErrors)
    };

    private static IReadOnlyDictionary<string, string[]> EmptyErrors { get; } = new Dictionary<string, string[]>();
}

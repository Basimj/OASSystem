using System.Diagnostics;
using System.IO;
using OAS.Application.Common.Exceptions;
using OAS.Application.Identity.Exceptions;
using OAS.Contracts.Common.Errors;
using OAS.Domain.Exceptions;

namespace OAS.API.Middleware;

public sealed class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger)
{
    private const int ClientClosedRequestStatusCode = 499;

    private static readonly Action<ILogger, string, Exception?> LogUnhandledFailure =
        LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(3001, nameof(ExceptionHandlingMiddleware)),
            "Unhandled request failure {TraceId}");

    private static readonly Action<ILogger, int, string, Exception?> LogRequestFailure =
        LoggerMessage.Define<int, string>(
            LogLevel.Warning,
            new EventId(3002, nameof(ExceptionHandlingMiddleware)),
            "Request failed with {Status} {TraceId}");

    private static readonly Action<ILogger, string, Exception?> LogRequestCancelled =
        LoggerMessage.Define<string>(
            LogLevel.Debug,
            new EventId(3003, nameof(ExceptionHandlingMiddleware)),
            "Request was cancelled by the client {TraceId}");

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException exception)
            when (context.RequestAborted.IsCancellationRequested)
        {
            /*
             * A browser can abort a request because the user typed another
             * lookup value, changed page, closed a tab, refreshed, or navigated
             * away. EF Core correctly observes RequestAborted and may throw
             * TaskCanceledException/OperationCanceledException.
             *
             * This is not a 500 and not a business error. Do not create an
             * ApiError for a connection the client has already abandoned.
             */
            LogRequestCancelled(
                logger,
                context.TraceIdentifier,
                exception);

            if (!context.Response.HasStarted)
            {
                context.Response.StatusCode =
                    ClientClosedRequestStatusCode;
            }
        }
        catch (Exception exception)
        {
            /*
             * The request might have been aborted between the time another
             * exception occurred and the time we reached the middleware.
             * There is no useful response body to write in that case.
             */
            if (context.RequestAborted.IsCancellationRequested)
            {
                LogRequestCancelled(
                    logger,
                    context.TraceIdentifier,
                    exception);

                if (!context.Response.HasStarted)
                {
                    context.Response.StatusCode =
                        ClientClosedRequestStatusCode;
                }

                return;
            }

            var (status, code, message, errors) =
                Map(exception);

            if (status >= StatusCodes.Status500InternalServerError)
            {
                LogUnhandledFailure(
                    logger,
                    context.TraceIdentifier,
                    exception);
            }
            else
            {
                LogRequestFailure(
                    logger,
                    status,
                    context.TraceIdentifier,
                    exception);
            }

            if (context.Response.HasStarted)
            {
                throw;
            }

            context.Response.StatusCode = status;
            context.Response.ContentType = "application/problem+json";

            var error = new ApiError
            {
                Status = status,
                Code = code,
                Message = message,
                TraceId = Activity.Current?.Id ?? context.TraceIdentifier,
                Errors = errors
            };

            try
            {
                /*
                 * We already checked RequestAborted above. Do not pass the
                 * request-abort token into error serialization because an abort
                 * racing with this write would create another cancellation
                 * exception from inside the exception handler itself.
                 */
                await context.Response.WriteAsJsonAsync(
                    error,
                    CancellationToken.None);
            }
            catch (OperationCanceledException)
                when (context.RequestAborted.IsCancellationRequested)
            {
                LogRequestCancelled(
                    logger,
                    context.TraceIdentifier,
                    null);
            }
            catch (IOException)
                when (context.RequestAborted.IsCancellationRequested)
            {
                LogRequestCancelled(
                    logger,
                    context.TraceIdentifier,
                    null);
            }
        }
    }

    private static (
        int Status,
        string Code,
        string Message,
        IReadOnlyDictionary<string, string[]> Errors)
        Map(Exception ex) =>
        ex switch
        {
            RequestValidationException validation =>
                (400,
                    "validation_failed",
                    validation.Message,
                    validation.Errors),

            AuthenticationFailedException =>
                (401,
                    "identity_invalid_credentials",
                    "Authentication failed.",
                    EmptyErrors),

            IdentityConflictException conflict =>
                (409,
                    conflict.Code,
                    "Identity request conflicts with existing data.",
                    EmptyErrors),

            ConflictException conflict =>
                (409,
                    conflict.Code,
                    conflict.Message,
                    EmptyErrors),

            NotFoundException =>
                (404,
                    "not_found",
                    ex.Message,
                    EmptyErrors),

            ForbiddenException forbidden =>
                (403,
                    forbidden.Code,
                    forbidden.Message,
                    EmptyErrors),

            ConcurrencyException =>
                (409,
                    "concurrency_conflict",
                    ex.Message,
                    EmptyErrors),

            DomainException =>
                (422,
                    "business_rule_failed",
                    ex.Message,
                    EmptyErrors),

            _ =>
                (500,
                    "unexpected_error",
                    "An unexpected server error occurred.",
                    EmptyErrors)
        };

    private static IReadOnlyDictionary<string, string[]> EmptyErrors { get; } =
        new Dictionary<string, string[]>();
}

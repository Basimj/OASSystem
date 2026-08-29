using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace OAS.Application.Common.Behaviors;

public sealed class PerformanceBehavior<TRequest, TResponse>(ILogger<PerformanceBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    private static readonly TimeSpan WarningThreshold = TimeSpan.FromMilliseconds(750);
    private static readonly Action<ILogger, string, long, Exception?> LogSlowRequest =
        LoggerMessage.Define<string, long>(
            LogLevel.Warning,
            new EventId(1001, nameof(PerformanceBehavior<TRequest, TResponse>)),
            "Slow request {RequestName} took {ElapsedMs} ms");

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        var response = await next(cancellationToken);
        sw.Stop();
        if (sw.Elapsed >= WarningThreshold)
            LogSlowRequest(logger, typeof(TRequest).Name, sw.ElapsedMilliseconds, null);
        return response;
    }
}

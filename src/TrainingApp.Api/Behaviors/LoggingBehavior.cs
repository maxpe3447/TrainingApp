using System.Diagnostics;
using FluentValidation;
using MediatR;
using TrainingApp.Api.Common;

namespace TrainingApp.Api.Behaviors;

/// <summary>
/// MediatR pipeline behavior: writes one log record when a command starts and one when it ends, for every command.
/// Registered before <see cref="ValidationBehavior{TRequest,TResponse}"/>, so it also sees rejected commands.
/// </summary>
/// <remarks>
/// Messages are templates, not interpolated strings: "{Command}" and the other placeholders become separate
/// attributes of the log record, so Loki can filter and aggregate by them.
/// </remarks>
public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    // Static fields of a generic type are per closed type: the name is computed once per command type.
    private static readonly string CommandName = typeof(TRequest).Name;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        logger.LogDebug("Processing {Command}", CommandName);
        var startedAt = Stopwatch.GetTimestamp();

        TResponse response;
        try
        {
            response = await next();
        }
        catch (ValidationException exception)
        {
            // A rejected command is the caller's mistake, not a failure of the service: a warning, not an error.
            logger.LogWarning(
                "{Command} rejected: {Errors}",
                CommandName,
                string.Join("; ", exception.Errors.Select(e => e.ErrorMessage)));
            throw;
        }
        catch (Exception exception)
        {
            // Pass the exception itself, not exception.Message: the stack trace goes into the log record.
            logger.LogError(exception, "{Command} failed", CommandName);
            throw;
        }

        var elapsedMs = Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds;

        if (response is DeliveryQuote quote)
        {
            logger.LogInformation(
                "{Command} quoted {DeliveryType} delivery: {Cost}, arrives {EstimatedArrival}, took {ElapsedMs:F1} ms",
                CommandName, quote.Type, quote.Cost, quote.EstimatedArrival, elapsedMs);
        }
        else
        {
            logger.LogInformation("{Command} processed in {ElapsedMs:F1} ms", CommandName, elapsedMs);
        }

        return response;
    }
}

using System.Diagnostics;
using FluentValidation;
using MediatR;
using TrainingApp.Api.Common;
using TrainingApp.Api.Telemetry;

namespace TrainingApp.Api.Behaviors;

/// <summary>
/// MediatR pipeline behavior: collects metrics for the command currently being processed.
/// Registered before <see cref="ValidationBehavior{TRequest,TResponse}"/>, so it also sees rejected commands.
/// </summary>
public sealed class MetricsBehavior<TRequest, TResponse>(IAppMetrics<TRequest> metrics)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        using var inProgress = metrics.TrackInProgress();
        using var timer = metrics.StartTimer();

        TResponse response;
        try
        {
            response = await next();
        }
        catch (ValidationException)
        {
            metrics.Increment(tags: new TagList { { "outcome", "rejected" } });
            throw;
        }
        catch
        {
            metrics.Increment(tags: new TagList { { "outcome", "error" } });
            throw;
        }

        metrics.Increment(tags: new TagList { { "outcome", "success" } });

        if (response is DeliveryQuote quote)
        {
            metrics.Record((double)quote.Cost);
        }

        return response;
    }
}

using System.Diagnostics;
using FluentValidation;
using MediatR;
using TrainingApp.Api.Common;
using TrainingApp.Api.Telemetry;

namespace TrainingApp.Api.Behaviors;

/// <summary>
/// MediatR pipeline behavior: opens a span named after the command for every command.
/// Registered first, so logs and metrics of the other behaviors are recorded while this span is current:
/// log records get its trace_id, duration measurements get it as an exemplar.
/// </summary>
public sealed class TracingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    // Static fields of a generic type are per closed type: the name is computed once per command type.
    private static readonly string CommandName = typeof(TRequest).Name;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // A child of Activity.Current — the HTTP request span created by the ASP.NET Core instrumentation.
        using var activity = AppTelemetry.ActivitySource.StartActivity(CommandName);
        activity?.SetTag("command", CommandName);

        TResponse response;
        try
        {
            response = await next();
        }
        catch (ValidationException)
        {
            // A rejected command is the caller's mistake, not a failure of the service: the span status stays unset.
            activity?.SetTag("outcome", "rejected");
            throw;
        }
        catch (Exception exception)
        {
            activity?.SetTag("outcome", "error");
            activity?.AddException(exception);
            activity?.SetStatus(ActivityStatusCode.Error, exception.Message);
            throw;
        }

        activity?.SetTag("outcome", "success");

        if (response is DeliveryQuote quote)
        {
            activity?.SetTag("delivery.type", quote.Type.ToString());
            activity?.SetTag("delivery.cost", (double)quote.Cost);
        }

        return response;
    }
}

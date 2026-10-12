using System.Diagnostics;
using MediatR;
using TrainingApp.Api.Common;
using TrainingApp.Api.Telemetry;

namespace TrainingApp.Api.Features.StandardDelivery;

/// <summary>
/// A handler does not have to derive from <see cref="DeliveryQuoteHandler{TCommand}"/>:
/// this one implements <see cref="IRequestHandler{TRequest,TResponse}"/> directly, which leaves room for feature-specific steps.
/// </summary>
/// <remarks>
/// Logging example: plain ILogger calls with message templates. Compare with the generated messages in PickupDeliveryLog.
/// Tracing example: a span of its own with business attributes, a child span, and an event.
/// </remarks>
public sealed class StandardDeliveryHandler(
    IDeliveryStrategy<StandardDeliveryCommand> strategy,
    TimeProvider timeProvider,
    ILogger<StandardDeliveryHandler> logger) : IRequestHandler<StandardDeliveryCommand, DeliveryQuote>
{
    public Task<DeliveryQuote> Handle(StandardDeliveryCommand request, CancellationToken cancellationToken)
    {
        // Attributes describe the input of this step. Unlike metric tags, span attributes may have many distinct values:
        // a span is a single event, not a time series.
        using var activity = AppTelemetry.ActivitySource.StartActivity("StandardDeliveryCommand.quote");
        activity?.SetTag("parcel.weight_kg", (double)request.WeightKg);
        activity?.SetTag("parcel.distance_km", request.DistanceKm);
        activity?.SetTag("parcel.fragile", request.IsFragile);

        var today = DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);

        if (request.IsFragile)
        {
            // Good: a template — WeightKg becomes an attribute you can filter on.
            // Bad:  $"Fragile surcharge applied to a {request.WeightKg} kg parcel" — every message is a new string.
            logger.LogInformation("Fragile surcharge applied to a {WeightKg} kg parcel", request.WeightKg);

            // An event is a point in time inside the span: "this happened here", with its own attributes.
            activity?.AddEvent(new ActivityEvent("fragile_surcharge_applied"));
        }

        decimal cost;
        using (AppTelemetry.ActivitySource.StartActivity("StandardDeliveryCommand.calculate_cost"))
        {
            cost = Math.Round(strategy.CalculateCost(request), 2);
        }

        var quote = new DeliveryQuote(strategy.Type, strategy.Description, cost, strategy.EstimateArrival(request, today));

        // Debug: details useful while developing; Development shows them, Production filters them out (appsettings).
        logger.LogDebug(
            "Standard delivery of {WeightKg} kg over {DistanceKm} km costs {Cost}",
            request.WeightKg, request.DistanceKm, quote.Cost);

        activity?.SetTag("delivery.estimated_arrival", quote.EstimatedArrival.ToString("O"));

        return Task.FromResult(quote);
    }
}

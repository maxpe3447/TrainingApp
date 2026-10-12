using MediatR;
using TrainingApp.Api.Common;

namespace TrainingApp.Api.Features.StandardDelivery;

/// <summary>
/// A handler does not have to derive from <see cref="DeliveryQuoteHandler{TCommand}"/>:
/// this one implements <see cref="IRequestHandler{TRequest,TResponse}"/> directly, which leaves room for feature-specific steps.
/// </summary>
/// <remarks>
/// Logging example: plain ILogger calls with message templates. Compare with the generated messages in PickupDeliveryLog.
/// </remarks>
public sealed class StandardDeliveryHandler(
    IDeliveryStrategy<StandardDeliveryCommand> strategy,
    TimeProvider timeProvider,
    ILogger<StandardDeliveryHandler> logger) : IRequestHandler<StandardDeliveryCommand, DeliveryQuote>
{
    public Task<DeliveryQuote> Handle(StandardDeliveryCommand request, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);

        if (request.IsFragile)
        {
            // Good: a template — WeightKg becomes an attribute you can filter on.
            // Bad:  $"Fragile surcharge applied to a {request.WeightKg} kg parcel" — every message is a new string.
            logger.LogInformation("Fragile surcharge applied to a {WeightKg} kg parcel", request.WeightKg);
        }

        var quote = new DeliveryQuote(
            strategy.Type,
            strategy.Description,
            Math.Round(strategy.CalculateCost(request), 2),
            strategy.EstimateArrival(request, today));

        // Debug: details useful while developing; Development shows them, Production filters them out (appsettings).
        logger.LogDebug(
            "Standard delivery of {WeightKg} kg over {DistanceKm} km costs {Cost}",
            request.WeightKg, request.DistanceKm, quote.Cost);

        return Task.FromResult(quote);
    }
}

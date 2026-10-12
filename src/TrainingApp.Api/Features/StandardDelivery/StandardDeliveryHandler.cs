using MediatR;
using TrainingApp.Api.Common;

namespace TrainingApp.Api.Features.StandardDelivery;

/// <summary>
/// A handler does not have to derive from <see cref="DeliveryQuoteHandler{TCommand}"/>:
/// this one implements <see cref="IRequestHandler{TRequest,TResponse}"/> directly, which leaves room for feature-specific steps.
/// </summary>
public sealed class StandardDeliveryHandler(
    IDeliveryStrategy<StandardDeliveryCommand> strategy,
    TimeProvider timeProvider) : IRequestHandler<StandardDeliveryCommand, DeliveryQuote>
{
    public Task<DeliveryQuote> Handle(StandardDeliveryCommand request, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);

        var quote = new DeliveryQuote(
            strategy.Type,
            strategy.Description,
            Math.Round(strategy.CalculateCost(request), 2),
            strategy.EstimateArrival(request, today));

        return Task.FromResult(quote);
    }
}

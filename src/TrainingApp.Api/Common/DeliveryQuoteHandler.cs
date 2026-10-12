using MediatR;

namespace TrainingApp.Api.Common;

/// <summary>
/// Strategy pattern context: the quote algorithm is written once and works only through the strategy interface.
/// </summary>
public abstract class DeliveryQuoteHandler<TCommand>(
    IDeliveryStrategy<TCommand> strategy,
    TimeProvider timeProvider) : IRequestHandler<TCommand, DeliveryQuote>
    where TCommand : IDeliveryCommand
{
    public Task<DeliveryQuote> Handle(TCommand request, CancellationToken cancellationToken)
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

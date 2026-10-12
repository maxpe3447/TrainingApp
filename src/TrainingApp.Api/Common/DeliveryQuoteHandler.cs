using MediatR;
using TrainingApp.Api.Telemetry;

namespace TrainingApp.Api.Common;

/// <summary>
/// Strategy pattern context: the quote algorithm is written once and works only through the strategy interface.
/// </summary>
/// <remarks>
/// Every step of the algorithm is a child span, so a trace shows where the time of a quote goes.
/// The static <see cref="AppTelemetry.ActivitySource"/> needs no constructor parameter.
/// </remarks>
public abstract class DeliveryQuoteHandler<TCommand>(
    IDeliveryStrategy<TCommand> strategy,
    TimeProvider timeProvider) : IRequestHandler<TCommand, DeliveryQuote>
    where TCommand : IDeliveryCommand
{
    private static readonly string CommandName = typeof(TCommand).Name;

    public Task<DeliveryQuote> Handle(TCommand request, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);

        decimal cost;
        using (AppTelemetry.ActivitySource.StartActivity($"{CommandName}.calculate_cost"))
        {
            cost = Math.Round(strategy.CalculateCost(request), 2);
        }

        DateOnly arrival;
        using (AppTelemetry.ActivitySource.StartActivity($"{CommandName}.estimate_arrival"))
        {
            arrival = strategy.EstimateArrival(request, today);
        }

        return Task.FromResult(new DeliveryQuote(strategy.Type, strategy.Description, cost, arrival));
    }
}

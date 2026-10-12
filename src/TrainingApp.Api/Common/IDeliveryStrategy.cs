namespace TrainingApp.Api.Common;

/// <summary>
/// Delivery strategy typed by the command it handles.
/// DI resolves the implementation by the command type: IDeliveryStrategy&lt;ExpressDeliveryCommand&gt; → ExpressDeliveryStrategy.
/// </summary>
public interface IDeliveryStrategy<in TCommand> where TCommand : IDeliveryCommand
{
    DeliveryType Type { get; }

    string Description { get; }

    bool CanDeliver(TCommand command);

    decimal CalculateCost(TCommand command);

    DateOnly EstimateArrival(TCommand command, DateOnly shipDate);
}

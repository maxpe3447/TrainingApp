using TrainingApp.Api.Common;

namespace TrainingApp.Api.Features.StandardDelivery;

public sealed class StandardDeliveryStrategy : IDeliveryStrategy<StandardDeliveryCommand>
{
    private const decimal MaxWeightKg = 30m;

    public DeliveryType Type => DeliveryType.Standard;

    public string Description => "Standard courier delivery";

    public bool CanDeliver(StandardDeliveryCommand command) => command.WeightKg <= MaxWeightKg;

    public decimal CalculateCost(StandardDeliveryCommand command)
    {
        var cost = 5m + command.WeightKg * 1.5m + command.DistanceKm * 0.05m;
        return command.IsFragile ? cost + 10m : cost;
    }

    public DateOnly EstimateArrival(StandardDeliveryCommand command, DateOnly shipDate) =>
        shipDate.AddDays(3 + command.DistanceKm / 500);
}

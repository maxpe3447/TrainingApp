using TrainingApp.Api.Common;

namespace TrainingApp.Api.Features.PickupDelivery;

/// <summary>
/// Logging example: the strategy logs WHY it makes a decision — something no generic behavior can know.
/// Strategies are singletons resolved from DI, so they can take dependencies such as a logger.
/// </summary>
public sealed class PickupDeliveryStrategy(ILogger<PickupDeliveryStrategy> logger) : IDeliveryStrategy<PickupDeliveryCommand>
{
    private const decimal MaxWeightKg = 15m;

    // Pickup point → number of days until the parcel is ready for pickup.
    private static readonly Dictionary<string, int> PickupPoints = new(StringComparer.OrdinalIgnoreCase)
    {
        ["KYIV-01"] = 0,
        ["KYIV-02"] = 1,
        ["LVIV-01"] = 2
    };

    public DeliveryType Type => DeliveryType.Pickup;

    public string Description => "Pickup from a pickup point";

    public bool CanDeliver(PickupDeliveryCommand command)
    {
        if (!PickupPoints.ContainsKey(command.PickupPointId))
        {
            logger.UnknownPickupPoint(command.PickupPointId);
            return false;
        }

        if (command.WeightKg > MaxWeightKg)
        {
            logger.ParcelTooHeavy(command.WeightKg, MaxWeightKg);
            return false;
        }

        return true;
    }

    public decimal CalculateCost(PickupDeliveryCommand command) => 0m;

    public DateOnly EstimateArrival(PickupDeliveryCommand command, DateOnly shipDate)
    {
        var readyInDays = PickupPoints[command.PickupPointId];
        logger.ParcelReadyIn(command.PickupPointId, readyInDays);

        return shipDate.AddDays(readyInDays);
    }
}

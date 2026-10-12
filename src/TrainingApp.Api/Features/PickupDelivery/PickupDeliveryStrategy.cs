using TrainingApp.Api.Common;

namespace TrainingApp.Api.Features.PickupDelivery;

public sealed class PickupDeliveryStrategy : IDeliveryStrategy<PickupDeliveryCommand>
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

    public bool CanDeliver(PickupDeliveryCommand command) =>
        command.WeightKg <= MaxWeightKg && PickupPoints.ContainsKey(command.PickupPointId);

    public decimal CalculateCost(PickupDeliveryCommand command) => 0m;

    public DateOnly EstimateArrival(PickupDeliveryCommand command, DateOnly shipDate) =>
        shipDate.AddDays(PickupPoints[command.PickupPointId]);
}

using TrainingApp.Api.Common;

namespace TrainingApp.Api.Features.ExpressDelivery;

public sealed class ExpressDeliveryStrategy : IDeliveryStrategy<ExpressDeliveryCommand>
{
    private const decimal MaxWeightKg = 10m;
    private const int MaxDistanceKm = 1000;
    private const decimal InsuranceRate = 0.01m;

    public DeliveryType Type => DeliveryType.Express;

    public string Description => "Insured next-day express delivery";

    public bool CanDeliver(ExpressDeliveryCommand command) =>
        command.WeightKg <= MaxWeightKg && command.DistanceKm <= MaxDistanceKm;

    public decimal CalculateCost(ExpressDeliveryCommand command)
    {
        var cost = 20m + command.WeightKg * 4m + command.DistanceKm * 0.15m;
        cost = command.IsFragile ? cost * 1.2m : cost;
        return cost + command.DeclaredValue * InsuranceRate;
    }

    public DateOnly EstimateArrival(ExpressDeliveryCommand command, DateOnly shipDate) => shipDate.AddDays(1);
}

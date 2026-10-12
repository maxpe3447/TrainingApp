using System.Diagnostics;
using System.Diagnostics.Metrics;
using TrainingApp.Api.Common;
using TrainingApp.Api.Telemetry;

namespace TrainingApp.Api.Features.ExpressDelivery;

/// <summary>
/// Metrics example: business metrics that only this feature can record —
/// why parcels are refused and what value customers insure.
/// The technical metrics (rate, errors, duration) come for free from MetricsBehavior.
/// </summary>
public sealed class ExpressDeliveryStrategy : IDeliveryStrategy<ExpressDeliveryCommand>
{
    private const decimal MaxWeightKg = 10m;
    private const int MaxDistanceKm = 1000;
    private const decimal InsuranceRate = 0.01m;

    // Histogram bucket boundaries for declared values: business values need their own buckets,
    // the default ones (0, 5, 10, 25 … 10000) are tuned for milliseconds.
    private static readonly double[] DeclaredValueBuckets = [0, 100, 500, 1_000, 5_000, 10_000, 50_000, 100_000];

    private readonly Counter<long> _refusals;
    private readonly Histogram<double> _declaredValue;

    // Instruments are created once: the strategy is a singleton. IMeterFactory (not "new Meter") ties the meter
    // to the DI container, so tests can observe it in isolation.
    public ExpressDeliveryStrategy(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(AppTelemetry.MeterName);

        // Prometheus adds the _total suffix to counters: express_delivery_refusals_total.
        _refusals = meter.CreateCounter<long>(
            "express_delivery_refusals",
            unit: "{parcel}",
            description: "Parcels refused by express delivery, by reason.");

        _declaredValue = meter.CreateHistogram(
            "express_delivery_declared_value",
            unit: "{currency}",
            description: "Declared value of parcels quoted for express delivery.",
            tags: null,
            advice: new InstrumentAdvice<double> { HistogramBucketBoundaries = DeclaredValueBuckets });
    }

    public DeliveryType Type => DeliveryType.Express;

    public string Description => "Insured next-day express delivery";

    public bool CanDeliver(ExpressDeliveryCommand command)
    {
        // A tag must have few distinct values (low cardinality): a fixed reason is fine,
        // the weight itself or a customer id would create a new time series per value.
        if (command.WeightKg > MaxWeightKg)
        {
            _refusals.Add(1, new KeyValuePair<string, object?>("reason", "too_heavy"));
            return false;
        }

        if (command.DistanceKm > MaxDistanceKm)
        {
            _refusals.Add(1, new KeyValuePair<string, object?>("reason", "too_far"));
            return false;
        }

        return true;
    }

    public decimal CalculateCost(ExpressDeliveryCommand command)
    {
        _declaredValue.Record((double)command.DeclaredValue, new TagList { { "fragile", command.IsFragile } });

        var cost = 20m + command.WeightKg * 4m + command.DistanceKm * 0.15m;
        cost = command.IsFragile ? cost * 1.2m : cost;
        return cost + command.DeclaredValue * InsuranceRate;
    }

    public DateOnly EstimateArrival(ExpressDeliveryCommand command, DateOnly shipDate) => shipDate.AddDays(1);
}

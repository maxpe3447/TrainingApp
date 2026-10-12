using System.Diagnostics.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using TrainingApp.Api.Features.ExpressDelivery;
using TrainingApp.Api.Telemetry;

namespace TrainingApp.UnitTests.Features.ExpressDelivery;

/// <summary>
/// Testing business metrics: MetricCollector records every measurement of one instrument in memory.
/// The strategy needs an IMeterFactory: a real one from a tiny DI container is simpler and safer than a mock,
/// and it isolates this test's meters from every other test running in parallel.
/// </summary>
public sealed class ExpressDeliveryStrategyTests : IDisposable
{
    private readonly ServiceProvider _services = new ServiceCollection().AddMetrics().BuildServiceProvider();
    private readonly IMeterFactory _meterFactory;
    private readonly ExpressDeliveryStrategy _strategy;

    public ExpressDeliveryStrategyTests()
    {
        _meterFactory = _services.GetRequiredService<IMeterFactory>();
        _strategy = new ExpressDeliveryStrategy(_meterFactory);
    }

    public void Dispose() => _services.Dispose();

    [Fact]
    public void CalculateCost_FragileInsuredParcel_AppliesSurchargeAndInsurance()
    {
        var command = new ExpressDeliveryCommand(WeightKg: 2.5m, DistanceKm: 300, IsFragile: true, DeclaredValue: 1500);

        var cost = _strategy.CalculateCost(command);

        // (20 + 2.5 × 4 + 300 × 0.15) × 1.2 + 1500 × 0.01
        cost.Should().Be(105m);
    }

    [Theory]
    [InlineData(10.5, 100, "too_heavy")]
    [InlineData(5, 1500, "too_far")]
    public void CanDeliver_RefusedParcel_CountsRefusalWithReason(decimal weightKg, int distanceKm, string reason)
    {
        using var refusals = new MetricCollector<long>(_meterFactory, AppTelemetry.MeterName, "express_delivery_refusals");

        var canDeliver = _strategy.CanDeliver(new ExpressDeliveryCommand(weightKg, distanceKm, false, 0));

        canDeliver.Should().BeFalse();
        refusals.GetMeasurementSnapshot().Should().ContainSingle()
            .Which.Should().Match<CollectedMeasurement<long>>(m => m.Value == 1 && m.ContainsTags("reason"));
        refusals.LastMeasurement!.Tags["reason"].Should().Be(reason);
    }

    [Fact]
    public void CanDeliver_AcceptedParcel_CountsNothing()
    {
        using var refusals = new MetricCollector<long>(_meterFactory, AppTelemetry.MeterName, "express_delivery_refusals");

        _strategy.CanDeliver(new ExpressDeliveryCommand(5, 500, false, 0));

        refusals.GetMeasurementSnapshot().Should().BeEmpty();
    }

    [Fact]
    public void CalculateCost_RecordsDeclaredValueWithFragileTag()
    {
        using var declaredValue = new MetricCollector<double>(_meterFactory, AppTelemetry.MeterName, "express_delivery_declared_value");

        _strategy.CalculateCost(new ExpressDeliveryCommand(1, 10, IsFragile: true, DeclaredValue: 750));

        declaredValue.LastMeasurement.Should().NotBeNull();
        declaredValue.LastMeasurement!.Value.Should().Be(750);
        declaredValue.LastMeasurement.Tags.Should().Contain("fragile", true);
    }
}

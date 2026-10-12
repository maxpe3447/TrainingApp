using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using TrainingApp.Api.Features.PickupDelivery;

namespace TrainingApp.UnitTests.Features.PickupDelivery;

/// <summary>
/// Testing logs: FakeLogger keeps every record in memory, with its level, message and template arguments.
/// Prefer it to Mock&lt;ILogger&gt;: LogInformation & co. are extension methods and cannot be verified with Moq directly.
/// </summary>
public sealed class PickupDeliveryStrategyTests
{
    private readonly FakeLogger<PickupDeliveryStrategy> _logger = new();
    private readonly PickupDeliveryStrategy _strategy;

    public PickupDeliveryStrategyTests()
    {
        _strategy = new PickupDeliveryStrategy(_logger);
    }

    [Fact]
    public void CanDeliver_UnknownPickupPoint_LogsWhy()
    {
        var canDeliver = _strategy.CanDeliver(new PickupDeliveryCommand("ODESA-99", WeightKg: 3));

        canDeliver.Should().BeFalse();
        _logger.LatestRecord.Level.Should().Be(LogLevel.Debug);
        _logger.LatestRecord.Message.Should().Be("Pickup point ODESA-99 does not exist");
        // Template arguments are checked separately from the text: that is what Loki filters on.
        _logger.LatestRecord.GetStructuredStateValue("PickupPointId").Should().Be("ODESA-99");
    }

    [Fact]
    public void CanDeliver_TooHeavyParcel_LogsWeightAndLimit()
    {
        var canDeliver = _strategy.CanDeliver(new PickupDeliveryCommand("KYIV-01", WeightKg: 20));

        canDeliver.Should().BeFalse();
        _logger.LatestRecord.GetStructuredStateValue("WeightKg").Should().Be("20");
        _logger.LatestRecord.GetStructuredStateValue("MaxWeightKg").Should().Be("15");
    }

    [Fact]
    public void CanDeliver_AcceptedParcel_LogsNothing()
    {
        _strategy.CanDeliver(new PickupDeliveryCommand("kyiv-01", WeightKg: 3)).Should().BeTrue("pickup point ids are case-insensitive");

        _logger.Collector.Count.Should().Be(0);
    }

    [Theory]
    [InlineData("KYIV-01", 0)]
    [InlineData("LVIV-01", 2)]
    public void EstimateArrival_ReturnsPickupPointReadiness_AndLogsIt(string pickupPointId, int days)
    {
        var shipDate = new DateOnly(2026, 3, 2);

        var arrival = _strategy.EstimateArrival(new PickupDeliveryCommand(pickupPointId, 3), shipDate);

        arrival.Should().Be(shipDate.AddDays(days));
        _logger.LatestRecord.Level.Should().Be(LogLevel.Information);
        _logger.LatestRecord.GetStructuredStateValue("ReadyInDays").Should().Be(days.ToString());
    }
}

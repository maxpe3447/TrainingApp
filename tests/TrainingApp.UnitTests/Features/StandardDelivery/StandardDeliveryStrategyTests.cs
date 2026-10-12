using TrainingApp.Api.Features.StandardDelivery;

namespace TrainingApp.UnitTests.Features.StandardDelivery;

/// <summary>
/// The simplest unit test: a class without dependencies, so there is nothing to mock.
/// Test names follow Method_Scenario_ExpectedResult; each test has Arrange / Act / Assert blocks.
/// </summary>
public sealed class StandardDeliveryStrategyTests
{
    private readonly StandardDeliveryStrategy _strategy = new();

    [Fact]
    public void CalculateCost_RegularParcel_AddsWeightAndDistanceToBasePrice()
    {
        // Arrange
        var command = new StandardDeliveryCommand(WeightKg: 2, DistanceKm: 100, IsFragile: false);

        // Act
        var cost = _strategy.CalculateCost(command);

        // Assert: 5 base + 2 kg × 1.5 + 100 km × 0.05
        cost.Should().Be(13m);
    }

    [Fact]
    public void CalculateCost_FragileParcel_AddsFixedSurcharge()
    {
        var regular = _strategy.CalculateCost(new StandardDeliveryCommand(2, 100, IsFragile: false));

        var fragile = _strategy.CalculateCost(new StandardDeliveryCommand(2, 100, IsFragile: true));

        fragile.Should().Be(regular + 10m, "fragile parcels cost a fixed 10 more");
    }

    // A theory runs the same test for every row of data: one test per boundary instead of copy-paste.
    [Theory]
    [InlineData(1, true)]
    [InlineData(30, true)]
    [InlineData(30.01, false)]
    [InlineData(100, false)]
    public void CanDeliver_DependsOnWeightLimitOf30Kg(decimal weightKg, bool expected)
    {
        var command = new StandardDeliveryCommand(weightKg, DistanceKm: 10, IsFragile: false);

        _strategy.CanDeliver(command).Should().Be(expected);
    }

    [Theory]
    [InlineData(100, 3)]
    [InlineData(499, 3)]
    [InlineData(500, 4)]
    [InlineData(1200, 5)]
    public void EstimateArrival_AddsOneDayPerFull500Km(int distanceKm, int expectedDays)
    {
        var shipDate = new DateOnly(2026, 3, 2);

        var arrival = _strategy.EstimateArrival(new StandardDeliveryCommand(1, distanceKm, false), shipDate);

        arrival.Should().Be(shipDate.AddDays(expectedDays));
    }
}

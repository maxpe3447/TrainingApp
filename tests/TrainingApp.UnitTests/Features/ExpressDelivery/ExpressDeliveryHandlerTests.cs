using Microsoft.Extensions.Time.Testing;
using TrainingApp.Api.Common;
using TrainingApp.Api.Features.ExpressDelivery;

namespace TrainingApp.UnitTests.Features.ExpressDelivery;

/// <summary>
/// A unit test of one method — <see cref="DeliveryQuoteHandler{TCommand}.Handle"/> — with every dependency replaced:
///   the strategy by a Moq mock: we decide what it returns and verify how it was called;
///   the clock by FakeTimeProvider: "today" is fixed, so the arrival date is deterministic.
/// The real ExpressDeliveryStrategy is not involved: its rules are tested in their own tests.
/// </summary>
public sealed class ExpressDeliveryHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 2, 9, 30, 0, TimeSpan.Zero);

    private readonly Mock<IDeliveryStrategy<ExpressDeliveryCommand>> _strategy = new();
    private readonly FakeTimeProvider _time = new(Now);
    private readonly ExpressDeliveryHandler _handler;

    public ExpressDeliveryHandlerTests()
    {
        _time.SetLocalTimeZone(TimeZoneInfo.Utc);

        // Setup: what the mock returns. It.IsAny matches any argument.
        _strategy.Setup(s => s.Type).Returns(DeliveryType.Express);
        _strategy.Setup(s => s.Description).Returns("Test express");
        _strategy.Setup(s => s.CalculateCost(It.IsAny<ExpressDeliveryCommand>())).Returns(10.456m);
        _strategy.Setup(s => s.EstimateArrival(It.IsAny<ExpressDeliveryCommand>(), It.IsAny<DateOnly>()))
            .Returns((ExpressDeliveryCommand _, DateOnly shipDate) => shipDate.AddDays(1));

        _handler = new ExpressDeliveryHandler(_strategy.Object, _time);
    }

    [Fact]
    public async Task Handle_BuildsQuoteFromStrategy()
    {
        var command = new ExpressDeliveryCommand(WeightKg: 1, DistanceKm: 10, IsFragile: false, DeclaredValue: 0);

        var quote = await _handler.Handle(command, TestContext.Current.CancellationToken);

        // BeEquivalentTo compares member by member and reports every difference at once.
        quote.Should().BeEquivalentTo(new DeliveryQuote(
            DeliveryType.Express,
            "Test express",
            Cost: 10.46m,
            EstimatedArrival: new DateOnly(2026, 3, 3)));
    }

    [Fact]
    public async Task Handle_RoundsCostToCents()
    {
        var quote = await _handler.Handle(new ExpressDeliveryCommand(1, 10, false, 0), TestContext.Current.CancellationToken);

        quote.Cost.Should().Be(10.46m, "10.456 is rounded to two decimal places");
    }

    [Fact]
    public async Task Handle_PassesTodayAsShipDate()
    {
        var command = new ExpressDeliveryCommand(1, 10, false, 0);

        await _handler.Handle(command, TestContext.Current.CancellationToken);

        // Verify: how the mock was called. It.Is checks the argument; Times.Once — exactly one call.
        _strategy.Verify(s => s.EstimateArrival(command, It.Is<DateOnly>(d => d == new DateOnly(2026, 3, 2))), Times.Once);
    }

    [Fact]
    public async Task Handle_UsesLocalDate_WhenTimeZoneIsAheadOfUtc()
    {
        // 23:30 UTC is already the next day in Kyiv (UTC+2 in March).
        _time.SetUtcNow(new DateTimeOffset(2026, 3, 2, 23, 30, 0, TimeSpan.Zero));
        _time.SetLocalTimeZone(TimeZoneInfo.CreateCustomTimeZone("UTC+2", TimeSpan.FromHours(2), "UTC+2", "UTC+2"));

        var quote = await _handler.Handle(new ExpressDeliveryCommand(1, 10, false, 0), TestContext.Current.CancellationToken);

        quote.EstimatedArrival.Should().Be(new DateOnly(2026, 3, 4));
    }

    [Fact]
    public async Task Handle_CallsEachStrategyMethodOnce()
    {
        var command = new ExpressDeliveryCommand(1, 10, false, 0);

        await _handler.Handle(command, TestContext.Current.CancellationToken);

        _strategy.Verify(s => s.CalculateCost(command), Times.Once);
        _strategy.Verify(s => s.EstimateArrival(command, It.IsAny<DateOnly>()), Times.Once);
        // The handler trusts the validator: it must not re-check CanDeliver.
        _strategy.Verify(s => s.CanDeliver(It.IsAny<ExpressDeliveryCommand>()), Times.Never);
    }
}

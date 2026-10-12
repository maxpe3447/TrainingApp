using System.Diagnostics;
using FluentValidation;
using MediatR;
using TrainingApp.Api.Behaviors;
using TrainingApp.Api.Common;
using TrainingApp.Api.Features.StandardDelivery;
using TrainingApp.Api.Telemetry;

namespace TrainingApp.UnitTests.Behaviors;

/// <summary>
/// IAppMetrics is an interface, so the behavior can be tested with a mock instead of a real meter:
/// the test checks WHAT the behavior reports, not how OpenTelemetry exports it.
/// </summary>
public sealed class MetricsBehaviorTests
{
    private static readonly StandardDeliveryCommand Command = new(1, 10, false);
    private static readonly DeliveryQuote Quote = new(DeliveryType.Standard, "Test", 12.5m, new DateOnly(2026, 3, 5));

    // Loose (the default) mock: methods without a setup do nothing and return default values.
    // StartTimer/TrackInProgress return default structs, whose Dispose is a no-op.
    private readonly Mock<IAppMetrics<StandardDeliveryCommand>> _metrics = new();
    private readonly MetricsBehavior<StandardDeliveryCommand, DeliveryQuote> _behavior;

    public MetricsBehaviorTests()
    {
        _behavior = new MetricsBehavior<StandardDeliveryCommand, DeliveryQuote>(_metrics.Object);
    }

    [Fact]
    public async Task Handle_Success_CountsSuccessAndRecordsCost()
    {
        await _behavior.Handle(Command, _ => Task.FromResult(Quote), TestContext.Current.CancellationToken);

        // Optional arguments are not allowed in expression trees, so every argument is spelled out.
        _metrics.Verify(m => m.Increment(1, It.Is<TagList>(tags => HasOutcome(tags, "success"))), Times.Once);
        _metrics.Verify(m => m.Record(12.5, It.IsAny<TagList>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ValidationFailure_CountsRejectedAndRethrows()
    {
        RequestHandlerDelegate<DeliveryQuote> next = _ => throw new ValidationException("Invalid");

        var act = () => _behavior.Handle(Command, next, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>();
        _metrics.Verify(m => m.Increment(1, It.Is<TagList>(tags => HasOutcome(tags, "rejected"))), Times.Once);
        _metrics.Verify(m => m.Record(It.IsAny<double>(), It.IsAny<TagList>()), Times.Never);
    }

    [Fact]
    public async Task Handle_UnexpectedException_CountsErrorAndRethrows()
    {
        RequestHandlerDelegate<DeliveryQuote> next = _ => throw new InvalidOperationException("Boom");

        var act = () => _behavior.Handle(Command, next, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Boom");
        _metrics.Verify(m => m.Increment(1, It.Is<TagList>(tags => HasOutcome(tags, "error"))), Times.Once);
    }

    [Fact]
    public async Task Handle_AlwaysTracksDurationAndInProgress()
    {
        await _behavior.Handle(Command, _ => Task.FromResult(Quote), TestContext.Current.CancellationToken);

        _metrics.Verify(m => m.StartTimer(It.IsAny<TagList>()), Times.Once);
        _metrics.Verify(m => m.TrackInProgress(It.IsAny<TagList>()), Times.Once);
    }

    private static bool HasOutcome(TagList tags, string outcome) =>
        tags.Any(tag => tag.Key == "outcome" && Equals(tag.Value, outcome));
}

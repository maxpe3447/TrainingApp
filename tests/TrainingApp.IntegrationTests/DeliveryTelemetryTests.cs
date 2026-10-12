using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using TrainingApp.Api.Features.StandardDelivery;
using TrainingApp.Api.Telemetry;

namespace TrainingApp.IntegrationTests;

/// <summary>
/// Telemetry is behavior too: dashboards and alerts depend on metric names, tags and spans.
/// These tests catch a renamed metric or a lost span before production does.
/// </summary>
[Collection(IntegrationCollection.Name)]
public sealed class DeliveryTelemetryTests(TrainingAppFactory factory)
{
    [Fact]
    public async Task Send_SuccessfulCommand_IsCountedWithSuccessOutcome()
    {
        // MetricCollector listens to one instrument of meters created by this IMeterFactory only —
        // other applications (or tests) in the same process do not interfere.
        var meterFactory = factory.Services.GetRequiredService<IMeterFactory>();
        using var commands = new MetricCollector<long>(meterFactory, AppTelemetry.MeterName, "standard_delivery_command");

        await SendAsync(new StandardDeliveryCommand(1, 10, false));

        commands.GetMeasurementSnapshot().Should().ContainSingle(m => Equals(m.Tags["outcome"], "success"));
    }

    [Fact]
    public async Task Send_FragileParcel_CreatesQuoteSpanWithAttributesAndEvent()
    {
        var spans = new ConcurrentQueue<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == AppTelemetry.ActivitySourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = spans.Enqueue
        };
        ActivitySource.AddActivityListener(listener);

        // A parent span of our own: everything the command creates shares its trace id,
        // which separates our spans from those of other tests running at the same time.
        using var parent = new Activity("test").Start();
        await SendAsync(new StandardDeliveryCommand(2, 100, IsFragile: true));
        var trace = spans.Where(span => span.TraceId == parent.TraceId).ToList();

        trace.Select(span => span.OperationName).Should().Contain(
            ["StandardDeliveryCommand", "StandardDeliveryCommand.quote", "StandardDeliveryCommand.calculate_cost"]);

        var quote = trace.Single(span => span.OperationName == "StandardDeliveryCommand.quote");
        quote.GetTagItem("parcel.weight_kg").Should().Be(2d);
        quote.GetTagItem("parcel.fragile").Should().Be(true);
        quote.Events.Select(e => e.Name).Should().Contain("fragile_surcharge_applied");
        quote.Parent!.OperationName.Should().Be("StandardDeliveryCommand", "the handler span is a child of the command span");
    }

    private async Task SendAsync(StandardDeliveryCommand command)
    {
        using var scope = factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<ISender>().Send(command, TestContext.Current.CancellationToken);
    }
}

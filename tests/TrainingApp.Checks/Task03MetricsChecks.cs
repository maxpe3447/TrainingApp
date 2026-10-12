using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using TrainingApp.Api.Telemetry;

namespace TrainingApp.Checks;

/// <summary>Task 03: business metrics of your feature. See tasks/03-metrics.md.</summary>
[Trait("Task", "03")]
public sealed partial class Task03MetricsChecks(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Feature_records_a_business_metric()
    {
        var feature = StudentFeature.Find();

        var measurements = await QuoteAndCollectBusinessMeasurementsAsync(feature);

        measurements.Should().NotBeEmpty(
            "record at least one metric of your own while quoting MyFeature.ValidRequest: inject IMeterFactory into your strategy, " +
            "create an instrument on meterFactory.Create(AppTelemetry.MeterName) and record to it, see ExpressDeliveryStrategy; " +
            "the {0}_* metrics recorded by MetricsBehavior do not count", CommandMetricName(feature));
    }

    [Fact]
    public async Task Business_metric_has_a_tag()
    {
        var feature = StudentFeature.Find();

        var measurements = await QuoteAndCollectBusinessMeasurementsAsync(feature);

        measurements.Should().Contain(m => m.Tags.Length > 0,
            "add at least one tag (dimension) with a small fixed set of values, e.g. reason, region or fragile; " +
            "tags let you split one metric in Grafana with 'sum by (tag)'");
    }

    [Fact]
    public async Task Business_metric_names_follow_conventions()
    {
        var feature = StudentFeature.Find();

        var measurements = await QuoteAndCollectBusinessMeasurementsAsync(feature);

        foreach (var name in measurements.Select(m => m.Instrument).Distinct())
        {
            SnakeCase().IsMatch(name).Should().BeTrue(
                "metric names are lowercase snake_case, e.g. same_day_delivery_refusals, but found '{0}'", name);
            name.Should().NotEndWith("_total", "the Prometheus exporter adds _total to counters itself");
        }
    }

    [Fact]
    public async Task Business_metric_is_exported_to_prometheus()
    {
        var feature = StudentFeature.Find();
        var measurements = await QuoteAndCollectBusinessMeasurementsAsync(feature);
        using var client = factory.CreateClient();

        var scrape = await client.GetStringAsync("/metrics", TestContext.Current.CancellationToken);

        foreach (var name in measurements.Select(m => m.Instrument).Distinct())
        {
            scrape.Should().Contain(name, "GET /metrics is what Prometheus scrapes; your metric must be there");
        }
    }

    private async Task<IReadOnlyList<Measurement>> QuoteAndCollectBusinessMeasurementsAsync(StudentFeature feature)
    {
        // Only meters of this application instance (created by its IMeterFactory) and only the exported meter name.
        var meterFactory = factory.Services.GetRequiredService<IMeterFactory>();
        var commandMetrics = CommandMetricName(feature);
        var measurements = new ConcurrentQueue<Measurement>();

        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, l) =>
        {
            if (instrument.Meter.Scope == meterFactory
                && instrument.Meter.Name == AppTelemetry.MeterName
                && !instrument.Name.StartsWith(commandMetrics, StringComparison.Ordinal))
            {
                l.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((i, _, tags, _) => measurements.Enqueue(new(i.Name, tags.ToArray())));
        listener.SetMeasurementEventCallback<int>((i, _, tags, _) => measurements.Enqueue(new(i.Name, tags.ToArray())));
        listener.SetMeasurementEventCallback<double>((i, _, tags, _) => measurements.Enqueue(new(i.Name, tags.ToArray())));
        listener.SetMeasurementEventCallback<decimal>((i, _, tags, _) => measurements.Enqueue(new(i.Name, tags.ToArray())));
        listener.Start();

        using var client = factory.CreateClient();
        await StudentFeature.PostValidRequestAsync(client, TestContext.Current.CancellationToken);

        return measurements.ToList();
    }

    // Name prefix of the technical metrics MetricsBehavior records for the command, e.g. same_day_delivery_command.
    private static string CommandMetricName(StudentFeature feature) =>
        JsonNamingPolicy.SnakeCaseLower.ConvertName(feature.CommandType.Name);

    [GeneratedRegex("^[a-z][a-z0-9_]*$")]
    private static partial Regex SnakeCase();

    private sealed record Measurement(string Instrument, KeyValuePair<string, object?>[] Tags);
}

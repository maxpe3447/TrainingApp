using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Text.Json;

namespace TrainingApp.Api.Telemetry;

/// <summary>
/// Creates the instruments for <typeparamref name="TCommand"/> once; registered as a singleton per closed type.
/// </summary>
public sealed class AppMetrics<TCommand> : IAppMetrics<TCommand> where TCommand : notnull
{
    private static readonly double[] DurationBuckets =
        [0.0005, 0.001, 0.0025, 0.005, 0.01, 0.025, 0.05, 0.1, 0.25, 0.5, 1, 2.5, 5];

    // Static fields of a generic type are per closed type, so the name is computed once per command type.
    private static readonly string CommandName = typeof(TCommand).Name;
    private static readonly string MetricName = ToSnakeCase(CommandName);

    private readonly Counter<long> _counter;
    private readonly UpDownCounter<long> _inProgress;
    private readonly Histogram<double> _values;
    private readonly Histogram<double> _duration;

    public AppMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(AppTelemetry.MeterName);

        _counter = meter.CreateCounter<long>(
            MetricName, description: $"Number of processed {CommandName} commands.");

        _inProgress = meter.CreateUpDownCounter<long>(
            $"{MetricName}_in_progress", description: $"Number of {CommandName} commands currently being processed.");

        _values = meter.CreateHistogram<double>(
            $"{MetricName}_value", description: $"Values recorded while processing {CommandName} commands.");

        _duration = meter.CreateHistogram(
            $"{MetricName}_duration",
            unit: "s",
            description: $"Duration of {CommandName} command processing.",
            tags: null,
            advice: new InstrumentAdvice<double> { HistogramBucketBoundaries = DurationBuckets });
    }

    public string Name => MetricName;

    public void Increment(long value = 1, in TagList tags = default) => _counter.Add(value, tags);

    public void Add(long delta, in TagList tags = default) => _inProgress.Add(delta, tags);

    public void Record(double value, in TagList tags = default) => _values.Record(value, tags);

    public MetricTimer StartTimer(in TagList tags = default) => new(_duration, tags);

    public InProgressScope TrackInProgress(in TagList tags = default) => new(_inProgress, tags);

    private static string ToSnakeCase(string typeName)
    {
        // Generic type names look like "Command`1" — drop the arity suffix.
        var backtick = typeName.IndexOf('`');
        var name = backtick < 0 ? typeName : typeName[..backtick];

        return JsonNamingPolicy.SnakeCaseLower.ConvertName(name);
    }
}

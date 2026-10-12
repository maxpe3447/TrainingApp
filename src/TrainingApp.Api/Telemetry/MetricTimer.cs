using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace TrainingApp.Api.Telemetry;

public readonly struct MetricTimer : IDisposable
{
    private readonly Histogram<double> _histogram;
    private readonly TagList _tags;
    private readonly long _startedAt;

    internal MetricTimer(Histogram<double> histogram, in TagList tags)
    {
        _histogram = histogram;
        _tags = tags;
        _startedAt = Stopwatch.GetTimestamp();
    }

    public void Dispose() => _histogram?.Record(Stopwatch.GetElapsedTime(_startedAt).TotalSeconds, _tags);
}

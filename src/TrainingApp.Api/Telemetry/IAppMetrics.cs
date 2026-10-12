using System.Diagnostics;

namespace TrainingApp.Api.Telemetry;

/// <summary>
/// Metrics for processing of <typeparamref name="TCommand"/>.
/// Metric names are derived from the command type name in snake_case,
/// e.g. StandardDeliveryCommand → standard_delivery_command_*.
/// These are the same technical metrics for every command, recorded by MetricsBehavior;
/// business metrics of a feature are separate instruments, see ExpressDeliveryStrategy.
/// </summary>
public interface IAppMetrics<TCommand> where TCommand : notnull
{
    /// <summary>Base metric name, e.g. "standard_delivery_command".</summary>
    string Name { get; }

    /// <summary>Counter {name}_total: adds a non-negative value.</summary>
    void Increment(long value = 1, in TagList tags = default);

    /// <summary>Up-down counter {name}_in_progress: adds a positive or negative delta.</summary>
    void Add(long delta, in TagList tags = default);

    /// <summary>Histogram {name}_value: records an arbitrary value.</summary>
    void Record(double value, in TagList tags = default);

    /// <summary>Histogram {name}_duration_seconds: records the elapsed time when disposed.</summary>
    MetricTimer StartTimer(in TagList tags = default);

    /// <summary>Up-down counter {name}_in_progress: adds 1 now and subtracts 1 when disposed.</summary>
    InProgressScope TrackInProgress(in TagList tags = default);
}

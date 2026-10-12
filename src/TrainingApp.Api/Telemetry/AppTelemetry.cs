using System.Diagnostics;

namespace TrainingApp.Api.Telemetry;

/// <summary>
/// Names shared by every telemetry signal of the application.
/// </summary>
public static class AppTelemetry
{
    /// <summary>
    /// OpenTelemetry resource attribute service.name: every log record, metric and span carries it,
    /// Loki turns it into the service_name label.
    /// </summary>
    public const string ServiceName = "TrainingApp.Api";

    /// <summary>
    /// The application's meter. Program.cs exports only the meters it lists in AddMeter,
    /// so create your instruments with <c>meterFactory.Create(AppTelemetry.MeterName)</c>.
    /// </summary>
    public const string MeterName = "TrainingApp.Api";

    /// <summary>
    /// Name of <see cref="ActivitySource"/>. Program.cs exports only the sources it lists in AddSource.
    /// </summary>
    public const string ActivitySourceName = "TrainingApp.Api";

    /// <summary>
    /// The application's source of spans (in .NET a span is an <see cref="Activity"/>).
    /// ActivitySource has no factory like IMeterFactory: one static instance for the whole application.
    /// </summary>
    /// <remarks>
    /// <c>StartActivity</c> returns null when nobody listens (no tracer provider, or the span is not sampled),
    /// so always use the null-conditional operator: <c>activity?.SetTag(...)</c>.
    /// </remarks>
    public static readonly ActivitySource ActivitySource = new(ActivitySourceName);
}

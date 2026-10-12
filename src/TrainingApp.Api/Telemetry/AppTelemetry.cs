namespace TrainingApp.Api.Telemetry;

/// <summary>
/// Names shared by every telemetry signal of the application.
/// </summary>
public static class AppTelemetry
{
    /// <summary>
    /// OpenTelemetry resource attribute service.name: every log record (and later metric and span) carries it,
    /// Loki turns it into the service_name label.
    /// </summary>
    public const string ServiceName = "TrainingApp.Api";
}

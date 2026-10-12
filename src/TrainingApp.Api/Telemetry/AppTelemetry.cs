namespace TrainingApp.Api.Telemetry;

/// <summary>
/// Names shared by every telemetry signal of the application.
/// </summary>
public static class AppTelemetry
{
    /// <summary>
    /// OpenTelemetry resource attribute service.name: every log record and metric carries it,
    /// Loki turns it into the service_name label.
    /// </summary>
    public const string ServiceName = "TrainingApp.Api";

    /// <summary>
    /// The application's meter. Program.cs exports only the meters it lists in AddMeter,
    /// so create your instruments with <c>meterFactory.Create(AppTelemetry.MeterName)</c>.
    /// </summary>
    public const string MeterName = "TrainingApp.Api";
}

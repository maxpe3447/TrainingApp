using System.Text.Json.Serialization;
using FluentValidation;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Formatting.Compact;
using TrainingApp.Api.Behaviors;
using TrainingApp.Api.Common;
using TrainingApp.Api.Infrastructure;
using TrainingApp.Api.Telemetry;

var builder = WebApplication.CreateBuilder(args);

// Logging: two providers behind the same ILogger<T>.
//   Serilog       → console: readable text in Development, one JSON object per line elsewhere;
//   OpenTelemetry → OTLP → otel-collector → Loki (see monitoring/); each record carries the trace_id of the current span.
// Levels per provider come from appsettings: Logging:Serilog:LogLevel and Logging:LogLevel.
builder.Logging.ClearProviders();
var consoleLogger = new LoggerConfiguration().MinimumLevel.Verbose();
consoleLogger = builder.Environment.IsDevelopment()
    ? consoleLogger.WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj} {TraceId}{NewLine}{Exception}")
    // One JSON object per line; @tr/@sp carry trace_id/span_id.
    : consoleLogger.WriteTo.Console(new CompactJsonFormatter());
builder.Logging.AddSerilog(consoleLogger.CreateLogger(), dispose: true);

// Technical metrics of every command: IAppMetrics<TCommand> names them after the command type in snake_case.
builder.Services.AddSingleton(typeof(IAppMetrics<>), typeof(AppMetrics<>));

// The OTLP endpoint is read from OTEL_EXPORTER_OTLP_ENDPOINT (appsettings.json or an environment variable).
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(AppTelemetry.ServiceName)
        .AddAttributes([new("deployment.environment.name", builder.Environment.EnvironmentName)]))
    // Metrics are pulled: Prometheus scrapes GET /metrics (see monitoring/prometheus/prometheus.yml).
    .WithMetrics(metrics => metrics
        .AddMeter(AppTelemetry.MeterName)
        .AddMeter("Microsoft.AspNetCore.Hosting", "Microsoft.AspNetCore.Server.Kestrel", "System.Runtime")
        // Exemplars: a histogram measurement recorded inside a sampled span keeps its trace_id,
        // so a point on a latency graph opens the very trace that produced it.
        .SetExemplarFilter(ExemplarFilterType.TraceBased)
        .AddPrometheusExporter())
    // Traces are pushed: OTLP → otel-collector → Tempo.
    .WithTracing(tracing => tracing
        .AddSource(AppTelemetry.ActivitySourceName)
        // The root span of every HTTP request; it also continues a trace from an incoming traceparent header.
        .AddAspNetCoreInstrumentation(options =>
            options.Filter = context => !context.Request.Path.StartsWithSegments("/metrics"))
        .AddOtlpExporter())
    .WithLogging(
        logging => logging.AddOtlpExporter(),
        options =>
        {
            // Loki shows the rendered message as the log line; the template arguments stay as attributes.
            options.IncludeFormattedMessage = true;
            options.IncludeScopes = true;
        });

// Controllers are discovered in the whole assembly, so a controller in a feature folder works too.
builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddOpenApi();

builder.Services.AddSingleton(TimeProvider.System);

// Strategies: every IDeliveryStrategy<TCommand> in this assembly; the command type uniquely determines the implementation.
builder.Services.AddDeliveryStrategies(typeof(Program).Assembly);

// Handlers and validators are discovered in this assembly as well: a new feature needs no registration here.
// Behaviors run in the order they are added: tracing comes first, so the logs and metrics of the others
// are recorded inside the command span; all of them wrap validation, so rejected commands are seen too.
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining<Program>();
    cfg.AddOpenBehavior(typeof(TracingBehavior<,>));
    cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
    cfg.AddOpenBehavior(typeof(MetricsBehavior<,>));
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
});
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ValidationExceptionHandler>();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.DisableAgent();
        options.DisableMcp();
    });
}

app.MapPrometheusScrapingEndpoint();

app.MapControllers();

app.Run();

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Time.Testing;

namespace TrainingApp.IntegrationTests;

/// <summary>
/// The application, started in memory with its real Program.cs. Only the boundaries are replaced:
///   TimeProvider → FakeTimeProvider: "today" is fixed, so arrival dates are deterministic;
///   logging      → FakeLogger as an extra provider: tests can read every log record.
/// </summary>
public sealed class TrainingAppFactory : WebApplicationFactory<Program>
{
    /// <summary>Monday, 2 March 2026, 09:00 UTC.</summary>
    public static readonly DateOnly Today = new(2026, 3, 2);

    public TrainingAppFactory()
    {
        Time.SetLocalTimeZone(TimeZoneInfo.Utc);
    }

    public FakeTimeProvider Time { get; } = new(new DateTimeOffset(2026, 3, 2, 9, 0, 0, TimeSpan.Zero));

    public FakeLogCollector Logs => Services.GetFakeLogCollector();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureLogging(logging => logging.AddFakeLogging());

        // ConfigureTestServices runs after Program.cs registered its services, so it can replace them.
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Time);
        });
    }
}

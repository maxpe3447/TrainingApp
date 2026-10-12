using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

namespace TrainingApp.Checks;

/// <summary>
/// Starts TrainingApp.Api in memory (Development environment): checks call it over HTTP and inspect its DI container.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    /// <summary>Every log record the application wrote, in memory.</summary>
    public FakeLogCollector Logs => Services.GetFakeLogCollector();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureLogging(logging => logging
            .AddFakeLogging()
            // A provider-specific rule wins over the general ones: capture every level, whatever appsettings says.
            .AddFilter<FakeLoggerProvider>(null, LogLevel.Trace));
    }
}

using Microsoft.AspNetCore.Mvc.Testing;

namespace TrainingApp.Checks;

/// <summary>
/// Starts TrainingApp.Api in memory (Development environment): checks call it over HTTP and inspect its DI container.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

namespace TrainingApp.Checks;

/// <summary>Task 02: structured logs of your feature. See tasks/02-logging.md.</summary>
[Trait("Task", "02")]
public sealed class Task02LoggingChecks(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Feature_writes_logs_while_quoting()
    {
        var feature = StudentFeature.Find();

        var records = await QuoteAndCollectFeatureLogsAsync(feature);

        records.Should().NotBeEmpty(
            "inject ILogger<T> into your strategy or handler and log the decisions it makes; " +
            "logs are expected from categories in {0}, see PickupDeliveryStrategy for an example",
            feature.Namespace);
    }

    [Fact]
    public async Task Feature_logs_are_structured()
    {
        var feature = StudentFeature.Find();

        var records = await QuoteAndCollectFeatureLogsAsync(feature);

        records.Should().Contain(record => HasTemplateArguments(record),
            "use message templates with placeholders, e.g. logger.LogInformation(\"Parcel of {WeightKg} kg\", weightKg), " +
            "not string interpolation: placeholders become attributes you can filter on in Loki");
    }

    [Fact]
    public async Task Successful_quote_logs_no_warnings_or_errors()
    {
        var feature = StudentFeature.Find();

        var records = await QuoteAndCollectFeatureLogsAsync(feature);

        records.Should().NotContain(record => record.Level >= LogLevel.Warning,
            "MyFeature.ValidRequest is a normal, successful request: use Debug or Information for it, " +
            "keep Warning and Error for situations someone has to look at");
    }

    private async Task<IReadOnlyList<FakeLogRecord>> QuoteAndCollectFeatureLogsAsync(StudentFeature feature)
    {
        using var client = factory.CreateClient();
        factory.Logs.Clear();

        await StudentFeature.PostValidRequestAsync(client, TestContext.Current.CancellationToken);

        return factory.Logs.GetSnapshot()
            .Where(record => record.Category?.StartsWith(feature.Namespace + ".", StringComparison.Ordinal) == true)
            .ToList();
    }

    // Template arguments are the structured state entries other than the template itself.
    private static bool HasTemplateArguments(FakeLogRecord record) =>
        record.StructuredState?.Any(entry => entry.Key != "{OriginalFormat}") == true;
}

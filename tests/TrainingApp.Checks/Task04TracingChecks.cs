using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Http.Json;
using TrainingApp.Api.Telemetry;

namespace TrainingApp.Checks;

/// <summary>Task 04: spans of your feature. See tasks/04-tracing.md.</summary>
[Trait("Task", "04")]
public sealed class Task04TracingChecks(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Feature_creates_spans_of_its_own()
    {
        var feature = StudentFeature.Find();

        var spans = await QuoteAndCollectFeatureSpansAsync(feature);

        spans.Should().NotBeEmpty(
            "start at least one span of your own while quoting MyFeature.ValidRequest: " +
            "using var activity = AppTelemetry.ActivitySource.StartActivity(\"{0}.<step>\"); see StandardDeliveryHandler. " +
            "Start it inside your handler or strategy, so it joins the trace of the request. " +
            "The spans '{0}', '{0}.calculate_cost' and '{0}.estimate_arrival' come from the course code and do not count",
            feature.CommandType.Name);
    }

    [Fact]
    public async Task Feature_spans_have_attributes()
    {
        var feature = StudentFeature.Find();

        var spans = await QuoteAndCollectFeatureSpansAsync(feature);

        spans.Should().Contain(span => span.TagObjects.Any(),
            "describe the work of your span with attributes: activity?.SetTag(\"parcel.weight_kg\", ...)");
    }

    [Fact]
    public async Task Rejected_request_is_not_an_error()
    {
        MyFeature.Route.Should().NotBeNullOrWhiteSpace("fill in Route in tests/TrainingApp.Checks/MyFeature.cs");
        MyFeature.InvalidRequest.Should().NotBeNull("fill in InvalidRequest in tests/TrainingApp.Checks/MyFeature.cs");

        var spans = await CollectSpansAsync(client => client.PostAsJsonAsync(
            MyFeature.Route, MyFeature.InvalidRequest, StudentFeature.JsonOptions, TestContext.Current.CancellationToken));

        spans.Should().NotContain(span => span.Status == ActivityStatusCode.Error,
            "a request rejected by validation is the caller's mistake, not a failure of the service: " +
            "leave the span status unset and use an attribute or an event to record the refusal");
    }

    private async Task<IReadOnlyList<Activity>> QuoteAndCollectFeatureSpansAsync(StudentFeature feature)
    {
        var courseSpans = new[]
        {
            feature.CommandType.Name,
            $"{feature.CommandType.Name}.calculate_cost",
            $"{feature.CommandType.Name}.estimate_arrival"
        };

        var spans = await CollectSpansAsync(client =>
            StudentFeature.PostValidRequestAsync(client, TestContext.Current.CancellationToken));

        return spans
            .Where(span => span.Source.Name == AppTelemetry.ActivitySourceName && !courseSpans.Contains(span.OperationName))
            .ToList();
    }

    /// <summary>
    /// Sends a request with a known trace id in the traceparent header and returns every span of that trace.
    /// </summary>
    private async Task<IReadOnlyList<Activity>> CollectSpansAsync(Func<HttpClient, Task> send)
    {
        var traceId = ActivityTraceId.CreateRandom();
        var spans = new ConcurrentQueue<Activity>();

        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name is AppTelemetry.ActivitySourceName or "Microsoft.AspNetCore",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity =>
            {
                if (activity.TraceId == traceId)
                {
                    spans.Enqueue(activity);
                }
            }
        };
        ActivitySource.AddActivityListener(listener);

        using var client = factory.CreateClient();
        // W3C trace context: version-traceid-parentspanid-flags. The request span continues this trace.
        client.DefaultRequestHeaders.Add("traceparent", $"00-{traceId}-{ActivitySpanId.CreateRandom()}-01");

        await send(client);

        return spans.ToList();
    }
}

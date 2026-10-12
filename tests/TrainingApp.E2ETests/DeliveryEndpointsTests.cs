using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace TrainingApp.E2ETests;

/// <summary>
/// The API as its clients see it: a container, an HTTP port, JSON in and out.
/// The clock is real here, so dates are checked loosely; exact values belong to unit and integration tests.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class DeliveryEndpointsTests(ApiContainerFixture api)
{
    [Fact]
    public async Task PostStandard_ReturnsQuoteAsJson()
    {
        using var response = await api.Client.PostAsJsonAsync(
            "/api/delivery/standard", DeliveryRequests.Standard, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");

        // Assert the JSON contract itself: property names and value formats are what clients depend on.
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        var root = json.RootElement;
        root.GetProperty("type").GetString().Should().Be("Standard", "enums are serialized as strings");
        root.GetProperty("cost").GetDecimal().Should().Be(62.5m);
        root.GetProperty("description").GetString().Should().NotBeNullOrEmpty();
        DateOnly.Parse(root.GetProperty("estimatedArrival").GetString()!)
            .Should().BeOnOrAfter(DateOnly.FromDateTime(DateTime.UtcNow).AddDays(3));
    }

    [Fact]
    public async Task PostExpress_TooHeavy_ReturnsProblemDetailsWithReason()
    {
        using var response = await api.Client.PostAsJsonAsync(
            "/api/delivery/express", DeliveryRequests.ExpressTooHeavy, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))
            .Should().Contain("Express delivery accepts parcels up to 10 kg and 1000 km.");
    }

    // DisableDiscoveryEnumeration: the rows hold anonymous objects, which cannot be serialized for test discovery.
    [Theory]
    [MemberData(nameof(DeliveryRequests.Invalid), MemberType = typeof(DeliveryRequests), DisableDiscoveryEnumeration = true)]
    public async Task Post_InvalidRequest_ReturnsBadRequest(string scenario, string route, object request)
    {
        using var response = await api.Client.PostAsJsonAsync(route, request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, scenario);
    }

    [Fact]
    public async Task GetMetrics_AfterRequest_ExposesCommandCounter()
    {
        (await api.Client.PostAsJsonAsync(
            "/api/delivery/standard", DeliveryRequests.Standard, TestContext.Current.CancellationToken)).Dispose();

        var metrics = await api.Client.GetStringAsync("/metrics", TestContext.Current.CancellationToken);

        metrics.Should().Contain("standard_delivery_command_total{", "Prometheus scrapes this endpoint in production");
    }

    [Fact]
    public async Task GetOpenApi_InProduction_IsNotExposed()
    {
        using var response = await api.Client.GetAsync("/openapi/v1.json", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound, "API documentation is mapped in Development only");
    }
}

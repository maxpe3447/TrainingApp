using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using TrainingApp.Api.Common;

namespace TrainingApp.IntegrationTests;

/// <summary>
/// HTTP requests to the in-memory server: routing, model binding, JSON and the exception handler are included.
/// Unlike the e2e tests, the clock is fake here, so even the arrival date can be asserted exactly.
/// </summary>
[Collection(IntegrationCollection.Name)]
public sealed class DeliveryApiTests(TrainingAppFactory factory)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    public async Task PostExpress_ValidParcel_ReturnsQuoteForTomorrow()
    {
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            "/api/delivery/express",
            new { weightKg = 2.5, distanceKm = 300, isFragile = true, declaredValue = 1500 },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var quote = await response.Content.ReadFromJsonAsync<DeliveryQuote>(Json, TestContext.Current.CancellationToken);
        quote.Should().BeEquivalentTo(new
        {
            Type = DeliveryType.Express,
            Cost = 105m,
            EstimatedArrival = TrainingAppFactory.Today.AddDays(1)
        });
    }

    [Fact]
    public async Task PostPickup_UnknownPickupPoint_ReturnsValidationProblem()
    {
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            "/api/delivery/pickup",
            new { pickupPointId = "ODESA-99", weightKg = 3 },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(TestContext.Current.CancellationToken);
        problem!.Errors.Values.SelectMany(messages => messages).Should().ContainMatch("*ODESA-99*");
    }
}

using System.Net;
using System.Net.Http.Json;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using TrainingApp.Api.Common;

namespace TrainingApp.Checks;

/// <summary>Task 01: your own delivery strategy and its endpoint. See tasks/01-strategy.md.</summary>
[Trait("Task", "01")]
public sealed class Task01StrategyChecks(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public void Strategy_exists()
    {
        StudentFeature.Find();
    }

    [Fact]
    public void Strategy_has_its_own_delivery_type()
    {
        var feature = StudentFeature.Find();

        var type = feature.GetDeliveryType(factory.Services);

        type.Should().NotBe(DeliveryType.Standard, "append a new value to DeliveryType for your feature")
            .And.NotBe(DeliveryType.Express, "append a new value to DeliveryType for your feature")
            .And.NotBe(DeliveryType.Pickup, "append a new value to DeliveryType for your feature");
    }

    [Fact]
    public void Command_has_a_validator()
    {
        var feature = StudentFeature.Find();
        using var scope = factory.Services.CreateScope();

        var validators = scope.ServiceProvider.GetServices(typeof(IValidator<>).MakeGenericType(feature.CommandType));

        validators.Should().NotBeEmpty("add an AbstractValidator<{0}> to your feature folder", feature.CommandType.Name);
    }

    [Fact]
    public void Command_has_a_handler()
    {
        var feature = StudentFeature.Find();
        using var scope = factory.Services.CreateScope();

        var handlerType = typeof(IRequestHandler<,>).MakeGenericType(feature.CommandType, typeof(DeliveryQuote));
        var handler = scope.ServiceProvider.GetService(handlerType);

        handler.Should().NotBeNull(
            "add a handler for {0}: derive it from DeliveryQuoteHandler<{0}> like ExpressDeliveryHandler does",
            feature.CommandType.Name);
    }

    [Fact]
    public async Task Endpoint_returns_a_quote_of_your_delivery_type()
    {
        var feature = StudentFeature.Find();
        using var client = factory.CreateClient();

        var quote = await StudentFeature.PostValidRequestAsync(client, TestContext.Current.CancellationToken);

        quote.Type.Should().Be(feature.GetDeliveryType(factory.Services), "your endpoint must be served by your strategy");
        quote.Description.Should().NotBeNullOrWhiteSpace("give your strategy a Description");
        quote.Cost.Should().BeGreaterThanOrEqualTo(0, "a delivery cannot cost less than nothing");
        quote.EstimatedArrival.Should().BeOnOrAfter(DateOnly.FromDateTime(DateTime.Now.AddDays(-1)),
            "a parcel cannot arrive before it is shipped");
    }

    [Fact]
    public async Task Endpoint_rejects_an_invalid_request()
    {
        MyFeature.Route.Should().NotBeNullOrWhiteSpace("fill in Route in tests/TrainingApp.Checks/MyFeature.cs");
        MyFeature.InvalidRequest.Should().NotBeNull("fill in InvalidRequest in tests/TrainingApp.Checks/MyFeature.cs");
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            MyFeature.Route, MyFeature.InvalidRequest, StudentFeature.JsonOptions, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "your validator must reject MyFeature.InvalidRequest; the ValidationBehavior turns that into 400");
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json",
            "validation errors are returned as ProblemDetails by ValidationExceptionHandler");
    }
}

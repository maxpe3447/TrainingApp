using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TrainingApp.Api.Common;
using TrainingApp.Api.Features.ExpressDelivery;
using TrainingApp.Api.Features.StandardDelivery;

namespace TrainingApp.IntegrationTests;

/// <summary>
/// Commands sent through the real MediatR pipeline: behaviors, validators, DI-resolved strategies and handlers together.
/// No mocks: this checks that the pieces unit tests covered one by one are wired up correctly.
/// </summary>
[Collection(IntegrationCollection.Name)]
public sealed class DeliveryPipelineTests(TrainingAppFactory factory)
{
    [Fact]
    public async Task Send_StandardCommand_IsQuotedByStandardStrategy()
    {
        using var scope = factory.Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var quote = await sender.Send(new StandardDeliveryCommand(5, 800, IsFragile: true), TestContext.Current.CancellationToken);

        quote.Should().BeEquivalentTo(new DeliveryQuote(
            DeliveryType.Standard,
            "Standard courier delivery",
            Cost: 62.5m, // 5 + 5 × 1.5 + 800 × 0.05 + 10 fragile
            EstimatedArrival: TrainingAppFactory.Today.AddDays(4)));
    }

    [Fact]
    public async Task Send_InvalidCommand_IsRejectedByValidationBehavior()
    {
        using var scope = factory.Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var act = () => sender.Send(new StandardDeliveryCommand(-1, 0, false), TestContext.Current.CancellationToken);

        (await act.Should().ThrowAsync<ValidationException>())
            .Which.Errors.Select(e => e.PropertyName).Should().Contain(["WeightKg", "DistanceKm"]);
    }

    [Fact]
    public async Task Send_RejectedCommand_IsLoggedAsWarningByLoggingBehavior()
    {
        using var scope = factory.Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        factory.Logs.Clear();

        var act = () => sender.Send(new ExpressDeliveryCommand(12, 300, false, 0), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>();
        factory.Logs.GetSnapshot().Should().Contain(record =>
            record.Level == LogLevel.Warning
            && record.Message.StartsWith("ExpressDeliveryCommand rejected", StringComparison.Ordinal));
    }

    [Fact]
    public void Container_ResolvesOneStrategyPerCommand()
    {
        // A wiring test: AddDeliveryStrategies registered the strategies the handlers depend on.
        factory.Services.GetService<IDeliveryStrategy<StandardDeliveryCommand>>().Should().BeOfType<StandardDeliveryStrategy>();
        factory.Services.GetService<IDeliveryStrategy<ExpressDeliveryCommand>>().Should().BeOfType<ExpressDeliveryStrategy>();
    }
}

using FluentValidation;
using FluentValidation.Results;
using MediatR;
using TrainingApp.Api.Behaviors;
using TrainingApp.Api.Common;
using TrainingApp.Api.Features.StandardDelivery;

namespace TrainingApp.UnitTests.Behaviors;

/// <summary>
/// A pipeline behavior has two collaborators: the validators and "next" (the rest of the pipeline).
/// Both are mocked, so the test sees exactly whether next was called.
/// </summary>
public sealed class ValidationBehaviorTests
{
    private static readonly StandardDeliveryCommand Command = new(1, 10, false);
    private static readonly DeliveryQuote Quote = new(DeliveryType.Standard, "Test", 1m, new DateOnly(2026, 3, 5));

    private readonly Mock<RequestHandlerDelegate<DeliveryQuote>> _next = new();

    public ValidationBehaviorTests()
    {
        _next.Setup(next => next(It.IsAny<CancellationToken>())).ReturnsAsync(Quote);
    }

    [Fact]
    public async Task Handle_NoValidators_CallsNext()
    {
        var behavior = new ValidationBehavior<StandardDeliveryCommand, DeliveryQuote>([]);

        var response = await behavior.Handle(Command, _next.Object, TestContext.Current.CancellationToken);

        response.Should().BeSameAs(Quote);
        _next.Verify(next => next(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ValidCommand_CallsNext()
    {
        var validator = MockValidator(new ValidationResult());
        var behavior = new ValidationBehavior<StandardDeliveryCommand, DeliveryQuote>([validator.Object]);

        var response = await behavior.Handle(Command, _next.Object, TestContext.Current.CancellationToken);

        response.Should().BeSameAs(Quote);
        validator.Verify(v => v.ValidateAsync(It.IsAny<IValidationContext>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_InvalidCommand_ThrowsAllFailuresAndDoesNotCallNext()
    {
        var first = MockValidator(new ValidationResult([new ValidationFailure("WeightKg", "Too light")]));
        var second = MockValidator(new ValidationResult([new ValidationFailure("DistanceKm", "Too short")]));
        var behavior = new ValidationBehavior<StandardDeliveryCommand, DeliveryQuote>([first.Object, second.Object]);

        var act = () => behavior.Handle(Command, _next.Object, TestContext.Current.CancellationToken);

        // Failures of every validator are collected into one exception.
        (await act.Should().ThrowAsync<ValidationException>())
            .Which.Errors.Select(e => e.PropertyName).Should().BeEquivalentTo("WeightKg", "DistanceKm");
        _next.Verify(next => next(It.IsAny<CancellationToken>()), Times.Never);
    }

    private static Mock<IValidator<StandardDeliveryCommand>> MockValidator(ValidationResult result)
    {
        var validator = new Mock<IValidator<StandardDeliveryCommand>>();
        // ValidationBehavior calls ValidateAsync(IValidationContext, ...): set up that overload.
        validator
            .Setup(v => v.ValidateAsync(It.IsAny<IValidationContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);
        return validator;
    }
}

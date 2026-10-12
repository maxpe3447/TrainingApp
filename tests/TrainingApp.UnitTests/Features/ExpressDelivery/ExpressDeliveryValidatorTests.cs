using FluentValidation.TestHelper;
using TrainingApp.Api.Common;
using TrainingApp.Api.Features.ExpressDelivery;

namespace TrainingApp.UnitTests.Features.ExpressDelivery;

/// <summary>
/// The validator depends on the strategy's CanDeliver. Mocking it separates two questions:
/// "does the validator check the fields?" and "does it ask the strategy?" — the strategy's own rules are not tested here.
/// FluentValidation.TestHelper adds TestValidate and ShouldHave… assertions.
/// </summary>
public sealed class ExpressDeliveryValidatorTests
{
    private readonly Mock<IDeliveryStrategy<ExpressDeliveryCommand>> _strategy = new();

    private ExpressDeliveryValidator CreateValidator(bool canDeliver)
    {
        _strategy.Setup(s => s.CanDeliver(It.IsAny<ExpressDeliveryCommand>())).Returns(canDeliver);
        return new ExpressDeliveryValidator(_strategy.Object);
    }

    [Fact]
    public void Validate_ValidCommandThatStrategyAccepts_HasNoErrors()
    {
        var validator = CreateValidator(canDeliver: true);

        var result = validator.TestValidate(new ExpressDeliveryCommand(2, 100, false, 500));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_StrategyRefuses_ReportsBusinessRule()
    {
        var validator = CreateValidator(canDeliver: false);

        var result = validator.TestValidate(new ExpressDeliveryCommand(2, 100, false, 500));

        // TestHelper assertions work per property; for a whole-object rule assert on the errors themselves.
        result.Errors.Should().ContainSingle()
            .Which.ErrorMessage.Should().Be("Express delivery accepts parcels up to 10 kg and 1000 km.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveWeight_ReportsWeightAndSkipsStrategy(decimal weightKg)
    {
        var validator = CreateValidator(canDeliver: true);

        var result = validator.TestValidate(new ExpressDeliveryCommand(weightKg, 100, false, 0));

        result.ShouldHaveValidationErrorFor(c => c.WeightKg);
        // The business rule only runs for well-formed input (the .When(...) clause).
        _strategy.Verify(s => s.CanDeliver(It.IsAny<ExpressDeliveryCommand>()), Times.Never);
    }

    [Fact]
    public void Validate_DeclaredValueOverLimit_ReportsDeclaredValue()
    {
        var validator = CreateValidator(canDeliver: true);

        var result = validator.TestValidate(new ExpressDeliveryCommand(2, 100, false, 100_001));

        result.ShouldHaveValidationErrorFor(c => c.DeclaredValue);
    }
}

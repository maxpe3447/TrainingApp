using FluentValidation;
using TrainingApp.Api.Common;

namespace TrainingApp.Api.Features.StandardDelivery;

public sealed class StandardDeliveryValidator : AbstractValidator<StandardDeliveryCommand>
{
    public StandardDeliveryValidator(IDeliveryStrategy<StandardDeliveryCommand> strategy)
    {
        RuleFor(x => x.WeightKg).GreaterThan(0);
        RuleFor(x => x.DistanceKm).GreaterThan(0).LessThanOrEqualTo(10_000);

        RuleFor(x => x)
            .Must(strategy.CanDeliver)
            .WithName("Parcel")
            .WithMessage("Standard delivery accepts parcels up to 30 kg.")
            .When(x => x.WeightKg > 0);
    }
}

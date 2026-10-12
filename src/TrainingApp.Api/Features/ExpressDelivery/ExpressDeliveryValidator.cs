using FluentValidation;
using TrainingApp.Api.Common;

namespace TrainingApp.Api.Features.ExpressDelivery;

public sealed class ExpressDeliveryValidator : AbstractValidator<ExpressDeliveryCommand>
{
    public ExpressDeliveryValidator(IDeliveryStrategy<ExpressDeliveryCommand> strategy)
    {
        RuleFor(x => x.WeightKg).GreaterThan(0);
        RuleFor(x => x.DistanceKm).GreaterThan(0);
        RuleFor(x => x.DeclaredValue).GreaterThanOrEqualTo(0).LessThanOrEqualTo(100_000);

        RuleFor(x => x)
            .Must(strategy.CanDeliver)
            .WithName("Parcel")
            .WithMessage("Express delivery accepts parcels up to 10 kg and 1000 km.")
            .When(x => x.WeightKg > 0 && x.DistanceKm > 0);
    }
}

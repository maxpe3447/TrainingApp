using FluentValidation;
using TrainingApp.Api.Common;

namespace TrainingApp.Api.Features.PickupDelivery;

public sealed class PickupDeliveryValidator : AbstractValidator<PickupDeliveryCommand>
{
    public PickupDeliveryValidator(IDeliveryStrategy<PickupDeliveryCommand> strategy)
    {
        RuleFor(x => x.PickupPointId).NotEmpty();
        RuleFor(x => x.WeightKg).GreaterThan(0);

        RuleFor(x => x)
            .Must(strategy.CanDeliver)
            .WithName("Parcel")
            .WithMessage(x => $"Pickup point '{x.PickupPointId}' does not exist or the parcel is heavier than 15 kg.")
            .When(x => !string.IsNullOrEmpty(x.PickupPointId) && x.WeightKg > 0);
    }
}

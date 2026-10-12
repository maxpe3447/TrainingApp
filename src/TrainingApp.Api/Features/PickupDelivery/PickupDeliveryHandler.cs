using TrainingApp.Api.Common;

namespace TrainingApp.Api.Features.PickupDelivery;

public sealed class PickupDeliveryHandler(
    IDeliveryStrategy<PickupDeliveryCommand> strategy,
    TimeProvider timeProvider) : DeliveryQuoteHandler<PickupDeliveryCommand>(strategy, timeProvider);

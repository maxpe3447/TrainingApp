using TrainingApp.Api.Common;

namespace TrainingApp.Api.Features.ExpressDelivery;

public sealed class ExpressDeliveryHandler(
    IDeliveryStrategy<ExpressDeliveryCommand> strategy,
    TimeProvider timeProvider) : DeliveryQuoteHandler<ExpressDeliveryCommand>(strategy, timeProvider);

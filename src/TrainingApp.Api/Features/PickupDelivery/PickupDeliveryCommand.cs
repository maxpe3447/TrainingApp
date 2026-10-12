using TrainingApp.Api.Common;

namespace TrainingApp.Api.Features.PickupDelivery;

public sealed record PickupDeliveryCommand(
    string PickupPointId,
    decimal WeightKg) : IDeliveryCommand;

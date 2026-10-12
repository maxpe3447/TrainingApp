using TrainingApp.Api.Common;

namespace TrainingApp.Api.Features.ExpressDelivery;

public sealed record ExpressDeliveryCommand(
    decimal WeightKg,
    int DistanceKm,
    bool IsFragile,
    decimal DeclaredValue) : IDeliveryCommand;

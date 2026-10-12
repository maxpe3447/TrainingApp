using TrainingApp.Api.Common;

namespace TrainingApp.Api.Features.StandardDelivery;

public sealed record StandardDeliveryCommand(
    decimal WeightKg,
    int DistanceKm,
    bool IsFragile) : IDeliveryCommand;

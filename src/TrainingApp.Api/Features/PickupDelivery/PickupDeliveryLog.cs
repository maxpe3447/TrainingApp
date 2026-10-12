namespace TrainingApp.Api.Features.PickupDelivery;

/// <summary>
/// Log messages of the pickup feature, generated at compile time by [LoggerMessage]:
/// the template is parsed once, there is no boxing of arguments, and nothing is formatted when the level is disabled.
/// </summary>
internal static partial class PickupDeliveryLog
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "Pickup point {PickupPointId} does not exist")]
    public static partial void UnknownPickupPoint(this ILogger logger, string pickupPointId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Parcel of {WeightKg} kg exceeds the {MaxWeightKg} kg limit of pickup points")]
    public static partial void ParcelTooHeavy(this ILogger logger, decimal weightKg, decimal maxWeightKg);

    [LoggerMessage(Level = LogLevel.Information, Message = "Parcel will be ready at {PickupPointId} in {ReadyInDays} day(s)")]
    public static partial void ParcelReadyIn(this ILogger logger, string pickupPointId, int readyInDays);
}

namespace TrainingApp.Api.Common;

public sealed record DeliveryQuote(
    DeliveryType Type,
    string Description,
    decimal Cost,
    DateOnly EstimatedArrival);

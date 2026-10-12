namespace TrainingApp.E2ETests;

/// <summary>
/// Test data shared by e2e tests. Requests are anonymous objects on purpose: the e2e tests know only the
/// JSON contract of the API, not its C# types — just like an external client.
/// </summary>
public static class DeliveryRequests
{
    public static object Standard => new { weightKg = 5, distanceKm = 800, isFragile = true };

    public static object ExpressTooHeavy => new { weightKg = 12, distanceKm = 300, isFragile = false, declaredValue = 0 };

    /// <summary>Rows for [MemberData]: every request here must be answered with 400.</summary>
    public static TheoryData<string, string, object> Invalid => new()
    {
        { "negative weight", "/api/delivery/standard", new { weightKg = -1, distanceKm = 100, isFragile = false } },
        { "zero distance", "/api/delivery/standard", new { weightKg = 1, distanceKm = 0, isFragile = false } },
        { "express too far", "/api/delivery/express", new { weightKg = 1, distanceKm = 5000, isFragile = false, declaredValue = 0 } },
        { "unknown pickup point", "/api/delivery/pickup", new { pickupPointId = "ODESA-99", weightKg = 3 } },
        { "empty pickup point", "/api/delivery/pickup", new { pickupPointId = "", weightKg = 3 } }
    };
}

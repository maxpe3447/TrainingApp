namespace TrainingApp.Checks;

/// <summary>
/// Tells the checks how to call YOUR endpoint. Fill it in when you finish task 01.
/// This file belongs to you: the course branches never change it, so merging the next step will not conflict.
/// </summary>
/// <example>
/// <code>
/// public static string? Route => "/api/delivery/same-day";
///
/// public static object? ValidRequest => new { weightKg = 2.5, distanceKm = 15 };
///
/// public static object? InvalidRequest => new { weightKg = -1, distanceKm = 15 };
/// </code>
/// </example>
public static class MyFeature
{
    /// <summary>Route of your endpoint, e.g. "/api/delivery/same-day".</summary>
    public static string? Route => null;

    /// <summary>A request body your endpoint accepts: it must answer 200 with a quote of your delivery type.</summary>
    public static object? ValidRequest => null;

    /// <summary>A request body your validator rejects: it must answer 400.</summary>
    public static object? InvalidRequest => null;
}

/// <summary>
/// Canonical analytics event names. Keep the live set tight.
/// Dictionary lock: names here must match docs/analytics-events.md.
/// Session, install, and platform events come from the analytics SDK and are not listed here.
/// </summary>
public static class AnalyticsEvents
{
    // Gameplay
    public const string LevelStarted = "level_started";
    public const string LevelCompleted = "level_completed";
    public const string LevelFailed = "level_failed";

    // Economy
    public const string CoinsSpent = "coins_spent";

    // Monetization: mapped onto the SDK standard ad impression event by the transport.
    public const string AdImpression = "adImpression";
}

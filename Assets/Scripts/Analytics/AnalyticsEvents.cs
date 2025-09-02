public static class AnalyticsEvents
{
    // Gameplay
    public const string LevelStarted = "level_started";
    public const string LevelCompleted = "level_completed";
    public const string LevelFailed = "level_failed";
    public const string TileMatched = "tile_matched";
    public const string BoosterUsed = "booster_used";
    public const string MatchDuration = "match_duration";

    // Economy
    public const string CoinsEarned = "coins_earned";
    public const string CoinsSpent = "coins_spent";
    public const string IAPPurchased = "iap_purchased";

    // Monetization
    public const string AdWatched = "ad_watched";
    public const string AdSkipped = "ad_skipped";

    // Engagement
    public const string SessionStarted = "session_started";
    public const string SessionEnded = "session_ended";
}
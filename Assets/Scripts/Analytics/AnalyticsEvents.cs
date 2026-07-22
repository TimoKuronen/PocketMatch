public static class AnalyticsEvents
{
    // Engagement
    public const string AppStarted = "app_started";
    public const string SessionStarted = "session_started";
    public const string SessionEnded = "session_ended";

    // Gameplay
    public const string LevelStarted = "level_started";
    public const string LevelCompleted = "level_completed";
    public const string LevelFailed = "level_failed";
    public const string ExtraAutomatedMatches = "extra_automated_matches";

    // Economy
    public const string CoinsEarned = "coins_earned";
    // Reserved until a coin sink exists (retry fee or booster shop).
    public const string CoinsSpent = "coins_spent";

    // Monetization
    public const string AdWatched = "ad_watched";
    public const string AdSkipped = "ad_skipped";

    // Reserved: high volume if fired per match; prefer aggregate metrics later.
    public const string TileMatched = "tile_matched";
    // Reserved until inventory boosters exist (power tiles from matches are not boosters).
    public const string BoosterUsed = "booster_used";
    // Reserved: IAP is Won't for this portfolio build.
    public const string IAPPurchased = "iap_purchased";
}
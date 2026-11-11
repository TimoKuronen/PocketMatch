# Analytics Events

Reference for analytics event names and parameters raised by PocketMatch.
Event names are defined in `Assets/Scripts/Analytics/AnalyticsEvents.cs` and go through `IAnalyticsService`.

**Transport:** `AnalyticsService` delivers events through `IAnalyticsTransport`, with a local queue in `analytics_cache.json` until the transport is ready. The registered `NullAnalyticsTransport` is disabled: events are logged in the Editor and development builds, and nothing is stored or leaves the device.

**Dictionary lock:** the names below match the live constants in `AnalyticsEvents`; no other names are fired.

---

## Events (11)

| Event name | Parameters | When fired | Source |
|------------|------------|------------|--------|
| `app_started` | (none) | Analytics service starts, once per process | `AnalyticsService` |
| `session_started` | `device` (string), `app_version` (string) | Analytics service starts | `AnalyticsService` |
| `session_ended` | `device` (string), `app_version` (string) | Bootstrap scope is disposed (app quit) | `AnalyticsService` |
| `level_started` | `level_name` (string, `Level_NNNN`), `level_index` (int) | Board initialized and level is ready to play | `AnalyticsService` via `LevelEvents.OnLevelStarted` |
| `level_completed` | `level_name` (string, `Level_NNNN`), `moves_spent` (int), `total_score` (int), `match_duration_sec` (int) | Victory conditions met | `AnalyticsService` via `LevelEvents.OnLevelCompleted` |
| `level_failed` | `level_name` (string, `Level_NNNN`), `match_duration_sec` (int) | Move limit reached before objectives are cleared | `AnalyticsService` via `LevelEvents.OnLevelFailed` |
| `coins_earned` | `amount` (int), `source` (string), `level_name` (string, `Level_NNNN`) | Coins granted after a level win (`source` = `level_complete`) | `AnalyticsService` via `LevelEvents.OnLevelCompleted` |
| `coins_spent` | `amount` (int), `reason` (string), `level_index` (int) | Coins spent via `EconomyService.TrySpendCoins` (e.g. lose-menu continue) | `AnalyticsService` via `IEconomyService.OnCoinsSpent` |
| `ad_watched` | `ad_format` (string), `placement` (string), `result` (string) | Interstitial closed after display (`result` = `completed`); rewarded closed with reward (`rewarded`) or without (`dismissed`); Editor-simulated show (`editor_simulated`) | `AdsService` |
| `ad_skipped` | `ad_format` (string), `placement` (string), `reason` (string) | Interstitial or rewarded could not be shown (`display_failed`, `not_ready`, `not_initialized`; rewarded also `no_fill`, `not_configured`); gameplay continues | `AdsService` |
| `extra_automated_matches` | `level_name` (string, `Level_NNNN`), `moves_spent` (int) | Match cascade cycle count exceeds 2 after a player move | `GridController` |

---

## Typical session sequence

```text
session_started
  -> level_started
  -> level_completed + coins_earned  OR  level_failed (+ optional coins_spent or rewarded ad_watched/ad_skipped on continue)
  -> ad_watched OR ad_skipped        (leaving the win screen via Loader interstitial gate)
session_ended
```

---

## Related docs

- [Architecture](architecture.md)

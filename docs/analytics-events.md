# Firebase Analytics Events

Reference for analytics event names and parameters logged by PocketMatch.
Event names are defined in `Assets/Scripts/Analytics/AnalyticsEvents.cs` and sent through `AnalyticsService`.

**SDK:** Firebase Analytics 13.5.0  
**Transport:** `AnalyticsService` queues events locally in `analytics_cache.json` and flushes when Firebase is ready.

---

## Active events

| Event name | Parameters | When fired | Source |
|------------|------------|------------|--------|
| `app_started` | (none) | Firebase dependencies resolve successfully on first init in this process | `AnalyticsService` |
| `session_started` | `device` (string), `app_version` (string) | `FirebaseInitializer` constructed at bootstrap | `FirebaseInitializer` |
| `session_ended` | `device` (string), `app_version` (string) | `FirebaseInitializer` disposed (app quit or scope teardown) | `FirebaseInitializer` |
| `level_started` | `level_name` (string), `level_index` (int) | Board initialized and level is ready to play | `AnalyticsService` via `LevelEvents.OnLevelStarted` |
| `level_completed` | `level_name` (string), `moves_spent` (int), `total_score` (int), `match_duration_sec` (int) | Victory conditions met | `AnalyticsService` via `LevelEvents.OnLevelCompleted` |
| `level_failed` | `level_name` (string), `match_duration_sec` (int) | Move limit reached before objectives are cleared | `AnalyticsService` via `LevelEvents.OnLevelFailed` |
| `coins_earned` | `amount` (int), `source` (string), `level_name` (string) | Coins granted after a level win (`source` = `level_complete`); not fired when the level cap is reached | `AnalyticsService` via `LevelEvents.OnLevelCompleted` |
| `ad_watched` | `ad_format` (string), `placement` (string), `result` (string) | Interstitial closed after display (`result` = `completed`), or Editor-simulated show (`editor_simulated`) | `AdsService` |
| `ad_skipped` | `ad_format` (string), `placement` (string), `reason` (string) | Interstitial could not be shown (`display_failed`, `not_ready`, or `not_initialized`); gameplay continues | `AdsService` |
| `extra_automated_matches` | `level_name` (string), `moves_spent` (int) | Match cascade cycle count exceeds 2 after a player move | `GridController` |

---

## Defined but not fired

| Event name | Status | Notes |
|------------|--------|-------|
| `coins_spent` | Not wired | No coin sink implemented yet |
| `booster_used` | Not wired | No inventory boosters; match-created power tiles are not logged as boosters |
| `tile_matched` | Not wired | Per-match volume is too high; use aggregates if needed later |
| `iap_purchased` | Not wired | In-app purchases are not implemented |

---

## Typical session sequence

```text
session_started
  -> level_started
  -> level_completed + coins_earned  OR  level_failed
  -> ad_watched OR ad_skipped        (on next-level load via Loader interstitial gate)
session_ended
```

`app_started` fires once per process when Firebase initializes, before or alongside the first queued events.

---

## Offline behavior

If Firebase is unavailable at log time, events are appended to the local queue at:

`Application.persistentDataPath/analytics_cache.json`

When Firebase becomes ready, `AnalyticsService` flushes the queue.

---

## Related docs

- [Architecture](architecture.md)

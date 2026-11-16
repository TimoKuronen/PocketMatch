# Analytics Events

Reference for analytics event names and parameters raised by PocketMatch.
Event names are defined in `Assets/Scripts/Analytics/AnalyticsEvents.cs` and go through `IAnalyticsService`.

**Transport:** `AnalyticsService` delivers events through `IAnalyticsTransport`. `UgsAnalyticsTransport` sends them to Unity Analytics, using the `development` environment in the Editor and development builds and `production` otherwise. Events wait in a local queue (`analytics_cache.json`, capped at 200) until the transport is ready, and are logged in the Editor and development builds.

**Consent:** nothing is recorded until the player answers the opt-in prompt on the main menu. A declined answer discards queued events and stops collection; the answer is kept on the device and can be changed from Settings.

**SDK events:** the analytics SDK reports installs, sessions, and platform data itself (`newPlayer`, `gameStarted`, `gameEnded`, with `clientVersion` and `platform`). They are not part of the dictionary below.

**Dictionary lock:** the names below match the live constants in `AnalyticsEvents`; no other names are fired.

---

## Events (5)

| Event name | Parameters | When fired | Source |
|------------|------------|------------|--------|
| `level_started` | `level_name` (string, `Level_NNNN`), `level_index` (int) | Board initialized and level is ready to play | `AnalyticsService` via `LevelEvents.OnLevelStarted` |
| `level_completed` | `level_name` (string, `Level_NNNN`), `moves_spent` (int), `total_score` (int), `match_duration_sec` (int) | Victory conditions met | `AnalyticsService` via `LevelEvents.OnLevelCompleted` |
| `level_failed` | `level_name` (string, `Level_NNNN`), `match_duration_sec` (int) | Move limit reached before objectives are cleared | `AnalyticsService` via `LevelEvents.OnLevelFailed` |
| `coins_spent` | `amount` (int), `reason` (string), `level_index` (int) | Coins spent via `EconomyService.TrySpendCoins` (e.g. lose-menu continue) | `AnalyticsService` via `IEconomyService.OnCoinsSpent` |
| `adImpression` | `ad_format` (string: `interstitial`, `rewarded`), `placement` (string), `result` (string) | Interstitial shown, or rewarded closed with reward (`rewarded`) or without (`dismissed`); simulated shows in the Editor carry a `simulated` result | `AdsService`, mapped by the transport onto the SDK standard ad impression event |

---

## Typical session sequence

```text
gameStarted (SDK)
  -> level_started
  -> level_completed  OR  level_failed (+ optional coins_spent or rewarded adImpression on continue)
  -> adImpression                    (leaving the win screen via Loader interstitial gate)
gameEnded (SDK)
```

---

## Related docs

- [Architecture](architecture.md)

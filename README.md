# PocketMatch

Mobile Match-3 prototype built in **Unity 6** for live mobile F2P client engineering: async board commands, VContainer service boundaries, Addressables content, Firebase analytics and cloud save, and LevelPlay ad mediation.

[![Unity 6000.3.14f1](https://img.shields.io/badge/Unity-6000.3.14f1-black.svg)](ProjectSettings/ProjectVersion.txt)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

<p align="center">
  <img src="docs/images/main-menu.png" alt="Main menu" width="240" />
  <img src="docs/images/gameplay-board.png" alt="Gameplay" width="240" />
</p>

## Highlights

- Match-3 core loop with power tiles, level objectives, and win/lose flow
- Async command-queued board resolution (swap, destroy, gravity, power tiles)
- Deadlock detection with shuffle-until-playable
- Level select and light meta progression (coins, unlocked levels)
- Coin economy: earn on win, spend to continue after fail
- VContainer scopes for bootstrap, menu, and gameplay lifetimes
- Addressables level loading and pooled VFX
- Firebase Analytics with a local offline event queue
- Encrypted local save plus Firestore cloud sync
- Unity LevelPlay banner and interstitial mediation
- Edit Mode tests for potential moves and shuffle
- Android CI build via GitHub Actions

## Architecture

```text
BootstrapLifetimeScope (DontDestroyOnLoad)
  Save / Analytics / Ads / Audio / Input / Firebase bootstrap
        |
        v
Loader -> MainMenu (MenuLifetimeScope)
        |
        v
Loader (optional interstitial gate) -> PlayScene (GameLifetimeScope)
```

Board mutations serialize through `CommandInvoker` and `ICommand` implementations. Live-service hooks enter at bootstrap (analytics/session), level events (start/complete/fail), and Loader transitions (interstitials).

Details: [docs/architecture.md](docs/architecture.md)

## Stack

| Area | Choice |
|------|--------|
| Engine | Unity `6000.3.14f1` (URP 2D) |
| DI | [VContainer](https://github.com/hadashiA/VContainer) |
| Async | [UniTask](https://github.com/Cysharp/UniTask) |
| Content | Addressables (levels, VFX) |
| Auth / cloud save | Firebase Anonymous Auth + Firestore |
| Ads | Unity LevelPlay (IronSource mediation) |
| Analytics | Firebase Analytics (offline queue) |

## Docs

- [Architecture](docs/architecture.md)
- [Analytics events](docs/analytics-events.md)
- [Third-party attribution](docs/third-party.md)

## License

Original first-party code and docs: [MIT](LICENSE).

Third-party packages and Asset Store plugins keep their own licenses — see [docs/third-party.md](docs/third-party.md).
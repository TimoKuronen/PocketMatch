# PocketMatch

Mobile Match-3 prototype built in **Unity 6**, focused on production-style architecture for a free-to-play client: dependency injection, async boot flow, local + cloud save, ads mediation, and analytics.

> Display name in player settings is currently `RuneMatch`. The repository and Android application id use `PocketMatch`.

## Stack

| Area | Choice |
|------|--------|
| Engine | Unity `6000.3.14f1` (URP 2D) |
| DI | [VContainer](https://github.com/hadashiA/VContainer) |
| Async | [UniTask](https://github.com/Cysharp/UniTask) |
| Content | Addressables (levels, VFX) |
| Auth / cloud save | Firebase Anonymous Auth + Firestore |
| Ads | Unity LevelPlay (IronSource mediation) |
| Analytics | Firebase Analytics (with local offline queue) |

## Project layout (first-party)

```text
Assets/
  _Project/          Scenes and ScriptableObject configs
  Scripts/           Gameplay, DI, services, UI, save, ads, analytics
  Prefabs/           Bootstrap and gameplay prefabs
  Addressables/      Level data and related content
  StreamingAssets/   Platform config (e.g. google-services.json)
Packages/            UPM manifest
ProjectSettings/     Unity project settings
.github/workflows/   Android CI build
```

Unity-generated folders such as `Library/`, `Temp/`, `Logs/`, `UserSettings/`, `GeneratedAssets/`, and `ProfilerCaptures/` are gitignored.

## Scenes

| Build order | Scene | Role |
|-------------|-------|------|
| 0 | `Assets/_Project/Scenes/Loader.unity` | Cold-start hub and loading |
| 1 | `Assets/_Project/Scenes/MainMenu.unity` | Meta / menu |
| 2 | `Assets/_Project/Scenes/PlayScene.unity` | Match-3 gameplay |

Open the project in Unity Hub with the pinned editor version, then enter Play Mode from the Loader flow (editor bootstrap mirrors device cold start).

## What this repo is (and is not)

**Is:** a portfolio-oriented Unity client showing Match-3 systems plus common mobile service wiring.

**Is not:** a shipped live-ops product. IAP, remote config, and a full meta loop are intentionally thin or absent. Local scratch notes and longer learning writeups under `Assets/` are kept out of Git on purpose so the public tree stays code-first.

## Build / CI

Android builds are defined in `.github/workflows/main.yml` (manual or push to `master`). CI expects Unity license secrets configured in the GitHub repo settings. Keep the workflow Unity version in sync with `ProjectSettings/ProjectVersion.txt` when upgrading.

## License / third-party

Third-party packages and Asset Store plugins remain under their own licenses (DOTween, Firebase, LevelPlay, Cartoon FX, etc.). First-party game code and content in this repository are the author's work unless noted otherwise.
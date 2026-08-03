# Third-party attribution

PocketMatch bundles third-party engines, SDKs, and Asset Store content. Their licenses and terms remain in force and supersede the repository MIT license for those files.

## Notable bundled dependencies

| Dependency | Location (typical) | Notes |
|------------|--------------------|-------|
| Unity engine and UPM packages | `Packages/`, ProjectSettings | Unity terms |
| Firebase Unity SDK | `Assets/Firebase/` | Google / Firebase terms |
| Unity LevelPlay (IronSource) | `Assets/LevelPlay/` | Unity LevelPlay terms |
| DOTween | `Assets/Plugins/Demigiant/DOTween/` | Demigiant license |
| Cartoon FX Remaster | `Assets/JMO Assets/Cartoon FX Remaster/` | Jean Moreno / Asset Store terms |
| Polygonal Particles | `Assets/Polygonal Particles/` | Asset Store terms |
| TextMesh Pro | `Assets/TextMesh Pro/` | Unity / TMP terms |
| External Dependency Manager | `Assets/ExternalDependencyManager/` | Google EDM4U license |
| VContainer | UPM / package cache | VContainer license |
| UniTask | UPM / package cache | Cysharp license |

This list is not exhaustive. Inspect vendor folders and package manifests for the authoritative license text shipped with each dependency.

## First-party scope

Original PocketMatch gameplay code under `Assets/Scripts/`, project scenes/prefabs/content authored for this game, and documentation under `docs/` and `README.md` are covered by the root [LICENSE](../LICENSE) unless a file says otherwise.

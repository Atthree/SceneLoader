# Changelog

## [0.1.0] - 2026-11-25
- Initial version: `SceneLoader` static API: `Load` (scene, group, build index), `Reload`, `LoadNext`,
  `LoadAdditive`, `Unload`, `Quit`, `Continue`; one load at a time; events `LoadStarted`,
  `BeforeSceneUnload`, `SceneActivated`, `LoadCompleted`, `ProgressChanged`.
- `LoadOptions`: fade (color, durations, real time), custom `SceneTransition` prefabs, loading screen with
  minimum time and "press any key", audio fade, time scale reset. Presets `Fade`, `Instant`,
  `WithLoadingScreen`.
- `SceneLoadOperation`: yield it in coroutines, progress, `Completed` event, failure info.
- `SceneReference` (drag a scene in the Inspector, kept by GUID, "Add to Build" button), `SceneGroup`.
- Built-in `FadeTransition` and `LoadingScreen` (bar, percentage, tips, events for TextMeshPro).
- Components: `SceneLoadButton`, `SceneLoadTrigger` (3D/2D, tag filter), `LoadSceneOnStart`,
  `PersistentObject` (one copy across scenes), `BlinkingText`.
- Window > Atthree > Scenes: open/play/reorder build scenes, other scenes in the project, Play From First Scene.
- Demo sample. 26 PlayMode tests (also pass without the Input System package).

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Atthree.SceneLoading
{
    /// <summary>
    /// Loads scenes with a transition and an optional loading screen.
    /// <code>
    /// SceneLoader.Load("Level1");                                       // 0.35 s black fade
    /// SceneLoader.Load(level, LoadOptions.WithLoadingScreen(1.5f));     // fade + loading screen
    /// SceneLoader.Reload();  SceneLoader.LoadNext();  SceneLoader.Quit();
    /// yield return SceneLoader.Load("Boss");                            // wait in a coroutine
    /// </code>
    /// Only one load runs at a time; a call made during a load is ignored (with a warning).
    /// </summary>
    public static class SceneLoader
    {
        /// <summary>Used when a load is called without options. Change it once at startup if you like.</summary>
        public static LoadOptions DefaultOptions { get; set; } = LoadOptions.Fade();

        /// <summary>A transition or load is running.</summary>
        public static bool IsLoading => Current != null;

        /// <summary>The running load, or null.</summary>
        public static SceneLoadOperation Current { get; private set; }

        /// <summary>A load started (before the screen is covered).</summary>
        public static event Action<SceneLoadOperation> LoadStarted;

        /// <summary>The screen is covered and the old scene is about to be unloaded: a good moment to save.</summary>
        public static event Action<SceneLoadOperation> BeforeSceneUnload;

        /// <summary>The new scene(s) are loaded and active, still behind the transition.</summary>
        public static event Action<SceneLoadOperation> SceneActivated;

        /// <summary>The load is over and the new scene is visible (also called when it failed).</summary>
        public static event Action<SceneLoadOperation> LoadCompleted;

        /// <summary>Loading progress 0–1 of the running load.</summary>
        public static event Action<float> ProgressChanged;

        // ================================================================ loading

        /// <summary>Replace the open scenes with this one.</summary>
        public static SceneLoadOperation Load(SceneReference scene, LoadOptions options = null) =>
            Start(new[] { scene }, 0, options);

        /// <summary>Replace the open scenes with a group: the first replaces, the others are added.</summary>
        public static SceneLoadOperation Load(SceneGroup group, LoadOptions options = null)
        {
            if (group == null || group.scenes.Count == 0)
                return Fail("(empty group)", "SceneLoader: the scene group is empty.");
            return Start(group.scenes.ToArray(), Mathf.Clamp(group.activeScene, 0, group.scenes.Count - 1), options);
        }

        /// <summary>Load a scene by its index in the build list.</summary>
        public static SceneLoadOperation LoadBuildIndex(int buildIndex, LoadOptions options = null)
        {
            if (buildIndex < 0 || buildIndex >= SceneManager.sceneCountInBuildSettings)
                return Fail($"#{buildIndex}", $"SceneLoader: there's no scene with build index {buildIndex}.");
            return Load(new SceneReference(SceneUtility.GetScenePathByBuildIndex(buildIndex)), options);
        }

        /// <summary>Load the active scene again (restart a level).</summary>
        public static SceneLoadOperation Reload(LoadOptions options = null)
        {
            var active = SceneManager.GetActiveScene();
            return Load(new SceneReference(string.IsNullOrEmpty(active.path) ? active.name : active.path), options);
        }

        /// <summary>Load the scene after the active one in the build list. At the end: the first one, or nothing if <paramref name="wrap"/> is false.</summary>
        public static SceneLoadOperation LoadNext(LoadOptions options = null, bool wrap = true)
        {
            int count = SceneManager.sceneCountInBuildSettings;
            int next = SceneManager.GetActiveScene().buildIndex + 1;
            if (next >= count)
            {
                if (!wrap || count == 0) return Fail("(next)", "SceneLoader: the active scene is the last one in the build list.");
                next = 0;
            }
            return LoadBuildIndex(next, options);
        }

        /// <summary>Add a scene on top of the open ones, no transition (e.g. a UI scene or a level chunk).</summary>
        public static SceneLoadOperation LoadAdditive(SceneReference scene, bool setActive = false)
        {
            var op = new SceneLoadOperation(scene?.Name);
            if (!CanLoad(scene, out var error)) { Debug.LogError(error); op.Finish(error); return op; }
            Runner.StartCoroutine(AdditiveRoutine(scene, setActive, op));
            return op;
        }

        /// <summary>Remove a scene that was added with <see cref="LoadAdditive"/>.</summary>
        public static SceneLoadOperation Unload(SceneReference scene)
        {
            var op = new SceneLoadOperation(scene?.Name);
            var loaded = scene?.LoadedScene ?? default;
            if (!loaded.IsValid() || !loaded.isLoaded)
            {
                op.Finish($"SceneLoader: {scene} isn't loaded.");
                return op;
            }
            if (SceneManager.sceneCount <= 1)
            {
                var error = $"SceneLoader: can't unload {scene}, it's the only open scene. Use Load to replace it.";
                Debug.LogWarning(error);
                op.Finish(error);
                return op;
            }
            Runner.StartCoroutine(UnloadRoutine(loaded, op));
            return op;
        }

        /// <summary>Fade out, then quit the game (or leave Play mode in the editor).</summary>
        public static void Quit(LoadOptions options = null)
        {
            options ??= DefaultOptions;
            Runner.StartCoroutine(QuitRoutine(options));
        }

        /// <summary>Go on when the loader is waiting for input (e.g. from a "Continue" button).</summary>
        public static void Continue() => Current?.Continue();

        // ================================================================ internals

        static SceneLoaderRunner runner;
        static SceneLoaderRunner Runner
        {
            get
            {
                if (runner == null)
                {
                    var go = new GameObject("[Scene Loader]");
                    Object.DontDestroyOnLoad(go);
                    runner = go.AddComponent<SceneLoaderRunner>();
                }
                return runner;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Current = null;
            runner = null;
            DefaultOptions = LoadOptions.Fade();
            LoadStarted = null;
            BeforeSceneUnload = null;
            SceneActivated = null;
            LoadCompleted = null;
            ProgressChanged = null;
        }

        static bool CanLoad(SceneReference scene, out string error)
        {
            error = null;
            if (scene == null || !scene.IsSet) error = "SceneLoader: no scene was given.";
            else if (!scene.IsInBuild && !Application.CanStreamedLevelBeLoaded(scene.LoadKey))
                error = $"SceneLoader: scene '{scene}' isn't in the build list. Add it in File > Build Profiles (or Window > Atthree > Scenes).";
            return error == null;
        }

        static SceneLoadOperation Fail(string name, string error)
        {
            Debug.LogError(error);
            var op = new SceneLoadOperation(name);
            op.Finish(error);
            return op;
        }

        static SceneLoadOperation Start(SceneReference[] scenes, int activeIndex, LoadOptions options)
        {
            var name = string.Join(" + ", scenes.Select(s => s?.ToString() ?? "(none)"));
            if (Current != null)
            {
                Debug.LogWarning($"SceneLoader: already loading {Current.SceneName}; ignored the request to load {name}.");
                return null;
            }
            foreach (var s in scenes)
            {
                if (!CanLoad(s, out var error)) return Fail(name, error);
            }

            var op = new SceneLoadOperation(name);
            Current = op;
            Runner.StartCoroutine(LoadRoutine(scenes, activeIndex, (options ?? DefaultOptions).Clone(), op));
            return op;
        }

        static IEnumerator LoadRoutine(SceneReference[] scenes, int activeIndex, LoadOptions o, SceneLoadOperation op)
        {
            Invoke(LoadStarted, op);
            float startTime = Time.realtimeSinceStartup;
            float volume = AudioListener.volume;

            // 1. cover the screen
            var transition = CreateTransition(o);
            if (transition != null) yield return Parallel(transition.Cover(o.fadeOutDuration), FadeAudio(o, volume, 0f, o.fadeOutDuration));
            else if (o.fadeAudio) AudioListener.volume = 0f;

            // 2. loading screen, revealed by the transition
            LoadingScreen screen = null;
            if (o.loadingScreen)
            {
                screen = o.loadingScreenPrefab != null ? Object.Instantiate(o.loadingScreenPrefab) : LoadingScreen.CreateDefault();
                screen.name = "[Loading Screen]";
                Object.DontDestroyOnLoad(screen.gameObject);
                screen.Show();
                if (transition != null) yield return transition.Reveal(o.fadeInDuration);
            }

            Invoke(BeforeSceneUnload, op);
            startTime = Time.realtimeSinceStartup;

            // 3. load. The last scene waits before it is activated, so the player can press a key first.
            for (int i = 0; i < scenes.Length; i++)
            {
                bool last = i == scenes.Length - 1;
                var async = SceneManager.LoadSceneAsync(scenes[i].LoadKey, i == 0 ? LoadSceneMode.Single : LoadSceneMode.Additive);
                if (async == null)
                {
                    yield return Finish(op, transition, screen, o, volume, $"SceneLoader: couldn't load '{scenes[i]}'.");
                    yield break;
                }
                if (last) async.allowSceneActivation = false;

                while (last ? async.progress < 0.9f : !async.isDone)
                {
                    Report(op, screen, (i + Mathf.Clamp01(async.progress / 0.9f)) / scenes.Length, o, startTime);
                    yield return null;
                }

                if (last)
                {
                    // minimum time on the loading screen, the bar fills up smoothly
                    while (screen != null && Time.realtimeSinceStartup - startTime < o.minLoadingTime)
                    {
                        Report(op, screen, 1f, o, startTime);
                        yield return null;
                    }
                    Report(op, screen, 1f, o, startTime);

                    if (o.waitForInput && screen != null) // without a screen there'd be nothing telling the player to press a key
                    {
                        op.WaitingForInput = true;
                        screen.ShowContinuePrompt();
                        yield return null; // the click that started the load doesn't count
                        while (!op.continueRequested && !screen.ContinuePressed()) yield return null;
                        op.WaitingForInput = false;
                    }

                    if (screen != null && transition != null) yield return transition.Cover(o.fadeOutDuration);
                    async.allowSceneActivation = true;
                    while (!async.isDone) yield return null;
                }
            }

            var active = scenes[Mathf.Clamp(activeIndex, 0, scenes.Length - 1)].LoadedScene;
            if (active.IsValid() && active.isLoaded) SceneManager.SetActiveScene(active);
            if (o.resetTimeScale) Time.timeScale = 1f;
            Invoke(SceneActivated, op);

            // 4. show the new scene
            if (screen != null) Object.Destroy(screen.gameObject);
            screen = null;
            yield return Finish(op, transition, null, o, volume, null);
        }

        static IEnumerator Finish(SceneLoadOperation op, SceneTransition transition, LoadingScreen screen, LoadOptions o, float volume, string error)
        {
            if (error != null) Debug.LogError(error);
            if (screen != null) Object.Destroy(screen.gameObject);
            if (transition != null)
            {
                yield return Parallel(transition.Reveal(o.fadeInDuration), FadeAudio(o, AudioListener.volume, volume, o.fadeInDuration));
                Object.Destroy(transition.gameObject);
            }
            if (o.fadeAudio) AudioListener.volume = volume;
            Current = null;
            op.Finish(error);
            Invoke(LoadCompleted, op);
        }

        static void Report(SceneLoadOperation op, LoadingScreen screen, float loadProgress, LoadOptions o, float startTime)
        {
            float shown = loadProgress;
            if (screen != null && o.minLoadingTime > 0f)
                shown = Mathf.Min(loadProgress, (Time.realtimeSinceStartup - startTime) / o.minLoadingTime);
            shown = Mathf.Max(op.Progress, Mathf.Clamp01(shown)); // never goes backwards
            if (Mathf.Approximately(shown, op.Progress) && shown > 0f) return;
            op.Progress = shown;
            screen?.SetProgress(shown);
            Invoke(ProgressChanged, shown);
        }

        static SceneTransition CreateTransition(LoadOptions o)
        {
            SceneTransition t = null;
            switch (o.transition)
            {
                case TransitionKind.Fade:
                    t = new GameObject("[Scene Transition]").AddComponent<FadeTransition>();
                    break;
                case TransitionKind.Custom when o.customTransition != null:
                    t = Object.Instantiate(o.customTransition);
                    t.name = "[Scene Transition]";
                    break;
                case TransitionKind.Custom:
                    Debug.LogWarning("SceneLoader: Transition is Custom but no prefab is set; using a fade.");
                    t = new GameObject("[Scene Transition]").AddComponent<FadeTransition>();
                    break;
            }
            if (t != null)
            {
                t.Color = o.fadeColor;
                Object.DontDestroyOnLoad(t.gameObject);
            }
            return t;
        }

        static IEnumerator FadeAudio(LoadOptions o, float from, float to, float duration)
        {
            if (!o.fadeAudio) yield break;
            float t = 0f;
            while (t < duration)
            {
                AudioListener.volume = Mathf.Lerp(from, to, t / duration);
                yield return null;
                t += Mathf.Min(Time.unscaledDeltaTime, 0.1f);
            }
            AudioListener.volume = to;
        }

        /// <summary>Runs two routines at the same time and waits for both.</summary>
        static IEnumerator Parallel(IEnumerator a, IEnumerator b)
        {
            bool aDone = false, bDone = false;
            Runner.StartCoroutine(Track(a, () => aDone = true));
            Runner.StartCoroutine(Track(b, () => bDone = true));
            while (!aDone || !bDone) yield return null;
        }

        static IEnumerator Track(IEnumerator routine, Action done)
        {
            yield return routine;
            done();
        }

        static IEnumerator AdditiveRoutine(SceneReference scene, bool setActive, SceneLoadOperation op)
        {
            var async = SceneManager.LoadSceneAsync(scene.LoadKey, LoadSceneMode.Additive);
            while (async != null && !async.isDone)
            {
                op.Progress = Mathf.Clamp01(async.progress / 0.9f);
                yield return null;
            }
            if (async == null) { op.Finish($"SceneLoader: couldn't load '{scene}'."); yield break; }
            if (setActive) SceneManager.SetActiveScene(scene.LoadedScene);
            op.Finish();
        }

        static IEnumerator UnloadRoutine(Scene scene, SceneLoadOperation op)
        {
            var async = SceneManager.UnloadSceneAsync(scene);
            while (async != null && !async.isDone)
            {
                op.Progress = async.progress;
                yield return null;
            }
            op.Finish();
        }

        static IEnumerator QuitRoutine(LoadOptions o)
        {
            var transition = CreateTransition(o);
            if (transition != null) yield return Parallel(transition.Cover(o.fadeOutDuration), FadeAudio(o, AudioListener.volume, 0f, o.fadeOutDuration));
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        static void Invoke<T>(Action<T> e, T arg)
        {
            if (e == null) return;
            foreach (var d in e.GetInvocationList())
            {
                try { ((Action<T>)d)(arg); }
                catch (Exception ex) { Debug.LogException(ex); }
            }
        }
    }
}

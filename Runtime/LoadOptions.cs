using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Atthree.SceneLoading
{
    public enum TransitionKind
    {
        /// <summary>Cut straight to the new scene.</summary>
        None,
        /// <summary>Fade to a color and back.</summary>
        Fade,
        /// <summary>Your own prefab with a <see cref="SceneTransition"/> component.</summary>
        Custom,
    }

    /// <summary>How a scene is loaded: transition, loading screen, timing.</summary>
    [Serializable]
    public class LoadOptions
    {
        [Tooltip("Fade = fade to a color and back. Custom = your own prefab with a SceneTransition component.")]
        public TransitionKind transition = TransitionKind.Fade;
        public Color fadeColor = Color.black;
        [Tooltip("Seconds to cover the screen (real time, works while paused).")]
        [Min(0)] public float fadeOutDuration = 0.35f;
        [Tooltip("Seconds to show the new scene.")]
        [Min(0)] public float fadeInDuration = 0.35f;
        [Tooltip("Prefab with a SceneTransition component (used when Transition = Custom).")]
        public SceneTransition customTransition;

        [Tooltip("Show a loading screen with a progress bar while loading.")]
        public bool loadingScreen;
        [Tooltip("Your own loading screen prefab. Empty = the built-in one.")]
        public LoadingScreen loadingScreenPrefab;
        [Tooltip("Keep the loading screen at least this many seconds, so it doesn't flash.")]
        [Min(0)] public float minLoadingTime = 0f;
        [Tooltip("Wait for a key, click or tap before showing the new scene.")]
        public bool waitForInput;

        [Tooltip("Fade the game's sound out and back in with the transition.")]
        public bool fadeAudio = true;
        [Tooltip("Set Time.timeScale back to 1 when the new scene starts (e.g. loading from a pause menu).")]
        public bool resetTimeScale = true;

        /// <summary>Copy, so the defaults or a component's settings aren't changed.</summary>
        public LoadOptions Clone() => (LoadOptions)MemberwiseClone();

        // ---------------------------------------------------------------- presets

        /// <summary>Default: 0.35 s black fade.</summary>
        public static LoadOptions Fade(float duration = 0.35f, Color? color = null) => new LoadOptions
        {
            transition = TransitionKind.Fade,
            fadeOutDuration = duration,
            fadeInDuration = duration,
            fadeColor = color ?? Color.black,
        };

        /// <summary>No transition at all.</summary>
        public static LoadOptions Instant() => new LoadOptions
        {
            transition = TransitionKind.None,
            fadeOutDuration = 0,
            fadeInDuration = 0,
            fadeAudio = false,
        };

        /// <summary>Fade, then a loading screen shown for at least <paramref name="minTime"/> seconds.</summary>
        public static LoadOptions WithLoadingScreen(float minTime = 1f, bool waitForInput = false) => new LoadOptions
        {
            transition = TransitionKind.Fade,
            loadingScreen = true,
            minLoadingTime = minTime,
            waitForInput = waitForInput,
        };

        public LoadOptions WithColor(Color color)
        {
            var o = Clone();
            o.fadeColor = color;
            return o;
        }
    }
}

using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
#if ATTHREE_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Atthree.SceneLoading
{
    /// <summary>
    /// Shown while a scene loads. The built-in one (a bar, a percentage and a tip) is made in code.
    /// For your own: make a prefab with a Canvas, add this component, assign the bar/texts (or use the
    /// events, e.g. for TextMeshPro), and set it as <see cref="LoadOptions.loadingScreenPrefab"/>.
    /// </summary>
    [AddComponentMenu("Atthree/Scene Loading/Loading Screen")]
    public class LoadingScreen : MonoBehaviour
    {
        [Header("Optional parts")]
        [Tooltip("An Image with Image Type = Filled. Its fill amount follows the progress.")]
        public Image progressBar;
        [Tooltip("Or a Slider (0–1).")]
        public Slider progressSlider;
        public Text progressText;
        public Text tipText;
        [Tooltip("Shown when the scene is ready and waits for a key (Wait For Input).")]
        public GameObject continuePrompt;

        [Header("Tips")]
        [Tooltip("One of these is shown at random.")]
        [TextArea] public string[] tips = Array.Empty<string>();

        [Header("Events (e.g. for TextMeshPro texts)")]
        public UnityEvent<float> onProgress = new UnityEvent<float>();
        public UnityEvent<string> onProgressText = new UnityEvent<string>();
        public UnityEvent onReady = new UnityEvent();

        /// <summary>Shown progress, 0–1.</summary>
        public float Progress { get; private set; }

        public const int SortingOrder = 31000;

        /// <summary>Called when the screen appears (it starts hidden behind the transition).</summary>
        public virtual void Show()
        {
            gameObject.SetActive(true);
            if (continuePrompt != null) continuePrompt.SetActive(false);
            if (tipText != null && tips != null && tips.Length > 0)
                tipText.text = tips[UnityEngine.Random.Range(0, tips.Length)];
            SetProgress(0f);
        }

        public virtual void SetProgress(float value)
        {
            Progress = Mathf.Clamp01(value);
            if (progressBar != null) progressBar.fillAmount = Progress;
            if (progressSlider != null) progressSlider.value = Progress;
            var text = $"{Mathf.RoundToInt(Progress * 100f)}%";
            if (progressText != null) progressText.text = text;
            onProgress.Invoke(Progress);
            onProgressText.Invoke(text);
        }

        /// <summary>The scene is loaded and waits for the player.</summary>
        public virtual void ShowContinuePrompt()
        {
            if (continuePrompt != null) continuePrompt.SetActive(true);
            onReady.Invoke();
        }

        /// <summary>True on the frame the player presses a key, clicks or taps.</summary>
        public virtual bool ContinuePressed()
        {
#if ATTHREE_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) return true;
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) return true;
            if (Gamepad.current != null && (Gamepad.current.buttonSouth.wasPressedThisFrame || Gamepad.current.startButton.wasPressedThisFrame)) return true;
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame) return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.anyKeyDown) return true;
#endif
            return false;
        }

        // ---------------------------------------------------------------- built-in screen

        static readonly string[] DefaultTips = Array.Empty<string>();

        /// <summary>Builds the default loading screen: dark background, progress bar, percentage, tip.</summary>
        public static LoadingScreen CreateDefault()
        {
            var canvas = SceneTransition.CreateOverlayCanvas("[Loading Screen]", SortingOrder);
            var screen = canvas.gameObject.AddComponent<LoadingScreen>();
            screen.tips = DefaultTips;
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var bg = SceneTransition.CreateFullScreen("Background", canvas.transform).gameObject.AddComponent<Image>();
            bg.color = new Color(0.07f, 0.07f, 0.09f, 1f);

            // bar frame + fill, bottom center
            var frame = new GameObject("Bar", typeof(RectTransform)).GetComponent<RectTransform>();
            frame.SetParent(canvas.transform, false);
            frame.anchorMin = frame.anchorMax = new Vector2(0.5f, 0.18f);
            frame.sizeDelta = new Vector2(900, 14);
            frame.gameObject.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.12f);

            var fill = SceneTransition.CreateFullScreen("Fill", frame).gameObject.AddComponent<Image>();
            fill.sprite = WhiteSprite();
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.color = new Color(0.95f, 0.95f, 0.95f, 1f);
            screen.progressBar = fill;

            screen.progressText = MakeText("Percent", canvas.transform, font, 34, new Vector2(0.5f, 0.18f), new Vector2(0, 40), TextAnchor.MiddleCenter);
            screen.tipText = MakeText("Tip", canvas.transform, font, 28, new Vector2(0.5f, 0.1f), Vector2.zero, TextAnchor.MiddleCenter);
            screen.tipText.color = new Color(1f, 1f, 1f, 0.6f);

            var prompt = MakeText("Continue", canvas.transform, font, 36, new Vector2(0.5f, 0.3f), Vector2.zero, TextAnchor.MiddleCenter);
            prompt.text = "Press any key";
            prompt.gameObject.AddComponent<BlinkingText>();
            screen.continuePrompt = prompt.gameObject;
            prompt.gameObject.SetActive(false);
            return screen;
        }

        static Text MakeText(string name, Transform parent, Font font, int size, Vector2 anchor, Vector2 offset, TextAnchor align)
        {
            var rt = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = anchor;
            rt.anchoredPosition = offset;
            rt.sizeDelta = new Vector2(1400, 60);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = font;
            t.fontSize = size;
            t.alignment = align;
            t.color = Color.white;
            t.raycastTarget = false;
            return t;
        }

        static Sprite whiteSprite;
        static Sprite WhiteSprite()
        {
            // Filled images need a sprite.
            if (whiteSprite == null)
            {
                var tex = Texture2D.whiteTexture;
                whiteSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                whiteSprite.name = "Loading Bar";
            }
            return whiteSprite;
        }
    }
}

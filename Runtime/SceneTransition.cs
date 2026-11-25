using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Atthree.SceneLoading
{
    /// <summary>
    /// Covers the screen before a scene changes and reveals it afterwards. Make your own by deriving from
    /// this (wipes, circles, sliding panels…), put it on a prefab and pick <c>Transition = Custom</c>.
    /// The object is kept alive across the scene change by the loader. Use unscaled time: the game may be paused.
    /// </summary>
    public abstract class SceneTransition : MonoBehaviour
    {
        /// <summary>Set by the loader from <see cref="LoadOptions.fadeColor"/> before Cover.</summary>
        public Color Color { get; set; } = Color.black;

        /// <summary>0 = scene fully visible, 1 = fully covered.</summary>
        public float Amount { get; protected set; }

        /// <summary>Animate until the screen is fully covered.</summary>
        public abstract IEnumerator Cover(float duration);

        /// <summary>Animate until the new scene is fully visible.</summary>
        public abstract IEnumerator Reveal(float duration);

        /// <summary>Runs <paramref name="apply"/> from 0 to 1 over <paramref name="duration"/> real seconds.</summary>
        protected static IEnumerator Animate(float duration, System.Action<float> apply)
        {
            if (duration <= 0f)
            {
                apply(1f);
                yield break;
            }
            float t = 0f;
            while (t < duration)
            {
                apply(Mathf.Clamp01(t / duration));
                yield return null;
                t += Mathf.Min(Time.unscaledDeltaTime, 0.1f); // a long load frame doesn't skip the fade
            }
            apply(1f);
        }

        /// <summary>A screen-space canvas above everything else, for transitions and loading screens.</summary>
        public static Canvas CreateOverlayCanvas(string name, int sortingOrder, Transform parent = null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            if (parent != null) go.transform.SetParent(parent, false);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>(); // blocks clicks on the scene below while covered
            return canvas;
        }

        /// <summary>A RectTransform child that fills its parent.</summary>
        public static RectTransform CreateFullScreen(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return rt;
        }
    }
}

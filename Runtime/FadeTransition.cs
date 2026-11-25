using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Atthree.SceneLoading
{
    /// <summary>
    /// Fades to a color and back. Used by default. On its own it builds a full-screen image; on a prefab
    /// with a <see cref="CanvasGroup"/> it fades that group instead, so you can add a logo or text to it.
    /// </summary>
    [AddComponentMenu("Atthree/Scene Loading/Fade Transition")]
    public class FadeTransition : SceneTransition
    {
        [Tooltip("Optional. Fades this group (with everything in it). Empty = a full-screen image is made.")]
        [SerializeField] CanvasGroup group;
        [Tooltip("Optional. Tinted with the load's fade color.")]
        [SerializeField] Image background;
        [Tooltip("Ease in and out instead of a linear fade.")]
        [SerializeField] bool smooth = true;

        public const int SortingOrder = 32000;

        void Awake() => Build();

        void Build()
        {
            if (group != null) return;
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = CreateOverlayCanvas("Fade Canvas", SortingOrder, transform);
            group = canvas.gameObject.AddComponent<CanvasGroup>();
            background = CreateFullScreen("Fade", canvas.transform).gameObject.AddComponent<Image>();
            Set(0f);
        }

        void Set(float amount)
        {
            Amount = amount;
            if (background != null) background.color = new Color(Color.r, Color.g, Color.b, 1f);
            group.alpha = smooth ? Mathf.SmoothStep(0f, 1f, amount) : amount;
            group.blocksRaycasts = amount > 0f;
            group.interactable = false;
        }

        public override IEnumerator Cover(float duration)
        {
            Build();
            float start = Amount;
            yield return Animate(duration * (1f - start), t => Set(Mathf.Lerp(start, 1f, t)));
        }

        public override IEnumerator Reveal(float duration)
        {
            Build();
            float start = Amount;
            yield return Animate(duration * start, t => Set(Mathf.Lerp(start, 0f, t)));
        }
    }
}

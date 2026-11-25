using UnityEngine;
using UnityEngine.UI;

namespace Atthree.SceneLoading
{
    /// <summary>Pulses a UI Graphic's alpha in real time, e.g. a "Press any key" prompt.</summary>
    [AddComponentMenu("Atthree/Scene Loading/Blinking Text")]
    [RequireComponent(typeof(Graphic))]
    public class BlinkingText : MonoBehaviour
    {
        [Min(0)] public float speed = 4f;
        [Range(0, 1)] public float minAlpha = 0.45f;

        Graphic graphic;

        void Awake() => graphic = GetComponent<Graphic>();

        void Update()
        {
            var c = graphic.color;
            c.a = Mathf.Lerp(minAlpha, 1f, 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * speed));
            graphic.color = c;
        }
    }
}

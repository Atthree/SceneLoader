using UnityEngine;

namespace Atthree.SceneLoading.Demo
{
    /// <summary>
    /// Lives on a PersistentObject in the menu scene: it survives every scene change and counts them.
    /// Go back to the menu: there is still only one.
    /// </summary>
    public class SceneLoaderDemoHud : MonoBehaviour
    {
        int loads;
        float lastDuration, startedAt;
        string last = "-";
        GUIStyle style;

        void OnEnable()
        {
            SceneLoader.LoadStarted += OnStarted;
            SceneLoader.LoadCompleted += OnCompleted;
        }

        void OnDisable()
        {
            SceneLoader.LoadStarted -= OnStarted;
            SceneLoader.LoadCompleted -= OnCompleted;
        }

        void OnStarted(SceneLoadOperation op) => startedAt = Time.realtimeSinceStartup;

        void OnCompleted(SceneLoadOperation op)
        {
            loads++;
            last = op.SceneName;
            lastDuration = Time.realtimeSinceStartup - startedAt;
        }

        void OnGUI()
        {
            if (style == null) style = new GUIStyle(GUI.skin.box) { fontSize = 14, alignment = TextAnchor.UpperLeft };
            GUI.Box(new Rect(10, 10, 320, 80),
                $"Persistent object (kept across scenes)\nLoads: {loads}\nLast: {last} ({lastDuration:0.00} s)", style);
        }
    }
}

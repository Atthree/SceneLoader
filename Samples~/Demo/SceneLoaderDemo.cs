using System;
using System.Collections.Generic;
using UnityEngine;

namespace Atthree.SceneLoading.Demo
{
    /// <summary>
    /// Demo scene script: buttons (drawn with OnGUI, no Canvas needed) that run SceneActions, and a cube
    /// that spins so you can see the game keeps running behind the transition.
    /// </summary>
    public class SceneLoaderDemo : MonoBehaviour
    {
        [Serializable]
        public class Entry
        {
            public string label = "Button";
            public SceneAction action = new SceneAction();
        }

        public string title = "Scene Loader Demo";
        [TextArea] public string description = "";
        public Transform spinner;
        public Color spinnerColor = Color.white;
        public List<Entry> buttons = new List<Entry>();

        GUIStyle titleStyle, textStyle, buttonStyle;

        void Start()
        {
            // a material that works in URP and in the Built-in pipeline
            if (spinner == null) return;
            var pipeline = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
            var mat = pipeline != null && pipeline.defaultMaterial != null
                ? new Material(pipeline.defaultMaterial)
                : new Material(Shader.Find("Standard"));
            mat.color = spinnerColor;
            spinner.GetComponent<Renderer>().material = mat;
        }

        void Update()
        {
            if (spinner != null) spinner.Rotate(20f * Time.deltaTime, 45f * Time.deltaTime, 0f);
        }

        void OnGUI()
        {
            if (titleStyle == null)
            {
                titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 40, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                textStyle = new GUIStyle(GUI.skin.label) { fontSize = 18, wordWrap = true, alignment = TextAnchor.UpperCenter };
                buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 20 };
            }
            float w = 460f;
            GUILayout.BeginArea(new Rect((Screen.width - w) / 2f, Screen.height * 0.12f, w, Screen.height * 0.85f));
            GUILayout.Label(title, titleStyle, GUILayout.Height(60));
            if (!string.IsNullOrEmpty(description)) GUILayout.Label(description, textStyle);
            GUILayout.Space(16);

            if (!AllInBuild())
            {
                GUILayout.Label("The demo scenes aren't in the build list yet.\nStop Play mode, open Window > Atthree > Scenes and click Add next to the three Demo scenes.", textStyle);
                GUILayout.EndArea();
                return;
            }

            GUI.enabled = !SceneLoader.IsLoading;
            foreach (var b in buttons)
            {
                if (GUILayout.Button(b.label, buttonStyle, GUILayout.Height(46))) b.action.Run();
                GUILayout.Space(6);
            }
            GUI.enabled = true;
            GUILayout.EndArea();
        }

        bool AllInBuild()
        {
            foreach (var b in buttons)
                if (b.action.action == SceneActionKind.LoadScene && !b.action.scene.IsInBuild) return false;
            return true;
        }
    }
}

using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Atthree.SceneLoading
{
    /// <summary>
    /// Window > Atthree > Scenes: open, play and order the build scenes, find the other scenes, and
    /// "Play From First Scene" so Play mode always starts in your boot scene.
    /// </summary>
    public class ScenesWindow : EditorWindow
    {
        const string PlayFromFirstKey = "Atthree.SceneLoading.PlayFromFirst";
        const string RestoreKey = "Atthree.SceneLoading.RestoreStartScene";

        Vector2 scroll;
        string search = "";
        bool showOthers = true;
        string[] projectScenes;

        [MenuItem("Window/Atthree/Scenes", priority = 2010)]
        public static void Open() => GetWindow<ScenesWindow>("Scenes").minSize = new Vector2(320, 240);

        void OnEnable()
        {
            EditorBuildSettings.sceneListChanged += Refresh;
            EditorApplication.projectChanged += Refresh;
            Refresh();
        }

        void OnDisable()
        {
            EditorBuildSettings.sceneListChanged -= Refresh;
            EditorApplication.projectChanged -= Refresh;
        }

        void Refresh()
        {
            projectScenes = AssetDatabase.FindAssets("t:SceneAsset", new[] { "Assets" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .OrderBy(p => p)
                .ToArray();
            Repaint();
        }

        // ---------------------------------------------------------------- play from first scene

        static string ProjectKey(string key) => key + "." + Application.dataPath.GetHashCode();

        public static bool PlayFromFirstScene
        {
            get => EditorPrefs.GetBool(ProjectKey(PlayFromFirstKey), false);
            set
            {
                EditorPrefs.SetBool(ProjectKey(PlayFromFirstKey), value);
                ApplyStartScene();
            }
        }

        [InitializeOnLoadMethod]
        static void Init()
        {
            EditorBuildSettings.sceneListChanged += ApplyStartScene;
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(RestoreKey, false))
                {
                    SessionState.SetBool(RestoreKey, false);
                    ApplyStartScene();
                }
            };
            EditorApplication.delayCall += ApplyStartScene;
        }

        /// <summary>Sets EditorSceneManager.playModeStartScene from the toggle.</summary>
        static void ApplyStartScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode && SessionState.GetBool(RestoreKey, false)) return;
            SceneAsset first = null;
            if (PlayFromFirstScene)
            {
                var entry = EditorBuildSettings.scenes.FirstOrDefault(s => s.enabled && File.Exists(s.path));
                if (entry != null) first = AssetDatabase.LoadAssetAtPath<SceneAsset>(entry.path);
            }
            EditorSceneManager.playModeStartScene = first;
        }

        /// <summary>Enter Play mode starting from this scene, without opening it.</summary>
        public static void PlayFrom(string path)
        {
            var asset = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
            if (asset == null) return;
            EditorSceneManager.playModeStartScene = asset;
            SessionState.SetBool(RestoreKey, true);
            EditorApplication.EnterPlaymode();
        }

        public static void OpenScene(string path, bool additive)
        {
            if (!additive && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(path, additive ? OpenSceneMode.Additive : OpenSceneMode.Single);
        }

        // ---------------------------------------------------------------- GUI

        void OnGUI()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                bool pf = GUILayout.Toggle(PlayFromFirstScene,
                    new GUIContent(" Play From First Scene", "Play mode always starts in the first scene of the build list (e.g. your Boot scene). The open scenes stay open in the editor."),
                    EditorStyles.toolbarButton);
                if (pf != PlayFromFirstScene) PlayFromFirstScene = pf;
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(new GUIContent("Add Open Scenes", "Add every open (saved) scene to the build list."), EditorStyles.toolbarButton))
                {
                    for (int i = 0; i < EditorSceneManager.sceneCount; i++)
                        SceneBuildList.Add(EditorSceneManager.GetSceneAt(i).path);
                }
                if (GUILayout.Button("Build Profiles", EditorStyles.toolbarButton))
                    EditorApplication.ExecuteMenuItem("File/Build Profiles");
            }

            scroll = EditorGUILayout.BeginScrollView(scroll);
            DrawBuildList();
            EditorGUILayout.Space(8);
            DrawOtherScenes();
            EditorGUILayout.EndScrollView();
        }

        void DrawBuildList()
        {
            EditorGUILayout.LabelField("Build list", EditorStyles.boldLabel);
            var scenes = EditorBuildSettings.scenes;
            if (scenes.Length == 0)
            {
                EditorGUILayout.HelpBox("No scenes in the build list. Add them below or with Add Open Scenes.", MessageType.Info);
                return;
            }

            int index = 0;
            for (int i = 0; i < scenes.Length; i++)
            {
                var s = scenes[i];
                bool exists = File.Exists(s.path);
                using (new EditorGUILayout.HorizontalScope())
                {
                    bool en = EditorGUILayout.Toggle(s.enabled, GUILayout.Width(16));
                    if (en != s.enabled) SetEnabled(i, en);
                    GUILayout.Label(s.enabled && exists ? (index++).ToString() : "–", EditorStyles.miniLabel, GUILayout.Width(18));

                    var name = Path.GetFileNameWithoutExtension(s.path);
                    bool isOpen = IsOpen(s.path);
                    var style = isOpen ? EditorStyles.boldLabel : EditorStyles.label;
                    using (new EditorGUI.DisabledScope(!exists))
                    {
                        if (GUILayout.Button(new GUIContent(exists ? name : name + " (missing)", s.path), style, GUILayout.MinWidth(60)))
                            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<SceneAsset>(s.path));
                        GUILayout.FlexibleSpace();
                        DrawSceneButtons(s.path);
                    }
                    using (new EditorGUI.DisabledScope(i == 0))
                        if (GUILayout.Button(new GUIContent("▲", "Move up"), EditorStyles.miniButtonLeft, GUILayout.Width(22))) Move(i, -1);
                    using (new EditorGUI.DisabledScope(i == scenes.Length - 1))
                        if (GUILayout.Button(new GUIContent("▼", "Move down"), EditorStyles.miniButtonMid, GUILayout.Width(22))) Move(i, +1);
                    if (GUILayout.Button(new GUIContent("✕", "Remove from the build list"), EditorStyles.miniButtonRight, GUILayout.Width(22)))
                    {
                        SceneBuildList.Remove(s.path);
                        GUIUtility.ExitGUI();
                    }
                }
            }
        }

        void DrawOtherScenes()
        {
            showOthers = EditorGUILayout.Foldout(showOthers, "Other scenes in Assets", true);
            if (!showOthers) return;
            search = EditorGUILayout.TextField(search, EditorStyles.toolbarSearchField);

            var inBuild = new HashSet<string>(EditorBuildSettings.scenes.Select(s => s.path));
            var others = (projectScenes ?? new string[0])
                .Where(p => !inBuild.Contains(p))
                .Where(p => string.IsNullOrEmpty(search) || p.IndexOf(search, System.StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();
            if (others.Count == 0)
            {
                EditorGUILayout.LabelField(string.IsNullOrEmpty(search) ? "Every scene is in the build list." : "No match.", EditorStyles.miniLabel);
                return;
            }
            foreach (var path in others.Take(200))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    var name = Path.GetFileNameWithoutExtension(path);
                    var dir = Path.GetDirectoryName(path)?.Replace('\\', '/');
                    if (GUILayout.Button(new GUIContent(name, path), IsOpen(path) ? EditorStyles.boldLabel : EditorStyles.label, GUILayout.MinWidth(60)))
                        EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<SceneAsset>(path));
                    GUILayout.Label(dir, EditorStyles.miniLabel, GUILayout.MinWidth(40));
                    GUILayout.FlexibleSpace();
                    DrawSceneButtons(path);
                    if (GUILayout.Button(new GUIContent("Add", "Add to the build list"), EditorStyles.miniButton, GUILayout.Width(40)))
                        SceneBuildList.Add(path);
                }
            }
        }

        static void DrawSceneButtons(string path)
        {
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            {
                if (GUILayout.Button(new GUIContent("Open", "Open this scene (asks to save the current one)"), EditorStyles.miniButtonLeft, GUILayout.Width(44)))
                    OpenScene(path, false);
                if (GUILayout.Button(new GUIContent("+", "Open additively, next to the open scenes"), EditorStyles.miniButtonMid, GUILayout.Width(22)))
                    OpenScene(path, true);
                if (GUILayout.Button(new GUIContent("▶", "Play starting from this scene"), EditorStyles.miniButtonRight, GUILayout.Width(24)))
                    PlayFrom(path);
            }
        }

        static bool IsOpen(string path)
        {
            for (int i = 0; i < EditorSceneManager.sceneCount; i++)
                if (EditorSceneManager.GetSceneAt(i).path == path) return true;
            return false;
        }

        static void SetEnabled(int i, bool enabled)
        {
            var list = EditorBuildSettings.scenes;
            list[i] = new EditorBuildSettingsScene(list[i].path, enabled);
            EditorBuildSettings.scenes = list;
        }

        static void Move(int i, int dir)
        {
            var list = EditorBuildSettings.scenes.ToList();
            var item = list[i];
            list.RemoveAt(i);
            list.Insert(i + dir, item);
            EditorBuildSettings.scenes = list.ToArray();
            GUIUtility.ExitGUI();
        }
    }
}

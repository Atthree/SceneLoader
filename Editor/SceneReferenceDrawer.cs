using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Atthree.SceneLoading
{
    /// <summary>Shows a SceneReference as a scene asset field, with an "Add to Build" button when needed.</summary>
    [CustomPropertyDrawer(typeof(SceneReference))]
    public class SceneReferenceDrawer : PropertyDrawer
    {
        const float ButtonWidth = 92f;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) => EditorGUIUtility.singleLineHeight;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var guidProp = property.FindPropertyRelative("guid");
            var pathProp = property.FindPropertyRelative("path");
            var nameProp = property.FindPropertyRelative("name");

            // find the asset: by GUID (survives moves/renames), else by path
            SceneAsset asset = null;
            if (!string.IsNullOrEmpty(guidProp.stringValue))
                asset = AssetDatabase.LoadAssetAtPath<SceneAsset>(AssetDatabase.GUIDToAssetPath(guidProp.stringValue));
            if (asset == null && !string.IsNullOrEmpty(pathProp.stringValue))
                asset = AssetDatabase.LoadAssetAtPath<SceneAsset>(pathProp.stringValue);

            // keep path and name up to date after a move or rename
            if (asset != null)
            {
                var path = AssetDatabase.GetAssetPath(asset);
                if (pathProp.stringValue != path || nameProp.stringValue != asset.name || string.IsNullOrEmpty(guidProp.stringValue))
                {
                    pathProp.stringValue = path;
                    nameProp.stringValue = asset.name;
                    guidProp.stringValue = AssetDatabase.AssetPathToGUID(path);
                }
            }

            label = EditorGUI.BeginProperty(position, label, property);
            var status = Status(asset, pathProp.stringValue, nameProp.stringValue);
            var fieldRect = position;
            if (status != BuildStatus.Ok) fieldRect.width -= ButtonWidth + 2;

            EditorGUI.BeginChangeCheck();
            var picked = EditorGUI.ObjectField(fieldRect, label, asset, typeof(SceneAsset), false) as SceneAsset;
            if (EditorGUI.EndChangeCheck())
            {
                var path = picked != null ? AssetDatabase.GetAssetPath(picked) : "";
                pathProp.stringValue = path;
                guidProp.stringValue = picked != null ? AssetDatabase.AssetPathToGUID(path) : "";
                nameProp.stringValue = picked != null ? picked.name : "";
            }

            if (status != BuildStatus.Ok)
            {
                var b = new Rect(fieldRect.xMax + 2, position.y, ButtonWidth, position.height);
                switch (status)
                {
                    case BuildStatus.NotInBuild:
                        if (GUI.Button(b, new GUIContent("Add to Build", "This scene isn't in the build list, so it can't be loaded.")))
                            SceneBuildList.Add(pathProp.stringValue);
                        break;
                    case BuildStatus.Disabled:
                        if (GUI.Button(b, new GUIContent("Enable", "This scene is in the build list but unticked.")))
                            SceneBuildList.Add(pathProp.stringValue);
                        break;
                    case BuildStatus.Missing:
                        EditorGUI.HelpBox(b, "Missing", MessageType.Error);
                        break;
                }
            }
            EditorGUI.EndProperty();
        }

        enum BuildStatus { Ok, NotInBuild, Disabled, Missing }

        static BuildStatus Status(SceneAsset asset, string path, string name)
        {
            if (asset == null) return string.IsNullOrEmpty(path) && string.IsNullOrEmpty(name) ? BuildStatus.Ok : BuildStatus.Missing;
            var entry = EditorBuildSettings.scenes.FirstOrDefault(s => s.path == path);
            if (entry == null) return BuildStatus.NotInBuild;
            return entry.enabled ? BuildStatus.Ok : BuildStatus.Disabled;
        }
    }

    /// <summary>Small helpers for the build scene list.</summary>
    public static class SceneBuildList
    {
        /// <summary>Adds (or ticks) a scene in the build list. Returns true if something changed.</summary>
        public static bool Add(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            var list = EditorBuildSettings.scenes.ToList();
            var i = list.FindIndex(s => s.path == path);
            if (i >= 0)
            {
                if (list[i].enabled) return false;
                list[i] = new EditorBuildSettingsScene(path, true);
            }
            else list.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = list.ToArray();
            return true;
        }

        public static bool Remove(string path)
        {
            var list = EditorBuildSettings.scenes.ToList();
            if (list.RemoveAll(s => s.path == path) == 0) return false;
            EditorBuildSettings.scenes = list.ToArray();
            return true;
        }

        public static bool Contains(string path) => EditorBuildSettings.scenes.Any(s => s.path == path && s.enabled);
    }
}

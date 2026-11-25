using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Atthree.SceneLoading
{
    /// <summary>
    /// A scene you can drag into a field in the Inspector, instead of typing its name.
    /// Keeps the scene's path and name, so it also works in builds.
    /// </summary>
    /// <example><code>
    /// public SceneReference level;
    /// void Start() => SceneLoader.Load(level);
    /// </code></example>
    [Serializable]
    public class SceneReference : IEquatable<SceneReference>
    {
        [SerializeField] string guid = "";
        [SerializeField] string path = "";
        [SerializeField] string name = "";

        public SceneReference() { }

        /// <summary>From a path ("Assets/Scenes/Level1.unity") or a scene name ("Level1").</summary>
        public SceneReference(string pathOrName)
        {
            if (string.IsNullOrEmpty(pathOrName)) return;
            if (pathOrName.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
            {
                path = pathOrName.Replace('\\', '/');
                name = System.IO.Path.GetFileNameWithoutExtension(path);
            }
            else name = pathOrName;
        }

        /// <summary>Assets/.../Level1.unity (empty if made from a name only).</summary>
        public string Path => path;

        /// <summary>Level1</summary>
        public string Name => name;

        /// <summary>The asset's GUID (editor use; keeps the reference when the scene is moved).</summary>
        public string Guid => guid;

        /// <summary>Something was assigned.</summary>
        public bool IsSet => !string.IsNullOrEmpty(path) || !string.IsNullOrEmpty(name);

        /// <summary>Index in the build list, or -1 if the scene isn't in it.</summary>
        public int BuildIndex
        {
            get
            {
                if (!string.IsNullOrEmpty(path))
                {
                    int i = SceneUtility.GetBuildIndexByScenePath(path);
                    if (i >= 0) return i;
                }
                if (!string.IsNullOrEmpty(name))
                {
                    for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
                    {
                        if (System.IO.Path.GetFileNameWithoutExtension(SceneUtility.GetScenePathByBuildIndex(i)) == name)
                            return i;
                    }
                }
                return -1;
            }
        }

        /// <summary>The scene is in the build list and can be loaded.</summary>
        public bool IsInBuild => BuildIndex >= 0;

        /// <summary>What SceneManager gets: the path if it's in the build, otherwise the name.</summary>
        public string LoadKey
        {
            get
            {
                if (!string.IsNullOrEmpty(path) && SceneUtility.GetBuildIndexByScenePath(path) >= 0) return path;
                int i = BuildIndex;
                return i >= 0 ? SceneUtility.GetScenePathByBuildIndex(i) : (string.IsNullOrEmpty(path) ? name : path);
            }
        }

        /// <summary>The loaded scene, if it is loaded right now.</summary>
        public Scene LoadedScene
        {
            get
            {
                var s = string.IsNullOrEmpty(path) ? default : SceneManager.GetSceneByPath(path);
                if (!s.IsValid() && !string.IsNullOrEmpty(name)) s = SceneManager.GetSceneByName(name);
                return s;
            }
        }

        public bool IsLoaded => LoadedScene.isLoaded;

        public static implicit operator SceneReference(string pathOrName) => new SceneReference(pathOrName);

        public override string ToString() => string.IsNullOrEmpty(name) ? "(no scene)" : name;

        public bool Equals(SceneReference other)
        {
            if (other is null) return false;
            if (!string.IsNullOrEmpty(path) && !string.IsNullOrEmpty(other.path)) return path == other.path;
            return name == other.name;
        }

        public override bool Equals(object obj) => obj is SceneReference other && Equals(other);
        public override int GetHashCode() => (name ?? "").GetHashCode();

#if UNITY_EDITOR
        /// <summary>Editor only: point at a scene asset (used by the drawer and tests).</summary>
        public void SetFromEditor(string assetGuid, string assetPath)
        {
            guid = assetGuid ?? "";
            path = assetPath ?? "";
            name = string.IsNullOrEmpty(path) ? "" : System.IO.Path.GetFileNameWithoutExtension(path);
        }
#endif
    }
}

using System;
using UnityEngine;

namespace Atthree.SceneLoading
{
    public enum SceneActionKind
    {
        /// <summary>Load the scene in the Scene field.</summary>
        LoadScene,
        /// <summary>Load the scene group in the Group field.</summary>
        LoadGroup,
        /// <summary>Load the active scene again.</summary>
        Reload,
        /// <summary>Load the next scene in the build list.</summary>
        LoadNext,
        /// <summary>Quit the game (leave Play mode in the editor).</summary>
        Quit,
    }

    /// <summary>What a scene button/trigger does: shared by the components.</summary>
    [Serializable]
    public class SceneAction
    {
        public SceneActionKind action = SceneActionKind.LoadScene;
        public SceneReference scene = new SceneReference();
        public SceneGroup group;
        [Tooltip("Off = use SceneLoader.DefaultOptions (a short black fade).")]
        public bool customOptions;
        public LoadOptions options = new LoadOptions();

        public SceneLoadOperation Run()
        {
            var o = customOptions ? options : null;
            switch (action)
            {
                case SceneActionKind.LoadScene: return SceneLoader.Load(scene, o);
                case SceneActionKind.LoadGroup: return SceneLoader.Load(group, o);
                case SceneActionKind.Reload: return SceneLoader.Reload(o);
                case SceneActionKind.LoadNext: return SceneLoader.LoadNext(o);
                case SceneActionKind.Quit: SceneLoader.Quit(o); return null;
            }
            return null;
        }

        public override string ToString() => action switch
        {
            SceneActionKind.LoadScene => $"Load {scene}",
            SceneActionKind.LoadGroup => $"Load group {(group != null ? group.name : "(none)")}",
            _ => action.ToString(),
        };
    }
}

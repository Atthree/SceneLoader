using System.Collections.Generic;
using UnityEngine;

namespace Atthree.SceneLoading
{
    /// <summary>
    /// Scenes that are loaded together, e.g. a level split into Geometry + Lighting + Gameplay + UI.
    /// The first one replaces the current scenes, the rest are added on top.
    /// </summary>
    [CreateAssetMenu(menuName = "Atthree/Scene Loading/Scene Group", fileName = "New Scene Group")]
    public class SceneGroup : ScriptableObject
    {
        [Tooltip("Loaded in this order. The first one replaces the open scenes.")]
        public List<SceneReference> scenes = new List<SceneReference>();

        [Tooltip("Which one becomes the active scene (new objects and lighting settings go there).")]
        [Min(0)] public int activeScene;

        public SceneReference ActiveScene =>
            scenes.Count == 0 ? null : scenes[Mathf.Clamp(activeScene, 0, scenes.Count - 1)];

        public static SceneGroup Create(int activeIndex, params SceneReference[] scenes)
        {
            var g = CreateInstance<SceneGroup>();
            g.scenes = new List<SceneReference>(scenes);
            g.activeScene = activeIndex;
            return g;
        }
    }
}

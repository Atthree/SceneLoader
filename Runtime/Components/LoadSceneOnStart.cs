using System.Collections;
using UnityEngine;

namespace Atthree.SceneLoading
{
    /// <summary>
    /// Loads a scene as soon as this scene starts, after an optional delay. For boot and splash scenes:
    /// Boot (managers, PersistentObjects) → Main Menu.
    /// </summary>
    [AddComponentMenu("Atthree/Scene Loading/Load Scene On Start")]
    public class LoadSceneOnStart : MonoBehaviour
    {
        [Tooltip("Seconds to wait first (real time).")]
        [Min(0)] public float delay;
        public SceneAction action = new SceneAction();

        IEnumerator Start()
        {
            if (delay > 0f) yield return new WaitForSecondsRealtime(delay);
            else yield return null; // let every Start in this scene run first
            action.Run();
        }
    }
}

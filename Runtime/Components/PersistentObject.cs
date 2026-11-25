using System.Collections.Generic;
using UnityEngine;

namespace Atthree.SceneLoading
{
    /// <summary>
    /// Keeps this object when scenes change (DontDestroyOnLoad). If you come back to a scene that has
    /// it again, the new copy removes itself, so there's always one: music players, managers, the player.
    /// </summary>
    [AddComponentMenu("Atthree/Scene Loading/Persistent Object")]
    [DefaultExecutionOrder(-10000)]
    [DisallowMultipleComponent]
    public class PersistentObject : MonoBehaviour
    {
        [Tooltip("Objects with the same id are the same object. Empty = the object's name.")]
        [SerializeField] string id = "";

        static readonly Dictionary<string, PersistentObject> alive = new Dictionary<string, PersistentObject>();

        public string Id => string.IsNullOrEmpty(id) ? gameObject.name : id;

        /// <summary>The kept object with this id, if any.</summary>
        public static PersistentObject Find(string id) => alive.TryGetValue(id, out var p) && p != null ? p : null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => alive.Clear();

        void Awake()
        {
            var existing = Find(Id);
            if (existing != null && existing != this)
            {
                // the copy in the freshly loaded scene goes away before its other components start
                gameObject.SetActive(false);
                Destroy(gameObject);
                return;
            }
            alive[Id] = this;
            if (transform.parent != null) transform.SetParent(null, true);
            DontDestroyOnLoad(gameObject);
        }

        void OnDestroy()
        {
            if (alive.TryGetValue(Id, out var p) && p == this) alive.Remove(Id);
        }
    }
}

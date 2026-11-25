using UnityEngine;

namespace Atthree.SceneLoading
{
    /// <summary>
    /// Loads a scene when something with the right tag enters this trigger (3D or 2D): level exits,
    /// doors, portals. The collider must be a trigger and one of the two objects needs a Rigidbody.
    /// </summary>
    [AddComponentMenu("Atthree/Scene Loading/Scene Load Trigger")]
    public class SceneLoadTrigger : MonoBehaviour
    {
        [Tooltip("Only objects with this tag. Empty = anything.")]
        public string requiredTag = "Player";
        [Tooltip("Ignore everything after the first time.")]
        public bool once = true;
        public SceneAction onEnter = new SceneAction();

        bool used;

        void OnTriggerEnter(Collider other) => Enter(other.gameObject);
        void OnTriggerEnter2D(Collider2D other) => Enter(other.gameObject);

        void Enter(GameObject other)
        {
            if (used && once) return;
            if (!Matches(other) || SceneLoader.IsLoading) return;
            used = true;
            onEnter.Run();
        }

        /// <summary>The object has the tag, or its Rigidbody's object has it (a child collider of the player entered).</summary>
        bool Matches(GameObject other)
        {
            if (string.IsNullOrEmpty(requiredTag) || other.CompareTag(requiredTag)) return true;
            var rb = other.GetComponentInParent<Rigidbody>();
            if (rb != null && rb.CompareTag(requiredTag)) return true;
            var rb2 = other.GetComponentInParent<Rigidbody2D>();
            return rb2 != null && rb2.CompareTag(requiredTag);
        }
    }
}

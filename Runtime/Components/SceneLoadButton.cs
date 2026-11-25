using UnityEngine;
using UnityEngine.UI;

namespace Atthree.SceneLoading
{
    /// <summary>
    /// Put it on a UI Button: clicking loads a scene, reloads, goes to the next level or quits.
    /// No code, no OnClick setup needed. You can also call <see cref="Run"/> from any UnityEvent.
    /// </summary>
    [AddComponentMenu("Atthree/Scene Loading/Scene Load Button")]
    public class SceneLoadButton : MonoBehaviour
    {
        public SceneAction onClick = new SceneAction();

        Button button;

        void Awake()
        {
            button = GetComponent<Button>();
            if (button != null) button.onClick.AddListener(Run);
        }

        void OnDestroy()
        {
            if (button != null) button.onClick.RemoveListener(Run);
        }

        public void Run() => onClick.Run();
    }
}

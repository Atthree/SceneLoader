using System;
using UnityEngine;

namespace Atthree.SceneLoading
{
    /// <summary>
    /// A running scene load. <c>yield return</c> it in a coroutine, or listen to <see cref="Completed"/>.
    /// </summary>
    public class SceneLoadOperation : CustomYieldInstruction
    {
        /// <summary>The scene(s) being loaded, for logs and events.</summary>
        public string SceneName { get; }

        /// <summary>Loading progress 0–1 (what the loading screen shows).</summary>
        public float Progress { get; internal set; }

        /// <summary>Finished: the new scene is active and the transition is over (or it failed).</summary>
        public bool IsDone { get; private set; }

        /// <summary>The load didn't happen, see <see cref="Error"/>.</summary>
        public bool Failed => Error != null;
        public string Error { get; private set; }

        /// <summary>Waiting for the player to press a key (<see cref="LoadOptions.waitForInput"/>).</summary>
        public bool WaitingForInput { get; internal set; }

        /// <summary>Called once when done (also when it failed). Added after it's done = called right away.</summary>
        public event Action<SceneLoadOperation> Completed
        {
            add
            {
                if (IsDone) value?.Invoke(this);
                else completed += value;
            }
            remove => completed -= value;
        }
        Action<SceneLoadOperation> completed;

        public override bool keepWaiting => !IsDone;

        internal bool continueRequested;

        internal SceneLoadOperation(string sceneName) => SceneName = sceneName;

        /// <summary>Go on when the loader is waiting for input (e.g. from a "Continue" button).</summary>
        public void Continue() => continueRequested = true;

        internal void Finish(string error = null)
        {
            if (IsDone) return;
            Error = error;
            if (error == null) Progress = 1f;
            IsDone = true;
            var cb = completed;
            completed = null;
            try { cb?.Invoke(this); }
            catch (Exception e) { Debug.LogException(e); }
        }

        public override string ToString() => $"Load {SceneName} ({(IsDone ? (Failed ? "failed" : "done") : $"{Progress:P0}")})";
    }
}

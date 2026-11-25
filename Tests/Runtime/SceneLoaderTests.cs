using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Atthree.SceneLoading.Tests
{
    [PrebuildSetup(typeof(TestScenes))]
    [PostBuildCleanup(typeof(TestScenes))]
    public class SceneLoaderTests
    {
        const string A = "SLTest_A", B = "SLTest_B", C = "SLTest_C";
        static readonly LoadOptions Quick = LoadOptions.Fade(0.05f);

        float volume;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            volume = AudioListener.volume;
            Time.timeScale = 1f;
            // wait for a load left over from a failed test
            float t = 0;
            while (SceneLoader.IsLoading && t < 5f) { t += Time.unscaledDeltaTime; yield return null; }
            yield return SceneLoader.Load(A, LoadOptions.Instant());
        }

        [TearDown]
        public void TearDown()
        {
            AudioListener.volume = volume;
            Time.timeScale = 1f;
            SceneLoader.DefaultOptions = LoadOptions.Fade();
        }

        static bool MarkerExists(string scene) => GameObject.Find("Marker_" + scene) != null;

        static IEnumerator Wait(SceneLoadOperation op, float timeout = 10f)
        {
            float t = 0;
            while (!op.IsDone && t < timeout) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(op.IsDone, $"timed out: {op}");
        }

        // ---------------------------------------------------------------- basic loading

        [UnityTest]
        public IEnumerator Load_ByName_ReplacesScene()
        {
            Assert.IsTrue(MarkerExists(A));
            var op = SceneLoader.Load(B, Quick);
            Assert.IsTrue(SceneLoader.IsLoading);
            yield return op;

            Assert.IsFalse(op.Failed);
            Assert.AreEqual(B, SceneManager.GetActiveScene().name);
            Assert.IsTrue(MarkerExists(B));
            Assert.IsFalse(MarkerExists(A), "the old scene is gone");
            Assert.IsFalse(SceneLoader.IsLoading);
            Assert.AreEqual(1f, op.Progress);
        }

        [UnityTest]
        public IEnumerator Load_BySceneReferencePath()
        {
            yield return SceneLoader.Load(new SceneReference(TestScenes.PathOf(C)), Quick);
            Assert.AreEqual(C, SceneManager.GetActiveScene().name);
        }

        [UnityTest]
        public IEnumerator Events_FireInOrder()
        {
            var log = new List<string>();
            void Started(SceneLoadOperation o) => log.Add("started");
            void Unload(SceneLoadOperation o) => log.Add("unload:" + (MarkerExists(A) ? "old scene still there" : "too late"));
            void Activated(SceneLoadOperation o) => log.Add("activated:" + SceneManager.GetActiveScene().name);
            void Completed(SceneLoadOperation o) => log.Add("completed");
            SceneLoader.LoadStarted += Started;
            SceneLoader.BeforeSceneUnload += Unload;
            SceneLoader.SceneActivated += Activated;
            SceneLoader.LoadCompleted += Completed;
            try
            {
                var op = SceneLoader.Load(B, Quick);
                bool opCompleted = false;
                op.Completed += _ => opCompleted = true;
                yield return op;
                CollectionAssert.AreEqual(new[] { "started", "unload:old scene still there", "activated:" + B, "completed" }, log);
                Assert.IsTrue(opCompleted);

                bool late = false;
                op.Completed += _ => late = true;
                Assert.IsTrue(late, "subscribing after it's done calls right away");
            }
            finally
            {
                SceneLoader.LoadStarted -= Started;
                SceneLoader.BeforeSceneUnload -= Unload;
                SceneLoader.SceneActivated -= Activated;
                SceneLoader.LoadCompleted -= Completed;
            }
        }

        [UnityTest]
        public IEnumerator SecondLoad_WhileLoading_IsIgnored()
        {
            var first = SceneLoader.Load(B, Quick);
            LogAssert.Expect(LogType.Warning, new Regex("already loading"));
            var second = SceneLoader.Load(C, Quick);
            Assert.IsNull(second);
            yield return first;
            Assert.AreEqual(B, SceneManager.GetActiveScene().name);
        }

        [Test]
        public void MissingScene_FailsRightAway()
        {
            LogAssert.Expect(LogType.Error, new Regex("isn't in the build list"));
            var op = SceneLoader.Load("NoSuchScene_123", Quick);
            Assert.IsTrue(op.IsDone);
            Assert.IsTrue(op.Failed);
            Assert.IsFalse(SceneLoader.IsLoading);
        }

        // ---------------------------------------------------------------- transition

        [UnityTest]
        public IEnumerator Fade_CoversDuringLoad_AndIsRemoved()
        {
            float alphaAtActivation = -1f;
            void Activated(SceneLoadOperation o)
            {
                var fade = Object.FindAnyObjectByType<FadeTransition>();
                alphaAtActivation = fade != null ? fade.GetComponentInChildren<CanvasGroup>().alpha : -1f;
            }
            SceneLoader.SceneActivated += Activated;
            try
            {
                yield return SceneLoader.Load(B, LoadOptions.Fade(0.1f, Color.red));
            }
            finally { SceneLoader.SceneActivated -= Activated; }

            Assert.AreEqual(1f, alphaAtActivation, 0.001f, "screen fully covered when the scene switches");
            yield return null; // Destroy happens at the end of the frame
            Assert.IsNull(Object.FindAnyObjectByType<FadeTransition>(), "transition object removed");
        }

        [UnityTest]
        public IEnumerator Fade_TakesAboutItsDuration()
        {
            float start = Time.realtimeSinceStartup;
            yield return SceneLoader.Load(B, LoadOptions.Fade(0.3f));
            float took = Time.realtimeSinceStartup - start;
            Assert.GreaterOrEqual(took, 0.55f, "0.3 s out + 0.3 s in");
            Assert.Less(took, 3f);
        }

        [UnityTest]
        public IEnumerator Instant_HasNoTransition()
        {
            bool sawTransition = false;
            void Activated(SceneLoadOperation o) => sawTransition = Object.FindAnyObjectByType<SceneTransition>() != null;
            SceneLoader.SceneActivated += Activated;
            try { yield return SceneLoader.Load(B, LoadOptions.Instant()); }
            finally { SceneLoader.SceneActivated -= Activated; }
            Assert.IsFalse(sawTransition);
        }

        [UnityTest]
        public IEnumerator Audio_FadesOutAndBack()
        {
            AudioListener.volume = 0.8f;
            float atActivation = -1f;
            void Activated(SceneLoadOperation o) => atActivation = AudioListener.volume;
            SceneLoader.SceneActivated += Activated;
            try { yield return SceneLoader.Load(B, Quick); }
            finally { SceneLoader.SceneActivated -= Activated; }
            Assert.AreEqual(0f, atActivation, 0.001f);
            Assert.AreEqual(0.8f, AudioListener.volume, 0.001f);
        }

        [UnityTest]
        public IEnumerator TimeScale_IsReset()
        {
            Time.timeScale = 0f; // e.g. loading from a pause menu
            yield return SceneLoader.Load(B, Quick);
            Assert.AreEqual(1f, Time.timeScale);
        }

        [UnityTest]
        public IEnumerator TimeScale_KeptWhenAsked()
        {
            Time.timeScale = 0.5f;
            var o = Quick.Clone();
            o.resetTimeScale = false;
            yield return SceneLoader.Load(B, o);
            Assert.AreEqual(0.5f, Time.timeScale);
        }

        // ---------------------------------------------------------------- loading screen

        [UnityTest]
        public IEnumerator LoadingScreen_ShowsProgress_AndKeepsMinTime()
        {
            var o = LoadOptions.WithLoadingScreen(0.6f);
            o.fadeOutDuration = o.fadeInDuration = 0.05f;
            var seen = new List<float>();
            void Progress(float p) => seen.Add(p);
            SceneLoader.ProgressChanged += Progress;

            bool sawScreen = false;
            float start = Time.realtimeSinceStartup;
            var op = SceneLoader.Load(B, o);
            try
            {
                while (!op.IsDone)
                {
                    sawScreen |= Object.FindAnyObjectByType<LoadingScreen>() != null;
                    yield return null;
                }
            }
            finally { SceneLoader.ProgressChanged -= Progress; }

            Assert.IsTrue(sawScreen, "the built-in screen appeared");
            Assert.GreaterOrEqual(Time.realtimeSinceStartup - start, 0.6f);
            Assert.Greater(seen.Count, 3, "progress reported several times");
            for (int i = 1; i < seen.Count; i++) Assert.GreaterOrEqual(seen[i], seen[i - 1], "never goes backwards");
            Assert.AreEqual(1f, seen[seen.Count - 1]);
            yield return null;
            Assert.IsNull(Object.FindAnyObjectByType<LoadingScreen>(), "screen removed");
        }

        [UnityTest]
        public IEnumerator LoadingScreen_DefaultHasBarAndText()
        {
            var screen = LoadingScreen.CreateDefault();
            screen.Show();
            screen.SetProgress(0.42f);
            Assert.AreEqual(0.42f, screen.progressBar.fillAmount, 0.001f);
            Assert.AreEqual("42%", screen.progressText.text);
            Assert.IsFalse(screen.continuePrompt.activeSelf);
            screen.ShowContinuePrompt();
            Assert.IsTrue(screen.continuePrompt.activeSelf);
            Object.Destroy(screen.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator WaitForInput_HoldsUntilContinue()
        {
            var o = LoadOptions.WithLoadingScreen(0f, waitForInput: true);
            o.fadeOutDuration = o.fadeInDuration = 0.02f;
            var op = SceneLoader.Load(B, o);

            float t = 0;
            while (!op.WaitingForInput && t < 5f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(op.WaitingForInput);
            for (int i = 0; i < 10; i++) yield return null;
            Assert.IsFalse(op.IsDone, "still waiting");
            Assert.AreNotEqual(B, SceneManager.GetActiveScene().name, "the new scene isn't shown yet");
            Assert.IsTrue(Object.FindAnyObjectByType<LoadingScreen>().continuePrompt.activeSelf);

            SceneLoader.Continue();
            yield return Wait(op);
            Assert.AreEqual(B, SceneManager.GetActiveScene().name);
        }

        // ---------------------------------------------------------------- other loads

        [UnityTest]
        public IEnumerator Reload_MakesFreshScene()
        {
            var marker = GameObject.Find("Marker_" + A);
            yield return SceneLoader.Reload(Quick);
            Assert.AreEqual(A, SceneManager.GetActiveScene().name);
            Assert.IsTrue(marker == null, "old objects destroyed");
            Assert.IsTrue(MarkerExists(A), "new objects created");
        }

        [UnityTest]
        public IEnumerator LoadNext_FollowsBuildOrder_AndWraps()
        {
            yield return SceneLoader.LoadNext(Quick);
            Assert.AreEqual(B, SceneManager.GetActiveScene().name);
            yield return SceneLoader.LoadNext(Quick);
            Assert.AreEqual(C, SceneManager.GetActiveScene().name);

            int last = SceneManager.sceneCountInBuildSettings - 1;
            yield return SceneLoader.LoadBuildIndex(last, Quick);
            yield return SceneLoader.LoadNext(Quick);
            Assert.AreEqual(0, SceneManager.GetActiveScene().buildIndex, "after the last comes the first");
        }

        [UnityTest]
        public IEnumerator Group_LoadsAll_AndSetsActive()
        {
            var group = SceneGroup.Create(1, B, C);
            yield return SceneLoader.Load(group, Quick);
            Assert.IsTrue(MarkerExists(B));
            Assert.IsTrue(MarkerExists(C));
            Assert.IsFalse(MarkerExists(A));
            Assert.AreEqual(C, SceneManager.GetActiveScene().name);
            Assert.AreEqual(2, SceneManager.sceneCount);
            Object.Destroy(group);
        }

        [UnityTest]
        public IEnumerator Additive_LoadAndUnload()
        {
            var op = SceneLoader.LoadAdditive(B);
            yield return Wait(op);
            Assert.IsTrue(MarkerExists(A));
            Assert.IsTrue(MarkerExists(B));
            Assert.AreEqual(A, SceneManager.GetActiveScene().name, "active scene unchanged");

            op = SceneLoader.Unload(B);
            yield return Wait(op);
            Assert.IsFalse(MarkerExists(B));
            Assert.AreEqual(1, SceneManager.sceneCount);
        }

        [Test]
        public void Unload_LastScene_IsRefused()
        {
            LogAssert.Expect(LogType.Warning, new Regex("only open scene"));
            var op = SceneLoader.Unload(A);
            Assert.IsTrue(op.Failed);
        }

        // ---------------------------------------------------------------- scene reference

        [Test]
        public void SceneReference_FindsBuildIndex()
        {
            var byPath = new SceneReference(TestScenes.PathOf(B));
            Assert.AreEqual(B, byPath.Name);
            Assert.AreEqual(1, byPath.BuildIndex);
            Assert.IsTrue(byPath.IsInBuild);

            SceneReference byName = C;
            Assert.AreEqual(2, byName.BuildIndex);
            Assert.AreEqual(TestScenes.PathOf(C), byName.LoadKey);

            Assert.IsFalse(new SceneReference("Nope").IsInBuild);
            Assert.IsFalse(new SceneReference().IsSet);
            Assert.AreEqual(byPath, new SceneReference(B), "path and name refer to the same scene");
        }

        [Test]
        public void SceneReference_KnowsIfLoaded()
        {
            Assert.IsTrue(new SceneReference(A).IsLoaded);
            Assert.IsFalse(new SceneReference(B).IsLoaded);
        }

        // ---------------------------------------------------------------- components

        [UnityTest]
        public IEnumerator PersistentObject_SurvivesAndStaysUnique()
        {
            var first = new GameObject("Music");
            first.AddComponent<PersistentObject>();
            yield return SceneLoader.Load(B, Quick);
            Assert.IsTrue(first != null, "kept across the load");

            var copy = new GameObject("Music");
            copy.AddComponent<PersistentObject>();
            yield return null;
            Assert.IsTrue(copy == null, "the second one removes itself");
            Assert.AreEqual(first.GetComponent<PersistentObject>(), PersistentObject.Find("Music"));
            Object.Destroy(first);
            yield return null;
        }

        [UnityTest]
        public IEnumerator SceneLoadButton_LoadsOnClick()
        {
            var go = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button));
            var b = go.AddComponent<SceneLoadButton>();
            b.onClick.action = SceneActionKind.LoadScene;
            b.onClick.scene = B;
            b.onClick.customOptions = true;
            b.onClick.options = Quick;
            yield return null;

            go.GetComponent<Button>().onClick.Invoke();
            Assert.IsTrue(SceneLoader.IsLoading);
            yield return Wait(SceneLoader.Current);
            Assert.AreEqual(B, SceneManager.GetActiveScene().name);
        }

        [UnityTest]
        public IEnumerator SceneLoadTrigger_LoadsWhenPlayerEnters()
        {
            var trigger = GameObject.CreatePrimitive(PrimitiveType.Cube);
            trigger.GetComponent<Collider>().isTrigger = true;
            var t = trigger.AddComponent<SceneLoadTrigger>();
            t.onEnter.scene = C;
            t.onEnter.customOptions = true;
            t.onEnter.options = Quick;

            var other = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            other.transform.position = new Vector3(5, 0, 0);
            var rb = other.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            yield return new WaitForFixedUpdate();
            rb.position = Vector3.zero;          // untagged: ignored
            for (int i = 0; i < 4; i++) yield return new WaitForFixedUpdate();
            Assert.IsFalse(SceneLoader.IsLoading, "untagged objects don't count");

            rb.position = new Vector3(5, 0, 0);
            for (int i = 0; i < 2; i++) yield return new WaitForFixedUpdate();
            other.tag = "Player";
            rb.position = Vector3.zero;
            float time = 0;
            while (!SceneLoader.IsLoading && time < 2f) { time += Time.deltaTime; yield return new WaitForFixedUpdate(); }
            Assert.IsTrue(SceneLoader.IsLoading, "the player triggers it");
            yield return Wait(SceneLoader.Current);
            Assert.AreEqual(C, SceneManager.GetActiveScene().name);
        }

        [UnityTest]
        public IEnumerator LoadSceneOnStart_Loads()
        {
            var go = new GameObject("Boot");
            go.SetActive(false);
            var l = go.AddComponent<LoadSceneOnStart>();
            l.action.scene = B;
            l.action.customOptions = true;
            l.action.options = Quick;
            go.SetActive(true);

            float t = 0;
            while (SceneManager.GetActiveScene().name != B && t < 5f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.AreEqual(B, SceneManager.GetActiveScene().name);
            while (SceneLoader.IsLoading) yield return null;
        }

        [UnityTest]
        public IEnumerator DefaultOptions_AreUsed()
        {
            SceneLoader.DefaultOptions = LoadOptions.Instant();
            bool sawTransition = false;
            void Activated(SceneLoadOperation o) => sawTransition = Object.FindAnyObjectByType<SceneTransition>() != null;
            SceneLoader.SceneActivated += Activated;
            try { yield return SceneLoader.Load(B); }
            finally { SceneLoader.SceneActivated -= Activated; }
            Assert.IsFalse(sawTransition);
        }
    }
}

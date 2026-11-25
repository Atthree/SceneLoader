using UnityEngine;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace Atthree.SceneLoading.Tests
{
    /// <summary>
    /// Makes three small scenes (each with a "Marker_X" object) and puts them in the build list before
    /// the PlayMode tests run; removes them afterwards.
    /// </summary>
    public class TestScenes : IPrebuildSetup, IPostBuildCleanup
    {
        public const string Folder = "Assets/__SceneLoaderTests";
        public static readonly string[] Names = { "SLTest_A", "SLTest_B", "SLTest_C" };

        public static string PathOf(string name) => $"{Folder}/{name}.unity";

        public void Setup()
        {
#if UNITY_EDITOR
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets", "__SceneLoaderTests");
            var setup = EditorSceneManager.GetSceneManagerSetup();
            foreach (var name in Names)
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                new GameObject("Marker_" + name);
                EditorSceneManager.SaveScene(scene, PathOf(name));
            }
            var list = EditorBuildSettings.scenes.Where(s => !s.path.StartsWith(Folder)).ToList();
            // test scenes first, so build index order is A, B, C
            list.InsertRange(0, Names.Select(n => new EditorBuildSettingsScene(PathOf(n), true)));
            EditorBuildSettings.scenes = list.ToArray();
            if (setup.Any(s => !string.IsNullOrEmpty(s.path))) EditorSceneManager.RestoreSceneManagerSetup(setup.Where(s => !string.IsNullOrEmpty(s.path)).ToArray());
            else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
#endif
        }

        public void Cleanup()
        {
#if UNITY_EDITOR
            EditorBuildSettings.scenes = EditorBuildSettings.scenes.Where(s => !s.path.StartsWith(Folder)).ToArray();
            AssetDatabase.DeleteAsset(Folder);
#endif
        }
    }
}

#if UNITY_EDITOR
using ForgettingBoxer.Knockout;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class KnockoutTestSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/KnockoutTest.unity";

    static KnockoutTestSceneBuilder()
    {
        EditorApplication.delayCall += CreateTestSceneIfMissing;
    }

    private static void CreateTestSceneIfMissing()
    {
        bool missingOrOutdated = !System.IO.File.Exists(ScenePath) ||
                                 !System.IO.File.ReadAllText(ScenePath).Contains("starTemplate:");
        if (!EditorApplication.isPlayingOrWillChangePlaymode && missingOrOutdated)
            Build();
    }

    [MenuItem("Tools/Knockout/Create or Rebuild Test Scene")]
    public static void Build()
    {
        var previousScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var mode = Application.isBatchMode ? NewSceneMode.Single : NewSceneMode.Additive;
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, mode);

        var camera = new GameObject("Main Camera", typeof(Camera));
        camera.tag = "MainCamera";
        camera.transform.SetPositionAndRotation(new Vector3(0f, 8f, -10f), Quaternion.Euler(32f, 0f, 0f));
        camera.GetComponent<Camera>().backgroundColor = new Color(0.07f, 0.09f, 0.14f);

        var light = new GameObject("Directional Light", typeof(Light));
        light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        light.GetComponent<Light>().type = LightType.Directional;

        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Paused World (watch rotation stop)";
        ground.transform.localScale = new Vector3(2f, 1f, 2f);
        ground.AddComponent<SpinForKnockoutTest>();

        var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Test Player";
        player.transform.position = new Vector3(0f, 1f, 0f);

        if (AssetDatabase.LoadAssetAtPath<GameObject>(KnockoutPrefabBuilder.PrefabPath) == null)
            KnockoutPrefabBuilder.BuildPrefab();

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(KnockoutPrefabBuilder.PrefabPath);
        var systemGo = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        systemGo.name = "Knockout System + Inspector Tests";
        KnockoutSystem system = systemGo.GetComponent<KnockoutSystem>();
        systemGo.AddComponent<KnockoutTestControls>();

        var serializedSystem = new SerializedObject(system);
        serializedSystem.FindProperty("player").objectReferenceValue = player;
        serializedSystem.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = AppendScene(EditorBuildSettings.scenes, ScenePath);
        AssetDatabase.SaveAssets();
        if (!Application.isBatchMode && previousScene.IsValid())
        {
            EditorSceneManager.SetActiveScene(previousScene);
            EditorSceneManager.CloseScene(scene, true);
        }
        else
        {
            Selection.activeGameObject = systemGo;
        }
        Debug.Log($"Created {ScenePath}. Enter Play Mode and use the inspector test buttons.");
    }

    private static EditorBuildSettingsScene[] AppendScene(EditorBuildSettingsScene[] scenes, string path)
    {
        foreach (var scene in scenes)
            if (scene.path == path) return scenes;
        var result = new EditorBuildSettingsScene[scenes.Length + 1];
        scenes.CopyTo(result, 0);
        result[^1] = new EditorBuildSettingsScene(path, true);
        return result;
    }

}

[CustomEditor(typeof(KnockoutTestControls))]
public sealed class KnockoutTestControlsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("Buttons work in Play Mode. Add several stars, then deal damage and click every scattered star before the timer reaches zero.", MessageType.Info);
        using (new EditorGUI.DisabledScope(!Application.isPlaying))
        {
            var controls = (KnockoutTestControls)target;
            if (GUILayout.Button("Add Stars")) controls.TestAddStars();
            if (GUILayout.Button("Take Damage")) controls.TestTakeDamage();
            if (GUILayout.Button("Reset Run")) controls.TestResetRun();
        }
    }
}
#endif

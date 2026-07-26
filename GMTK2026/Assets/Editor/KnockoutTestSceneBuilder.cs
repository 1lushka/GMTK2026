#if UNITY_EDITOR
using ForgettingBoxer.Knockout;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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

        var systemGo = new GameObject("Knockout System + Inspector Tests");
        var system = systemGo.AddComponent<KnockoutSystem>();
        systemGo.AddComponent<KnockoutTestControls>();

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        var canvasGo = new GameObject("Knockout UI (EDIT ME)", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform collectSpawn = CreateRect("Collect Spawn Point (center)", canvasGo.transform);
        collectSpawn.anchorMin = collectSpawn.anchorMax = new Vector2(0.5f, 0.5f);
        collectSpawn.sizeDelta = new Vector2(20f, 20f);

        RectTransform counter = CreateRect("Star Counter", canvasGo.transform);
        counter.anchorMin = counter.anchorMax = new Vector2(1f, 1f);
        counter.pivot = new Vector2(1f, 1f);
        counter.sizeDelta = new Vector2(220f, 80f);
        counter.anchoredPosition = new Vector2(-35f, -25f);
        var counterGroup = counter.gameObject.AddComponent<CanvasGroup>();
        TMP_Text counterText = CreateText("Counter Text", counter, 48f, TextAlignmentOptions.Center);
        Stretch(counterText.rectTransform);
        counterText.color = new Color(1f, 0.72f, 0.05f);
        RectTransform counterPoint = CreateRect("Landing + Launch Point", counter);
        counterPoint.anchorMin = counterPoint.anchorMax = new Vector2(0.5f, 0.5f);
        counterPoint.sizeDelta = new Vector2(20f, 20f);

        TMP_Text timer = CreateText("Knockout Timer", canvasGo.transform, 120f, TextAlignmentOptions.Center);
        timer.rectTransform.anchorMin = timer.rectTransform.anchorMax = new Vector2(0.5f, 0.82f);
        timer.rectTransform.sizeDelta = new Vector2(400f, 160f);
        timer.gameObject.SetActive(false);

        TMP_Text status = CreateText("Status", canvasGo.transform, 40f, TextAlignmentOptions.Center);
        status.rectTransform.anchorMin = status.rectTransform.anchorMax = new Vector2(0.5f, 0.68f);
        status.rectTransform.sizeDelta = new Vector2(1000f, 100f);
        status.color = new Color(1f, 0.35f, 0.12f);

        RectTransform flyingRoot = CreateRect("Flying Stars (top interaction layer)", canvasGo.transform);
        Stretch(flyingRoot);
        var interactionCanvas = flyingRoot.gameObject.AddComponent<Canvas>();
        interactionCanvas.overrideSorting = true;
        interactionCanvas.sortingOrder = 1000;
        flyingRoot.gameObject.AddComponent<GraphicRaycaster>();
        var templateGo = new GameObject("Star Template (EDIT ME)", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        var templateRect = (RectTransform)templateGo.transform;
        templateRect.SetParent(flyingRoot, false);
        templateRect.sizeDelta = new Vector2(72f, 72f);
        var templateImage = templateGo.GetComponent<Image>();
        templateImage.color = new Color(1f, 0.72f, 0.05f, 1f);
        var templateButton = templateGo.GetComponent<Button>();
        templateButton.targetGraphic = templateImage;
        TMP_Text starLabel = CreateText("Star Graphic", templateRect, 52f, TextAlignmentOptions.Center);
        Stretch(starLabel.rectTransform);
        starLabel.text = "★";
        starLabel.color = new Color(0.35f, 0.16f, 0.02f);
        starLabel.raycastTarget = false;
        templateGo.SetActive(false);

        var serializedSystem = new SerializedObject(system);
        serializedSystem.FindProperty("player").objectReferenceValue = player;
        serializedSystem.FindProperty("starCounterPoint").objectReferenceValue = counterPoint;
        serializedSystem.FindProperty("collectSpawnPoint").objectReferenceValue = collectSpawn;
        serializedSystem.FindProperty("flyingStarsRoot").objectReferenceValue = flyingRoot;
        serializedSystem.FindProperty("starTemplate").objectReferenceValue = templateButton;
        serializedSystem.FindProperty("starCounterGroup").objectReferenceValue = counterGroup;
        serializedSystem.FindProperty("starCounterText").objectReferenceValue = counterText;
        serializedSystem.FindProperty("timerText").objectReferenceValue = timer;
        serializedSystem.FindProperty("statusText").objectReferenceValue = status;
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

    private static RectTransform CreateRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        return rect;
    }

    private static TMP_Text CreateText(string name, Transform parent, float size, TextAlignmentOptions alignment)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var text = go.GetComponent<TMP_Text>();
        text.fontSize = size;
        text.alignment = alignment;
        text.fontStyle = FontStyles.Bold;
        return text;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
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

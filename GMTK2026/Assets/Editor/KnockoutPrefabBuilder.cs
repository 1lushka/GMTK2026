#if UNITY_EDITOR
using ForgettingBoxer.Knockout;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[InitializeOnLoad]
public static class KnockoutPrefabBuilder
{
    public const string PrefabPath = "Assets/Prefabs/Systems/KnockoutSystem.prefab";

    static KnockoutPrefabBuilder()
    {
        EditorApplication.delayCall += BuildIfMissing;
    }

    private static void BuildIfMissing()
    {
        if (!EditorApplication.isPlayingOrWillChangePlaymode &&
            AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
            BuildPrefab();
    }

    [MenuItem("Tools/Knockout/Create or Rebuild Production Prefab")]
    public static void BuildPrefab()
    {
        EnsureFolder("Assets/Prefabs", "Systems");
        GameObject root = CreateConfiguredRoot();
        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
    }

    public static GameObject CreateConfiguredRoot()
    {
        var root = new GameObject("Knockout System");
        KnockoutSystem system = root.AddComponent<KnockoutSystem>();

        RectTransform canvasRect = CreateCanvas(root.transform);
        RectTransform collectSpawn = CreatePoint("Collect Spawn Point", canvasRect, new Vector2(0.5f, 0.5f));
        CanvasGroup counterGroup = CreateCounter(canvasRect, out TMP_Text counterText, out RectTransform counterPoint);
        TMP_Text timer = CreateTimer(canvasRect);
        TMP_Text status = CreateStatus(canvasRect);
        RectTransform flyingRoot = CreateFlyingLayer(canvasRect, out Button starTemplate);
        CreateEventSystem(root.transform);

        var serialized = new SerializedObject(system);
        SetReference(serialized, "starCounterPoint", counterPoint);
        SetReference(serialized, "collectSpawnPoint", collectSpawn);
        SetReference(serialized, "flyingStarsRoot", flyingRoot);
        SetReference(serialized, "starTemplate", starTemplate);
        SetReference(serialized, "starCounterGroup", counterGroup);
        SetReference(serialized, "starCounterText", counterText);
        SetReference(serialized, "timerText", timer);
        SetReference(serialized, "statusText", status);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return root;
    }

    private static RectTransform CreateCanvas(Transform parent)
    {
        var go = new GameObject("Knockout UI (EDIT ME)", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        go.transform.SetParent(parent, false);
        Canvas canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        return (RectTransform)go.transform;
    }

    private static CanvasGroup CreateCounter(Transform parent, out TMP_Text text, out RectTransform point)
    {
        RectTransform counter = CreateRect("Star Counter", parent);
        counter.anchorMin = counter.anchorMax = Vector2.one;
        counter.pivot = Vector2.one;
        counter.sizeDelta = new Vector2(220f, 80f);
        counter.anchoredPosition = new Vector2(-35f, -25f);
        CanvasGroup group = counter.gameObject.AddComponent<CanvasGroup>();
        text = CreateText("Counter Text", counter, 48f);
        text.color = new Color(1f, 0.72f, 0.05f);
        point = CreatePoint("Landing + Launch Point", counter, new Vector2(0.5f, 0.5f));
        return group;
    }

    private static TMP_Text CreateTimer(Transform parent)
    {
        TMP_Text text = CreateText("Knockout Timer", parent, 120f);
        text.rectTransform.anchorMin = text.rectTransform.anchorMax = new Vector2(0.5f, 0.82f);
        text.rectTransform.sizeDelta = new Vector2(400f, 160f);
        text.gameObject.SetActive(false);
        return text;
    }

    private static TMP_Text CreateStatus(Transform parent)
    {
        TMP_Text text = CreateText("Status", parent, 40f);
        text.rectTransform.anchorMin = text.rectTransform.anchorMax = new Vector2(0.5f, 0.68f);
        text.rectTransform.sizeDelta = new Vector2(1000f, 100f);
        text.color = new Color(1f, 0.35f, 0.12f);
        return text;
    }

    private static RectTransform CreateFlyingLayer(Transform parent, out Button template)
    {
        RectTransform root = CreateRect("Flying Stars (top interaction layer)", parent);
        Stretch(root);
        Canvas canvas = root.gameObject.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = 1000;
        root.gameObject.AddComponent<GraphicRaycaster>();

        var go = new GameObject("Star Template (EDIT ME)", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(root, false);
        rect.sizeDelta = new Vector2(72f, 72f);
        Image image = go.GetComponent<Image>();
        image.color = new Color(1f, 0.72f, 0.05f);
        template = go.GetComponent<Button>();
        template.targetGraphic = image;
        TMP_Text label = CreateText("Star Graphic", rect, 52f);
        label.text = "\u2605";
        label.color = new Color(0.35f, 0.16f, 0.02f);
        label.raycastTarget = false;
        go.SetActive(false);
        return root;
    }

    private static void CreateEventSystem(Transform parent)
    {
        var go = new GameObject("Fallback EventSystem", typeof(EventSystem), typeof(StandaloneInputModule), typeof(KnockoutEventSystemGuard));
        go.transform.SetParent(parent, false);
        var serialized = new SerializedObject(go.GetComponent<KnockoutEventSystemGuard>());
        SetReference(serialized, "localEventSystem", go.GetComponent<EventSystem>());
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static TMP_Text CreateText(string name, Transform parent, float size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        TMP_Text text = go.GetComponent<TMP_Text>();
        text.fontSize = size;
        text.alignment = TextAlignmentOptions.Center;
        text.fontStyle = FontStyles.Bold;
        Stretch(text.rectTransform);
        return text;
    }

    private static RectTransform CreatePoint(string name, Transform parent, Vector2 anchor)
    {
        RectTransform point = CreateRect(name, parent);
        point.anchorMin = point.anchorMax = anchor;
        point.sizeDelta = new Vector2(20f, 20f);
        return point;
    }

    private static RectTransform CreateRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    private static void SetReference(SerializedObject target, string name, Object value)
    {
        target.FindProperty(name).objectReferenceValue = value;
    }

    private static void EnsureFolder(string parent, string name)
    {
        string path = $"{parent}/{name}";
        if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, name);
    }
}
#endif

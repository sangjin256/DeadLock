using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class CanvasBoardReferenceSceneBuilder
{
    private const string ScenePath = "Assets/01.Scenes/LevelPlayCanvasReference.unity";
    private const float ReferenceWidth = 1920f;
    private const float ReferenceHeight = 1080f;

    private static readonly Color BackgroundColor = new Color(0.91f, 0.94f, 0.93f);
    private static readonly Color InkColor = new Color(0.10f, 0.15f, 0.18f);
    private static readonly Color ShadowColor = new Color(0.10f, 0.15f, 0.18f, 0.24f);
    private static readonly Color SurfaceColor = new Color(0.93f, 0.97f, 0.97f);
    private static readonly Color TealColor = new Color(0.12f, 0.77f, 0.68f);
    private static readonly Color BlueColor = new Color(0.39f, 0.60f, 0.94f);
    private static readonly Color LimeColor = new Color(0.69f, 0.81f, 0.17f);
    private static readonly Color OrangeColor = new Color(1.00f, 0.66f, 0.20f);
    private static readonly Color RedColor = new Color(0.95f, 0.35f, 0.36f);

    [MenuItem("Tools/DeadLock/Visuals/Create Canvas Board Reference Scene")]
    public static void CreateCanvasBoardReferenceScene()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
        {
            Debug.LogError("[CanvasBoardReferenceSceneBuilder] LevelPlayCanvasReference already exists. The existing reference scene was preserved.");
            return;
        }

        if (Application.isBatchMode)
        {
            Scene batchScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            BuildActiveReferenceScene(batchScene);
            EditorSceneManager.SaveScene(batchScene, ScenePath);
        }
        else
        {
            Scene previousActiveScene = SceneManager.GetActiveScene();
            Scene referenceScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(referenceScene);
            BuildActiveReferenceScene(referenceScene);
            EditorSceneManager.SaveScene(referenceScene, ScenePath);
            EditorSceneManager.CloseScene(referenceScene, true);
            SceneManager.SetActiveScene(previousActiveScene);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[CanvasBoardReferenceSceneBuilder] Canvas board reference scene created without changing the active runtime scene.");
    }

    public static void RebuildCanvasBoardReferenceScene()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
        {
            AssetDatabase.DeleteAsset(ScenePath);
        }

        CreateCanvasBoardReferenceScene();
    }

    [MenuItem("Tools/DeadLock/Visuals/Build Canvas Board Reference")]
    public static void BuildCanvasBoardReference()
    {
        Scene activeScene = SceneManager.GetActiveScene();

        if (activeScene.path != ScenePath)
        {
            Debug.LogError("[CanvasBoardReferenceSceneBuilder] Open LevelPlayCanvasReference before building it.");
            return;
        }

        BuildActiveReferenceScene(activeScene);
        EditorSceneManager.SaveScene(activeScene);
        AssetDatabase.SaveAssets();
        Debug.Log("[CanvasBoardReferenceSceneBuilder] Canvas board reference scene saved.");
    }

    private static void BuildActiveReferenceScene(Scene activeScene)
    {
        ClearActiveScene(activeScene);

        GameObject canvasObject = new GameObject("Canvas Board Reference", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.pixelPerfect = false;

        CanvasScaler canvasScaler = canvasObject.GetComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
        canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        canvasScaler.matchWidthOrHeight = 0.5f;

        CreateEventSystem();

        RectTransform backgroundRoot = CreateStretchRect("Background", canvasObject.transform);
        CreateRoundedRect(backgroundRoot, "Fill", Vector2.zero, new Vector2(ReferenceWidth, ReferenceHeight), BackgroundColor, 0f);

        RectTransform boardRoot = CreateStretchRect("Board", canvasObject.transform);
        RectTransform connectionRoot = CreateStretchRect("Connections", boardRoot);
        RectTransform relayRoot = CreateStretchRect("Relays", boardRoot);
        RectTransform nodeRoot = CreateStretchRect("Nodes", boardRoot);

        Vector2 processOnePosition = new Vector2(-680f, 220f);
        Vector2 processTwoPosition = new Vector2(-630f, -220f);
        Vector2 resourceOnePosition = new Vector2(-180f, 185f);
        Vector2 resourceTwoPosition = new Vector2(230f, -145f);
        Vector2 resourceThreePosition = new Vector2(610f, 160f);

        CreateConnection(connectionRoot, processOnePosition + new Vector2(75f, 0f), resourceOnePosition + new Vector2(-82f, 0f), LimeColor);
        CreateConnection(connectionRoot, processOnePosition + new Vector2(58f, -45f), resourceTwoPosition + new Vector2(-82f, 12f), BlueColor);
        CreateConnection(connectionRoot, processTwoPosition + new Vector2(76f, 0f), resourceTwoPosition + new Vector2(-82f, 0f), RedColor);
        CreateConnection(connectionRoot, resourceTwoPosition + new Vector2(82f, 18f), resourceThreePosition + new Vector2(-82f, -18f), OrangeColor);

        CreateRelay(relayRoot, resourceOnePosition + new Vector2(84f, 57f), resourceThreePosition + new Vector2(-84f, 57f), "LINK", InkColor);
        CreateRelay(relayRoot, resourceTwoPosition + new Vector2(84f, -56f), new Vector2(715f, -270f), "TRANSFER", BlueColor);

        CreateProcess(nodeRoot, "Process_Selected", processOnePosition, new[] { LimeColor, BlueColor, RedColor }, true);
        CreateProcess(nodeRoot, "Process_Waiting", processTwoPosition, new[] { RedColor, LimeColor }, false);
        CreateResource(nodeRoot, "Resource_Clock", resourceOnePosition, new[] { BlueColor, LimeColor, RedColor, OrangeColor }, "CLOCK", "3");
        CreateResource(nodeRoot, "Resource_ColorSwitch", resourceTwoPosition, new[] { RedColor, LimeColor, BlueColor }, "SWITCH", string.Empty);
        CreateResource(nodeRoot, "Resource_Simultaneous", resourceThreePosition, new[] { TealColor, RedColor }, "SYNC", string.Empty);

        CreateHud(canvasObject.transform);

        EditorSceneManager.MarkSceneDirty(activeScene);
    }

    private static void ClearActiveScene(Scene scene)
    {
        foreach (GameObject rootObject in scene.GetRootGameObjects())
        {
            Object.DestroyImmediate(rootObject);
        }
    }

    private static void CreateEventSystem()
    {
        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    private static void CreateConnection(RectTransform parent, Vector2 start, Vector2 end, Color color)
    {
        Vector2 delta = end - start;
        float length = delta.magnitude;
        float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
        Vector2 center = (start + end) * 0.5f;

        CanvasRoundedRectGraphic line = CreateRoundedRect(parent, "Track", center, new Vector2(length, 18f), color, 9f);
        line.rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    private static void CreateRelay(RectTransform parent, Vector2 start, Vector2 end, string label, Color color)
    {
        CreateConnection(parent, start, end, color);
        CreateDisc(parent, "StartEndpoint", start, new Vector2(24f, 24f), InkColor, 0f);
        CreateDisc(parent, "EndEndpoint", end, new Vector2(24f, 24f), InkColor, 0f);
        CreateText(parent, label, (start + end) * 0.5f + new Vector2(0f, 20f), new Vector2(130f, 28f), 17f, InkColor, TextAlignmentOptions.Center);
    }

    private static void CreateProcess(RectTransform parent, string name, Vector2 position, Color[] requiredColorArray, bool isSelected)
    {
        RectTransform root = CreateRect(name, parent, position, new Vector2(176f, 176f));
        CreateDisc(root, "Shadow", new Vector2(6f, -8f), new Vector2(164f, 164f), ShadowColor, 0f);
        CreateDisc(root, "Rim", Vector2.zero, new Vector2(164f, 164f), isSelected ? TealColor : new Color(0.56f, 0.63f, 0.65f), 0f);
        CreateDisc(root, "Fill", Vector2.zero, new Vector2(140f, 140f), SurfaceColor, 0f);
        CreateDisc(root, "Port", Vector2.zero, new Vector2(32f, 32f), InkColor, 0f);

        float chipStartX = -(requiredColorArray.Length - 1) * 19f;

        for (int index = 0; index < requiredColorArray.Length; index++)
        {
            Vector2 chipPosition = new Vector2(chipStartX + index * 38f, 103f);
            CreateDisc(root, "RequiredChip_" + (index + 1), chipPosition, new Vector2(31f, 31f), InkColor, 0f);
            CreateDisc(root, "RequiredFill_" + (index + 1), chipPosition, new Vector2(21f, 21f), requiredColorArray[index], 0f);
        }

        if (isSelected)
        {
            CreateDisc(root, "SelectionRing", Vector2.zero, new Vector2(182f, 182f), new Color(TealColor.r, TealColor.g, TealColor.b, 0.28f), 0.86f);
        }
    }

    private static void CreateResource(RectTransform parent, string name, Vector2 position, Color[] occupancyColorArray, string ruleName, string ruleValue)
    {
        RectTransform root = CreateRect(name, parent, position, new Vector2(198f, 198f));
        CreateRoundedRect(root, "Shadow", new Vector2(7f, -9f), new Vector2(174f, 174f), ShadowColor, 35f);
        CreateRoundedRect(root, "Rim", Vector2.zero, new Vector2(174f, 174f), ruleName == "SWITCH" ? BlueColor : TealColor, 34f);
        CreateRoundedRect(root, "Fill", Vector2.zero, new Vector2(152f, 152f), SurfaceColor, 27f);

        Vector2[] slotPositionArray =
        {
            new Vector2(-39f, 30f),
            new Vector2(39f, 30f),
            new Vector2(-39f, -38f),
            new Vector2(39f, -38f)
        };

        for (int index = 0; index < slotPositionArray.Length; index++)
        {
            CreateDisc(root, "Slot_" + (index + 1), slotPositionArray[index], new Vector2(38f, 38f), new Color(InkColor.r, InkColor.g, InkColor.b, 0.10f), 0f);

            if (index < occupancyColorArray.Length)
            {
                CreateDisc(root, "Occupancy_" + (index + 1), slotPositionArray[index], new Vector2(25f, 25f), occupancyColorArray[index], 0f);
            }
        }

        CreateRoundedRect(root, "RuleBadge", new Vector2(0f, -103f), new Vector2(104f, 32f), InkColor, 16f);
        CreateText(root, ruleName, new Vector2(0f, -103f), new Vector2(104f, 32f), 16f, Color.white, TextAlignmentOptions.Center);

        if (!string.IsNullOrEmpty(ruleValue))
        {
            CreateDisc(root, "ClockBadge", new Vector2(77f, 77f), new Vector2(58f, 58f), OrangeColor, 0f);
            CreateDisc(root, "ClockFill", new Vector2(77f, 77f), new Vector2(46f, 46f), SurfaceColor, 0f);
            CreateText(root, ruleValue, new Vector2(77f, 77f), new Vector2(46f, 46f), 31f, InkColor, TextAlignmentOptions.Center);
        }

        if (ruleName == "SWITCH")
        {
            CreateDisc(root, "SwitchChip_1", new Vector2(-34f, 78f), new Vector2(18f, 18f), RedColor, 0f);
            CreateDisc(root, "SwitchChip_2", new Vector2(0f, 78f), new Vector2(18f, 18f), LimeColor, 0f);
            CreateDisc(root, "SwitchChip_3", new Vector2(34f, 78f), new Vector2(18f, 18f), BlueColor, 0f);
        }
    }

    private static void CreateHud(Transform parent)
    {
        RectTransform hudRoot = CreateStretchRect("HUD", parent);
        CreateText(hudRoot, "LEVEL 08", new Vector2(-820f, 476f), new Vector2(230f, 52f), 34f, InkColor, TextAlignmentOptions.Left);
        CreateText(hudRoot, "RESERVED 4 / 8", new Vector2(0f, 476f), new Vector2(260f, 52f), 29f, InkColor, TextAlignmentOptions.Center);
        CreateButton(hudRoot, "RestartButton", new Vector2(834f, 476f), new Vector2(96f, 52f), "R", 28f, SurfaceColor, TealColor);

        RectTransform rail = CreateRect("RequiredColorRail", hudRoot, new Vector2(-848f, -10f), new Vector2(58f, 270f));
        CreateRoundedRect(rail, "Rail", Vector2.zero, new Vector2(58f, 270f), new Color(InkColor.r, InkColor.g, InkColor.b, 0.72f), 29f);
        Color[] railColorArray = { RedColor, LimeColor, BlueColor, OrangeColor };

        for (int index = 0; index < railColorArray.Length; index++)
        {
            Vector2 chipPosition = new Vector2(0f, 88f - index * 58f);
            CreateDisc(rail, "RailChip_" + (index + 1), chipPosition, new Vector2(38f, 38f), Color.white, 0f);
            CreateDisc(rail, "RailFill_" + (index + 1), chipPosition, new Vector2(25f, 25f), railColorArray[index], 0f);
        }

        CreateButton(hudRoot, "RunButton", new Vector2(0f, -470f), new Vector2(336f, 82f), "RUN", 37f, SurfaceColor, TealColor);
        CreateButton(hudRoot, "PauseButton", new Vector2(492f, -468f), new Vector2(152f, 60f), "PAUSE", 23f, SurfaceColor, TealColor);
        CreateButton(hudRoot, "SpeedHalfButton", new Vector2(650f, -468f), new Vector2(74f, 60f), "0.5x", 18f, SurfaceColor, BlueColor);
        CreateButton(hudRoot, "SpeedNormalButton", new Vector2(730f, -468f), new Vector2(60f, 60f), "1x", 18f, SurfaceColor, BlueColor);
        CreateButton(hudRoot, "SpeedDoubleButton", new Vector2(800f, -468f), new Vector2(60f, 60f), "2x", 18f, SurfaceColor, BlueColor);
    }

    private static Button CreateButton(RectTransform parent, string name, Vector2 position, Vector2 size, string label, float fontSize, Color fillColor, Color rimColor)
    {
        RectTransform root = CreateRect(name, parent, position, size);
        CreateRoundedRect(root, "Shadow", new Vector2(4f, -6f), size, ShadowColor, size.y * 0.5f);
        CanvasRoundedRectGraphic background = CreateRoundedRect(root, "Background", Vector2.zero, size, rimColor, size.y * 0.5f);
        CreateRoundedRect(root, "Fill", Vector2.zero, size - new Vector2(12f, 12f), fillColor, (size.y - 12f) * 0.5f);
        CreateText(root, "Label", Vector2.zero, size, fontSize, InkColor, TextAlignmentOptions.Center);
        root.GetComponentInChildren<TextMeshProUGUI>().text = label;

        Button button = root.gameObject.AddComponent<Button>();
        button.targetGraphic = background;
        return button;
    }

    private static CanvasRoundedRectGraphic CreateRoundedRect(RectTransform parent, string name, Vector2 position, Vector2 size, Color color, float cornerRadius)
    {
        RectTransform root = CreateRect(name, parent, position, size);
        root.gameObject.AddComponent<CanvasRenderer>();
        CanvasRoundedRectGraphic graphic = root.gameObject.AddComponent<CanvasRoundedRectGraphic>();
        graphic.color = color;
        graphic.CornerRadius = cornerRadius;
        graphic.raycastTarget = false;
        return graphic;
    }

    private static CanvasDiscGraphic CreateDisc(RectTransform parent, string name, Vector2 position, Vector2 size, Color color, float innerRadiusRatio)
    {
        RectTransform root = CreateRect(name, parent, position, size);
        root.gameObject.AddComponent<CanvasRenderer>();
        CanvasDiscGraphic graphic = root.gameObject.AddComponent<CanvasDiscGraphic>();
        graphic.color = color;
        graphic.InnerRadiusRatio = innerRadiusRatio;
        graphic.raycastTarget = false;
        return graphic;
    }

    private static TextMeshProUGUI CreateText(RectTransform parent, string name, Vector2 position, Vector2 size, float fontSize, Color color, TextAlignmentOptions alignment)
    {
        RectTransform root = CreateRect(name, parent, position, size);
        TextMeshProUGUI label = root.gameObject.AddComponent<TextMeshProUGUI>();
        label.font = TMP_Settings.defaultFontAsset;
        label.fontSize = fontSize;
        label.color = color;
        label.alignment = alignment;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.text = string.Empty;
        label.raycastTarget = false;
        return label;
    }

    private static RectTransform CreateStretchRect(string name, Transform parent)
    {
        RectTransform root = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        root.SetParent(parent, false);
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;
        return root;
    }

    private static RectTransform CreateRect(string name, Transform parent, Vector2 position, Vector2 size)
    {
        RectTransform root = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        root.SetParent(parent, false);
        root.anchorMin = new Vector2(0.5f, 0.5f);
        root.anchorMax = new Vector2(0.5f, 0.5f);
        root.pivot = new Vector2(0.5f, 0.5f);
        root.anchoredPosition = position;
        root.sizeDelta = size;
        return root;
    }
}

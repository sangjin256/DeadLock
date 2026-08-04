using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class LevelPlayCanvasHudBuilder
{
    private const string HudPrefabPath = "Assets/03.Prefabs/LevelPlay/Hud/Prefab_UI_LevelPlayHud.prefab";
    private const string RuntimeScenePath = "Assets/01.Scenes/LevelPlayRuntime.unity";
    private const string VisualSettingsPath = "Assets/05.Visual Resources/Settings/VisualSettings_Default.asset";
    private const float ReferenceWidth = 1920f;
    private const float ReferenceHeight = 1080f;

    private static readonly Color SurfaceColor = new(0.93f, 0.97f, 0.97f);
    private static readonly Color InkColor = new(0.10f, 0.15f, 0.18f);
    private static readonly Color ShadowColor = new(0.10f, 0.15f, 0.18f, 0.26f);
    private static readonly Color TealColor = new(0.12f, 0.77f, 0.68f);
    private static readonly Color BlueColor = new(0.39f, 0.60f, 0.94f);
    private static readonly Color OrangeColor = new(1f, 0.66f, 0.20f);

    [MenuItem("Tools/DeadLock/UI/Rebuild LevelPlay Canvas HUD")]
    public static void RebuildCanvasHud()
    {
        VisualSettingsSO visualSettings = AssetDatabase.LoadAssetAtPath<VisualSettingsSO>(VisualSettingsPath);
        GameObject hudPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HudPrefabPath);
        SceneAsset runtimeScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(RuntimeScenePath);

        if (visualSettings == null || hudPrefab == null || runtimeScene == null)
        {
            Debug.LogError("[LevelPlayCanvasHudBuilder] Required LevelPlay HUD asset, runtime scene, or VisualSettings asset is missing.");
            return;
        }

        BuildHudPrefab(hudPrefab, visualSettings);
        PlaceHudInRuntimeScene(hudPrefab);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[LevelPlayCanvasHudBuilder] Rebuilt Canvas HUD prefab and updated LevelPlayRuntime.");
    }

    private static void BuildHudPrefab(GameObject hudPrefab, VisualSettingsSO visualSettings)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(HudPrefabPath);

        try
        {
            ClearChildren(root.transform);
            RemoveHudViews(root);

            RectTransform canvasRoot = CreateStretchRect("Canvas", root.transform);
            Canvas canvas = canvasRoot.gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = false;

            CanvasScaler scaler = canvasRoot.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasRoot.gameObject.AddComponent<GraphicRaycaster>();

            LevelPlayHudView hudView = canvasRoot.gameObject.AddComponent<LevelPlayHudView>();
            HudReferences references = CreateHudHierarchy(canvasRoot, visualSettings);
            hudView.ConfigurePrefabReferences(visualSettings,
                                              references.PlanningRoot,
                                              references.PlaybackRoot,
                                              references.ColorRailRoot,
                                              references.ResultRoot,
                                              references.StageLabel,
                                              references.PlanningProgressLabel,
                                              references.PlaybackRoundLabel,
                                              references.PauseLabel,
                                              references.ResultTitleLabel,
                                              references.ResultRoundLabel,
                                              references.RunButton,
                                              references.RestartButton,
                                              references.PauseButton,
                                              references.ResultRestartButton,
                                              references.SpeedButtonArray,
                                              references.SpeedSelectionRingArray,
                                              references.ColorChipReferenceArray,
                                              references.ResultStarRootArray,
                                              references.DragPreviewRoot,
                                              references.DragPreviewFill);
            PrefabUtility.SaveAsPrefabAsset(root, HudPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static HudReferences CreateHudHierarchy(RectTransform canvasRoot, VisualSettingsSO visualSettings)
    {
        RectTransform headerRoot = CreateStretchRect("Header", canvasRoot);
        TextMeshProUGUI stageLabel = CreateText(headerRoot, "LevelLabel", new Vector2(-830f, 484f), new Vector2(280f, 56f), 34f, InkColor, TextAlignmentOptions.Left);
        TextMeshProUGUI planningProgressLabel = CreateText(headerRoot, "ReservedLabel", new Vector2(0f, 484f), new Vector2(340f, 56f), 30f, InkColor, TextAlignmentOptions.Center);
        TextMeshProUGUI playbackRoundLabel = CreateText(headerRoot, "RoundLabel", new Vector2(0f, 484f), new Vector2(260f, 56f), 32f, InkColor, TextAlignmentOptions.Center);
        HudButton restart = CreateButton(headerRoot, "Restart", new Vector2(840f, 482f), new Vector2(102f, 58f), "↻", 35f, SurfaceColor, TealColor);

        RectTransform planningRoot = CreateStretchRect("Planning", canvasRoot);
        RectTransform railRoot = CreateRect("RequiredColorRail", planningRoot, new Vector2(-852f, -2f), new Vector2(70f, 394f));
        CreateRoundedRect(railRoot, "Shadow", new Vector2(4f, -6f), railRoot.sizeDelta, ShadowColor, 35f);
        CreateRoundedRect(railRoot, "Background", Vector2.zero, railRoot.sizeDelta, new Color(InkColor.r, InkColor.g, InkColor.b, 0.84f), 35f);
        HudColorChipReference[] colorChipArray = new HudColorChipReference[6];

        for (int index = 0; index < colorChipArray.Length; index++)
        {
            RectTransform chipRoot = CreateRect("Chip_" + (index + 1), railRoot, new Vector2(0f, 145f - index * 58f), new Vector2(50f, 50f));
            CanvasDiscGraphic rim = CreateDisc(chipRoot, "Rim", Vector2.zero, new Vector2(46f, 46f), Color.white, 0f);
            CanvasDiscGraphic fill = CreateDisc(chipRoot, "Fill", Vector2.zero, new Vector2(31f, 31f), visualSettings.MutedColor, 0f);
            CanvasDiscGraphic selection = CreateDisc(chipRoot, "Selected", Vector2.zero, new Vector2(52f, 52f), TealColor, 0.82f);
            selection.gameObject.SetActive(false);
            CanvasRoundedRectGraphic inputSurface = CreateRoundedRect(chipRoot, "InputSurface", Vector2.zero, chipRoot.sizeDelta, Color.clear, chipRoot.sizeDelta.x * 0.5f);
            inputSurface.raycastTarget = true;
            HudColorChipInput input = chipRoot.gameObject.AddComponent<HudColorChipInput>();
            input.Configure(index);
            colorChipArray[index] = new HudColorChipReference(chipRoot.gameObject, fill, selection);
        }

        HudButton run = CreateButton(planningRoot, "Run", new Vector2(0f, -474f), new Vector2(350f, 84f), "RUN", 38f, SurfaceColor, TealColor);

        RectTransform playbackRoot = CreateStretchRect("Playback", canvasRoot);
        HudButton pause = CreateButton(playbackRoot, "Pause", new Vector2(-216f, -474f), new Vector2(184f, 68f), "PAUSE", 27f, SurfaceColor, TealColor);
        HudButton halfSpeed = CreateButton(playbackRoot, "HalfSpeed", new Vector2(-40f, -474f), new Vector2(112f, 68f), "0.5x", 24f, SurfaceColor, BlueColor);
        HudButton normalSpeed = CreateButton(playbackRoot, "NormalSpeed", new Vector2(78f, -474f), new Vector2(94f, 68f), "1x", 24f, SurfaceColor, BlueColor);
        HudButton doubleSpeed = CreateButton(playbackRoot, "DoubleSpeed", new Vector2(178f, -474f), new Vector2(94f, 68f), "2x", 24f, SurfaceColor, BlueColor);
        CanvasDiscGraphic[] speedSelectionRingArray =
        {
            CreateSpeedSelection(halfSpeed.Root),
            CreateSpeedSelection(normalSpeed.Root),
            CreateSpeedSelection(doubleSpeed.Root)
        };

        RectTransform resultRoot = CreateStretchRect("ResultModal", canvasRoot);
        CanvasRoundedRectGraphic blocker = CreateRoundedRect(resultRoot, "InputBlocker", Vector2.zero, new Vector2(ReferenceWidth, ReferenceHeight), new Color(0.03f, 0.06f, 0.08f, 0.58f), 0f);
        blocker.rectTransform.anchorMin = Vector2.zero;
        blocker.rectTransform.anchorMax = Vector2.one;
        blocker.rectTransform.offsetMin = Vector2.zero;
        blocker.rectTransform.offsetMax = Vector2.zero;
        blocker.raycastTarget = true;
        RectTransform panel = CreateRect("Panel", resultRoot, new Vector2(0f, 0f), new Vector2(510f, 316f));
        CreateRoundedRect(panel, "Shadow", new Vector2(9f, -12f), panel.sizeDelta, ShadowColor, 42f);
        CreateRoundedRect(panel, "Rim", Vector2.zero, panel.sizeDelta, TealColor, 42f);
        CreateRoundedRect(panel, "Fill", Vector2.zero, panel.sizeDelta - new Vector2(16f, 16f), SurfaceColor, 35f);
        TextMeshProUGUI resultTitleLabel = CreateText(panel, "Title", new Vector2(0f, 88f), new Vector2(430f, 62f), 46f, visualSettings.CompletedColor, TextAlignmentOptions.Center);
        TextMeshProUGUI resultRoundLabel = CreateText(panel, "Round", new Vector2(0f, 27f), new Vector2(320f, 38f), 25f, InkColor, TextAlignmentOptions.Center);
        GameObject[] starRootArray = new GameObject[3];

        for (int index = 0; index < starRootArray.Length; index++)
        {
            TextMeshProUGUI star = CreateText(panel, "Star_" + (index + 1), new Vector2(-52f + index * 52f, -25f), new Vector2(50f, 50f), 45f, OrangeColor, TextAlignmentOptions.Center);
            star.text = "★";
            starRootArray[index] = star.gameObject;
        }

        HudButton resultRestart = CreateButton(panel, "Restart", new Vector2(0f, -101f), new Vector2(244f, 62f), "RESTART", 27f, SurfaceColor, TealColor);
        resultRoot.gameObject.SetActive(false);

        RectTransform dragPreviewRoot = CreateRect("DragPreview", canvasRoot, Vector2.zero, new Vector2(62f, 62f));
        CreateDisc(dragPreviewRoot, "Shadow", new Vector2(4f, -6f), new Vector2(62f, 62f), ShadowColor, 0f);
        CreateDisc(dragPreviewRoot, "Rim", Vector2.zero, new Vector2(58f, 58f), Color.white, 0f);
        CanvasDiscGraphic dragPreviewFill = CreateDisc(dragPreviewRoot, "Fill", Vector2.zero, new Vector2(42f, 42f), visualSettings.MutedColor, 0f);
        dragPreviewRoot.gameObject.SetActive(false);

        return new HudReferences(planningRoot.gameObject,
                                 playbackRoot.gameObject,
                                 railRoot.gameObject,
                                 resultRoot.gameObject,
                                 stageLabel,
                                 planningProgressLabel,
                                 playbackRoundLabel,
                                 pause.Label,
                                 resultTitleLabel,
                                 resultRoundLabel,
                                 run.Button,
                                 restart.Button,
                                 pause.Button,
                                 resultRestart.Button,
                                 new[] { halfSpeed.Button, normalSpeed.Button, doubleSpeed.Button },
                                 speedSelectionRingArray,
                                 colorChipArray,
                                 starRootArray,
                                 dragPreviewRoot,
                                 dragPreviewFill);
    }

    private static CanvasDiscGraphic CreateSpeedSelection(RectTransform root)
    {
        CanvasDiscGraphic selection = CreateDisc(root, "Selected", Vector2.zero, root.sizeDelta + new Vector2(10f, 10f), TealColor, 0.88f);
        selection.transform.SetAsFirstSibling();
        selection.gameObject.SetActive(false);
        return selection;
    }

    private static void PlaceHudInRuntimeScene(GameObject hudPrefab)
    {
        Scene scene = EditorSceneManager.OpenScene(RuntimeScenePath, OpenSceneMode.Single);
        LevelPlayHudView[] existingHudViewArray = Object.FindObjectsByType<LevelPlayHudView>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        for (int index = 0; index < existingHudViewArray.Length; index++)
        {
            Object.DestroyImmediate(existingHudViewArray[index].transform.root.gameObject);
        }

        GameObject hudInstance = (GameObject)PrefabUtility.InstantiatePrefab(hudPrefab, scene);
        BoardPresenter presenter = Object.FindFirstObjectByType<BoardPresenter>();

        if (presenter == null)
        {
            Debug.LogError("[LevelPlayCanvasHudBuilder] BoardPresenter is missing from LevelPlayRuntime.");
            return;
        }

        SerializedObject presenterSerializedObject = new(presenter);
        presenterSerializedObject.FindProperty("_hudView").objectReferenceValue = hudInstance.GetComponentInChildren<LevelPlayHudView>(true);
        presenterSerializedObject.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static HudButton CreateButton(RectTransform parent, string name, Vector2 position, Vector2 size, string text, float fontSize, Color fillColor, Color rimColor)
    {
        RectTransform root = CreateRect(name, parent, position, size);
        CreateRoundedRect(root, "Shadow", new Vector2(4f, -6f), size, ShadowColor, size.y * 0.5f);
        CanvasRoundedRectGraphic background = CreateRoundedRect(root, "Background", Vector2.zero, size, rimColor, size.y * 0.5f);
        background.raycastTarget = true;
        CreateRoundedRect(root, "Fill", Vector2.zero, size - new Vector2(12f, 12f), fillColor, (size.y - 12f) * 0.5f);
        TextMeshProUGUI label = CreateText(root, "Label", Vector2.zero, size, fontSize, InkColor, TextAlignmentOptions.Center);
        label.text = text;
        Button button = root.gameObject.AddComponent<Button>();
        button.targetGraphic = background;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 1f, 1f, 0.92f);
        colors.pressedColor = new Color(1f, 1f, 1f, 0.78f);
        colors.disabledColor = new Color(1f, 1f, 1f, 0.38f);
        button.colors = colors;
        return new HudButton(root, button, label);
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

    private static void ClearChildren(Transform root)
    {
        for (int index = root.childCount - 1; index >= 0; index--)
        {
            Object.DestroyImmediate(root.GetChild(index).gameObject);
        }
    }

    private static void RemoveHudViews(GameObject root)
    {
        LevelPlayHudView[] hudViewArray = root.GetComponentsInChildren<LevelPlayHudView>(true);

        for (int index = 0; index < hudViewArray.Length; index++)
        {
            Object.DestroyImmediate(hudViewArray[index]);
        }
    }

    private readonly struct HudButton
    {
        public readonly RectTransform Root;
        public readonly Button Button;
        public readonly TextMeshProUGUI Label;

        public HudButton(RectTransform root, Button button, TextMeshProUGUI label)
        {
            Root = root;
            Button = button;
            Label = label;
        }
    }

    private readonly struct HudReferences
    {
        public readonly GameObject PlanningRoot;
        public readonly GameObject PlaybackRoot;
        public readonly GameObject ColorRailRoot;
        public readonly GameObject ResultRoot;
        public readonly TMP_Text StageLabel;
        public readonly TMP_Text PlanningProgressLabel;
        public readonly TMP_Text PlaybackRoundLabel;
        public readonly TMP_Text PauseLabel;
        public readonly TMP_Text ResultTitleLabel;
        public readonly TMP_Text ResultRoundLabel;
        public readonly Button RunButton;
        public readonly Button RestartButton;
        public readonly Button PauseButton;
        public readonly Button ResultRestartButton;
        public readonly Button[] SpeedButtonArray;
        public readonly CanvasDiscGraphic[] SpeedSelectionRingArray;
        public readonly HudColorChipReference[] ColorChipReferenceArray;
        public readonly GameObject[] ResultStarRootArray;
        public readonly RectTransform DragPreviewRoot;
        public readonly CanvasDiscGraphic DragPreviewFill;

        public HudReferences(GameObject planningRoot,
                             GameObject playbackRoot,
                             GameObject colorRailRoot,
                             GameObject resultRoot,
                             TMP_Text stageLabel,
                             TMP_Text planningProgressLabel,
                             TMP_Text playbackRoundLabel,
                             TMP_Text pauseLabel,
                             TMP_Text resultTitleLabel,
                             TMP_Text resultRoundLabel,
                             Button runButton,
                             Button restartButton,
                             Button pauseButton,
                             Button resultRestartButton,
                             Button[] speedButtonArray,
                             CanvasDiscGraphic[] speedSelectionRingArray,
                             HudColorChipReference[] colorChipReferenceArray,
                             GameObject[] resultStarRootArray,
                             RectTransform dragPreviewRoot,
                             CanvasDiscGraphic dragPreviewFill)
        {
            PlanningRoot = planningRoot;
            PlaybackRoot = playbackRoot;
            ColorRailRoot = colorRailRoot;
            ResultRoot = resultRoot;
            StageLabel = stageLabel;
            PlanningProgressLabel = planningProgressLabel;
            PlaybackRoundLabel = playbackRoundLabel;
            PauseLabel = pauseLabel;
            ResultTitleLabel = resultTitleLabel;
            ResultRoundLabel = resultRoundLabel;
            RunButton = runButton;
            RestartButton = restartButton;
            PauseButton = pauseButton;
            ResultRestartButton = resultRestartButton;
            SpeedButtonArray = speedButtonArray;
            SpeedSelectionRingArray = speedSelectionRingArray;
            ColorChipReferenceArray = colorChipReferenceArray;
            ResultStarRootArray = resultStarRootArray;
            DragPreviewRoot = dragPreviewRoot;
            DragPreviewFill = dragPreviewFill;
        }
    }
}

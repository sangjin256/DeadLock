using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class LevelEditorWindow : EditorWindow
{
    private const string NewLevelAssetPath = "Assets/02.Scripts/02.Repository/Levels/Level_New.asset";
    private const float CellSize = 92f;
    private const float NodeSize = 64f;
    private const float PaletteWidth = 260f;
    private const float InspectorWidth = 370f;
    private const float ValidationPanelHeight = 240f;
    private const string AutoOptimalTestCaseName = "Auto Optimal";

    private static readonly Color BackgroundColor = new Color(0.12f, 0.13f, 0.15f);
    private static readonly Color PanelColor = new Color(0.17f, 0.18f, 0.20f);
    private static readonly Color CanvasColor = new Color(0.10f, 0.11f, 0.13f);
    private static readonly Color GridColor = new Color(0.27f, 0.29f, 0.32f);
    private static readonly Color NodeBaseColor = new Color(0.96f, 0.96f, 0.94f);
    private static readonly Color SelectedColor = new Color(1.0f, 0.78f, 0.22f);
    private static readonly Color WarningColor = new Color(0.90f, 0.24f, 0.20f);

    private readonly LevelEditorColorMap _colorMap = new LevelEditorColorMap();
    private readonly LevelEditorValidationUtility _validationUtility = new LevelEditorValidationUtility();
    private readonly LevelDifficultyAnalyzer _difficultyAnalyzer = new LevelDifficultyAnalyzer();

    private LevelSO _levelSO;
    private SerializedObject _serializedObject;
    private ObjectField _levelObjectField;
    private VisualElement _toolContainer;
    private VisualElement _paletteContainer;
    private VisualElement _selectedColorPreview;
    private Label _selectedColorLabel;
    private VisualElement _boardContainer;
    private VisualElement _inspectorContainer;
    private VisualElement _validationContainer;
    private ELevelEditorTool _currentTool = ELevelEditorTool.Select;
    private int _selectedRow = -1;
    private int _selectedColumn = -1;
    private int _selectedColorId = 1;
    private int _relayDraftFirstResourceId = -1;
    private int _relayDraftSecondResourceId = -1;
    private int _relayDraftSenderResourceId = -1;
    private int _selectedRelayIndex = -1;
    private ERelayType _relayDraftType = ERelayType.Link;
    private string _relayDraftMessage = string.Empty;
    private string _finalValidationText = string.Empty;
    private string _testRunText = string.Empty;
    private string _solutionFinderText = string.Empty;
    private string _generationImportText = string.Empty;
    private string _testCaseEditMessage = string.Empty;
    private readonly List<LevelTestConnectionRunData> _lastTestConnectionRunDataList = new List<LevelTestConnectionRunData>();
    private LevelSolveReport _lastSolveReport;
    private LevelDifficultyReport _lastDifficultyReport;
    private SimulationReport _lastTestSimulationReport;
    private int _activeTestCaseIndex = -1;
    private int _lastRunTestCaseIndex = -1;
    private int _selectedTestRoundIndex = -1;
    private int _selectedTestProcessId = -1;
    private int _selectedTestSlotId = -1;

    internal ELevelEditorTool CurrentTool => _currentTool;
    internal int SelectedRelayIndex => _selectedRelayIndex;
    internal int RelayDraftFirstResourceId => _relayDraftFirstResourceId;
    internal int RelayDraftSecondResourceId => _relayDraftSecondResourceId;
    internal int RelayDraftSenderResourceId => _relayDraftSenderResourceId;
    internal ERelayType RelayDraftType => _relayDraftType;
    internal bool HasRelayDraftPair => _relayDraftFirstResourceId >= 0 && _relayDraftSecondResourceId >= 0;
    internal int SelectedResourceId => GetSelectedResourceId();
    internal bool IsTestCaseEditActive => _activeTestCaseIndex >= 0;

    [MenuItem("Tools/DeadLock/Levels/Level Editor")]
    public static void Open()
    {
        LevelEditorWindow window = GetWindow<LevelEditorWindow>("레벨 에디터");
        window.minSize = new Vector2(1120f, 720f);
    }

    public void CreateGUI()
    {
        _colorMap.Reload();

        rootVisualElement.Clear();
        rootVisualElement.style.flexDirection = FlexDirection.Column;
        rootVisualElement.style.backgroundColor = new StyleColor(BackgroundColor);
        rootVisualElement.style.paddingLeft = 12f;
        rootVisualElement.style.paddingRight = 12f;
        rootVisualElement.style.paddingTop = 12f;
        rootVisualElement.style.paddingBottom = 12f;

        BuildHeader();
        BuildEditorBody();
        RefreshAll();
    }

    private void BuildHeader()
    {
        VisualElement header = new VisualElement();
        header.style.flexDirection = FlexDirection.Row;
        header.style.alignItems = Align.Center;
        header.style.marginBottom = 10f;
        rootVisualElement.Add(header);

        Label titleLabel = new Label("DeadLock 레벨 에디터");
        titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        titleLabel.style.fontSize = 18f;
        titleLabel.style.marginRight = 16f;
        titleLabel.style.color = new StyleColor(Color.white);
        header.Add(titleLabel);

        _levelObjectField = new ObjectField("LevelSO");
        _levelObjectField.objectType = typeof(LevelSO);
        _levelObjectField.style.flexGrow = 1f;
        _levelObjectField.RegisterValueChangedCallback(OnLevelChanged);
        header.Add(_levelObjectField);

        header.Add(CreateHeaderButton("새 레벨", CreateNewLevel));
        header.Add(CreateHeaderButton("저장", SaveLevel));
        header.Add(CreateHeaderButton("검증", RunFinalValidation));
        header.Add(CreateHeaderButton("AI 후보 가져오기", ImportGeneratedCandidate));
        header.Add(CreateHeaderButton("색상 새로고침", ReloadColors));
    }

    private void BuildEditorBody()
    {
        TwoPaneSplitView body = new TwoPaneSplitView(1,
                                                     ValidationPanelHeight,
                                                     TwoPaneSplitViewOrientation.Vertical);
        body.style.flexGrow = 1f;
        rootVisualElement.Add(body);

        BuildMainContent(body);
        BuildValidationPanel(body);
    }

    private void BuildMainContent(VisualElement parent)
    {
        TwoPaneSplitView main = new TwoPaneSplitView(0, PaletteWidth, TwoPaneSplitViewOrientation.Horizontal);
        main.style.flexGrow = 1f;
        parent.Add(main);

        _paletteContainer = CreatePanel(PaletteWidth);
        main.Add(_paletteContainer);

        TwoPaneSplitView content = new TwoPaneSplitView(1, InspectorWidth, TwoPaneSplitViewOrientation.Horizontal);
        content.style.flexGrow = 1f;
        main.Add(content);

        _boardContainer = CreatePanel(0f);
        _boardContainer.style.flexGrow = 1f;
        content.Add(_boardContainer);

        _inspectorContainer = new ScrollView();
        _inspectorContainer.style.width = InspectorWidth;
        _inspectorContainer.style.flexShrink = 0f;
        ApplyPanelStyle(_inspectorContainer);
        content.Add(_inspectorContainer);
    }

    private void BuildValidationPanel(VisualElement parent)
    {
        _validationContainer = new ScrollView();
        _validationContainer.style.minHeight = 96f;
        ApplyPanelStyle(_validationContainer);
        parent.Add(_validationContainer);
    }

    private VisualElement CreatePanel(float width)
    {
        VisualElement panel = new VisualElement();

        if (width > 0f)
        {
            panel.style.width = width;
            panel.style.flexShrink = 0f;
        }

        ApplyPanelStyle(panel);
        return panel;
    }

    private void ApplyPanelStyle(VisualElement element)
    {
        element.style.backgroundColor = new StyleColor(PanelColor);
        element.style.paddingLeft = 10f;
        element.style.paddingRight = 10f;
        element.style.paddingTop = 10f;
        element.style.paddingBottom = 10f;
        element.style.borderTopLeftRadius = 8f;
        element.style.borderTopRightRadius = 8f;
        element.style.borderBottomLeftRadius = 8f;
        element.style.borderBottomRightRadius = 8f;
        SetBorder(element, 1f, new Color(0.28f, 0.30f, 0.34f));
    }

    private Button CreateHeaderButton(string text, Action action)
    {
        Button button = new Button(action)
        {
            text = text,
        };

        button.style.marginLeft = 6f;
        return button;
    }

    private void OnLevelChanged(ChangeEvent<UnityEngine.Object> changeEvent)
    {
        SetLevel(changeEvent.newValue as LevelSO);
    }

    private void SetLevel(LevelSO levelSO)
    {
        _levelSO = levelSO;
        _serializedObject = _levelSO == null ? null : new SerializedObject(_levelSO);
        _selectedRow = -1;
        _selectedColumn = -1;
        ClearRelayDraft();
        _selectedRelayIndex = -1;
        _finalValidationText = string.Empty;
        _generationImportText = string.Empty;
        ClearTestRunState();
        ClearSolutionFinderState();
        _testCaseEditMessage = string.Empty;
        _activeTestCaseIndex = -1;
        _selectedTestProcessId = -1;
        _selectedTestSlotId = -1;
        RefreshAll();
    }

    private void RefreshAll()
    {
        if (_levelObjectField != null)
        {
            _levelObjectField.SetValueWithoutNotify(_levelSO);
        }

        if (_serializedObject != null)
        {
            _serializedObject.Update();
        }

        RefreshPalette();
        RefreshBoard();
        RefreshInspector();
        RefreshValidationLog();
    }

    private void RefreshPalette()
    {
        if (_paletteContainer == null)
        {
            return;
        }

        _paletteContainer.Clear();
        _paletteContainer.Add(CreateSectionTitle("색상 팔레트"));
        BuildSelectedColorCard();
        BuildToolButtons();

        ScrollView paletteScroll = new ScrollView();
        paletteScroll.style.flexGrow = 1f;
        paletteScroll.style.marginTop = 10f;
        _paletteContainer.Add(paletteScroll);

        List<int> colorIdList = GatherColorIdList();

        for (int i = 0; i < colorIdList.Count; i++)
        {
            paletteScroll.Add(CreatePaletteRow(colorIdList[i]));
        }
    }

    private void BuildSelectedColorCard()
    {
        VisualElement card = CreateBox();
        card.style.marginBottom = 10f;
        card.style.minHeight = 132f;
        _paletteContainer.Add(card);

        Label label = new Label("선택 색상");
        label.style.unityFontStyleAndWeight = FontStyle.Bold;
        card.Add(label);

        VisualElement row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.alignItems = Align.Center;
        row.style.marginTop = 6f;
        card.Add(row);

        _selectedColorPreview = CreateColorSwatch(_selectedColorId, 32f);
        row.Add(_selectedColorPreview);

        _selectedColorLabel = new Label(GetColorLabel(_selectedColorId));
        _selectedColorLabel.style.color = new StyleColor(Color.white);
        _selectedColorLabel.style.flexGrow = 1f;
        _selectedColorLabel.style.whiteSpace = WhiteSpace.Normal;
        row.Add(_selectedColorLabel);

        VisualElement fieldRow = new VisualElement();
        fieldRow.style.flexDirection = FlexDirection.Row;
        fieldRow.style.alignItems = Align.Center;
        fieldRow.style.marginTop = 8f;
        card.Add(fieldRow);

        Label fieldLabel = new Label("색 ID");
        fieldLabel.style.color = new StyleColor(new Color(0.75f, 0.78f, 0.82f));
        fieldLabel.style.width = 48f;
        fieldRow.Add(fieldLabel);

        IntegerField selectedColorField = new IntegerField();
        selectedColorField.SetValueWithoutNotify(_selectedColorId);
        selectedColorField.RegisterValueChangedCallback(changeEvent =>
        {
            _selectedColorId = Mathf.Max(1, changeEvent.newValue);
            RefreshAll();
        });
        selectedColorField.style.width = 72f;
        selectedColorField.style.flexShrink = 0f;
        fieldRow.Add(selectedColorField);
    }

    private void BuildToolButtons()
    {
        _toolContainer = new VisualElement();
        _toolContainer.style.flexDirection = FlexDirection.Column;
        _toolContainer.style.marginTop = 8f;
        _paletteContainer.Add(_toolContainer);

        AddToolButton(ELevelEditorTool.Select, "선택", "보드 칸을 선택하고 세부 정보를 확인합니다.");
        AddToolButton(ELevelEditorTool.AddProcess, "프로세스 추가", "선택한 ColorId로 원형 프로세스 노드를 배치합니다.");
        AddToolButton(ELevelEditorTool.AddResource, "리소스 추가", "선택한 ColorId로 사각 리소스 노드를 배치합니다.");
        AddToolButton(ELevelEditorTool.AddRelay, "Relay 추가", "Resource 두 개를 순서대로 선택해 Relay를 생성합니다.");
        AddToolButton(ELevelEditorTool.Erase, "지우기", "클릭한 칸의 노드를 삭제합니다.");
    }

    private void AddToolButton(ELevelEditorTool tool, string label, string tooltip)
    {
        Button button = new Button(() =>
        {
            _currentTool = tool;
            _selectedRelayIndex = -1;

            if (_currentTool != ELevelEditorTool.AddRelay)
            {
                ClearRelayDraft();
            }
            else
            {
                _relayDraftMessage = "첫 번째 Resource를 선택해 주세요.";
            }

            RefreshPalette();
            RefreshBoard();
            RefreshInspector();
        })
        {
            text = label,
            tooltip = tooltip,
        };

        button.style.marginBottom = 4f;

        if (_currentTool == tool)
        {
            button.style.backgroundColor = new StyleColor(new Color(0.24f, 0.43f, 0.64f));
            button.style.color = new StyleColor(Color.white);
        }

        _toolContainer.Add(button);
    }

    private List<int> GatherColorIdList()
    {
        HashSet<int> colorIdSet = new HashSet<int>();
        colorIdSet.Add(_selectedColorId);

        foreach (KeyValuePair<int, Color> pair in _colorMap.ColorByIdDict)
        {
            colorIdSet.Add(pair.Key);
        }

        if (_levelSO != null)
        {
            AddLevelColorIds(colorIdSet);
        }

        List<int> colorIdList = new List<int>(colorIdSet);
        colorIdList.Sort();
        return colorIdList;
    }

    private void AddLevelColorIds(HashSet<int> colorIdSet)
    {
        for (int i = 0; i < _levelSO.ProcessDataList.Count; i++)
        {
            LevelProcessData processData = _levelSO.ProcessDataList[i];

            if (processData == null)
            {
                continue;
            }

            for (int j = 0; j < processData.SlotDataList.Count; j++)
            {
                LevelProcessSlotData slotData = processData.SlotDataList[j];

                if (slotData != null && slotData.RequiredColorId > 0)
                {
                    colorIdSet.Add(slotData.RequiredColorId);
                }
            }
        }

        for (int i = 0; i < _levelSO.ResourceDataList.Count; i++)
        {
            LevelResourceData resourceData = _levelSO.ResourceDataList[i];

            if (resourceData == null)
            {
                continue;
            }

            if (resourceData.InitialColorId > 0)
            {
                colorIdSet.Add(resourceData.InitialColorId);
            }

            AddRuleColorIds(resourceData, colorIdSet);
        }
    }

    private void AddRuleColorIds(LevelResourceData resourceData, HashSet<int> colorIdSet)
    {
        for (int i = 0; i < resourceData.RuleDataList.Count; i++)
        {
            LevelResourceRuleData ruleData = resourceData.RuleDataList[i];

            if (ruleData == null)
            {
                continue;
            }

            for (int j = 0; j < ruleData.ColorIdList.Count; j++)
            {
                int colorId = ruleData.ColorIdList[j];

                if (colorId > 0)
                {
                    colorIdSet.Add(colorId);
                }
            }
        }
    }

    private VisualElement CreatePaletteRow(int colorId)
    {
        VisualElement row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.alignItems = Align.Center;
        row.style.paddingLeft = 6f;
        row.style.paddingRight = 6f;
        row.style.paddingTop = 5f;
        row.style.paddingBottom = 5f;
        row.style.marginBottom = 3f;
        row.style.minHeight = 40f;
        row.style.flexShrink = 0f;
        row.style.borderTopLeftRadius = 6f;
        row.style.borderTopRightRadius = 6f;
        row.style.borderBottomLeftRadius = 6f;
        row.style.borderBottomRightRadius = 6f;

        if (colorId == _selectedColorId)
        {
            row.style.backgroundColor = new StyleColor(new Color(0.25f, 0.32f, 0.38f));
            SetBorder(row, 1f, SelectedColor);
        }

        row.Add(CreateColorSwatch(colorId, 22f));

        Label label = new Label(GetColorLabel(colorId));
        label.style.flexGrow = 1f;
        label.style.whiteSpace = WhiteSpace.Normal;
        label.style.color = new StyleColor(Color.white);
        row.Add(label);

        row.RegisterCallback<ClickEvent>(_ =>
        {
            _selectedColorId = colorId;
            RefreshAll();
        });

        return row;
    }

    private string GetColorLabel(int colorId)
    {
        return $"ID {colorId}  {_colorMap.GetLegacyHex(colorId)}";
    }

    private void RefreshBoard()
    {
        if (_boardContainer == null)
        {
            return;
        }

        _boardContainer.Clear();
        BuildBoardHeader();

        if (_levelSO == null)
        {
            _boardContainer.Add(CreateEmptyState("LevelSO를 선택하거나 새로 만들어 주세요."));
            return;
        }

        if (_levelSO.RowCount <= 0 || _levelSO.ColumnCount <= 0)
        {
            _boardContainer.Add(CreateEmptyState("보드 크기는 1 이상이어야 합니다."));
            return;
        }

        LevelBoardGraphView graphView = new LevelBoardGraphView(this, _colorMap);
        graphView.Populate(_levelSO);
        _boardContainer.Add(graphView);
    }

    private void BuildBoardHeader()
    {
        VisualElement header = new VisualElement();
        header.style.flexDirection = FlexDirection.Row;
        header.style.alignItems = Align.Center;
        header.style.marginBottom = 8f;
        _boardContainer.Add(header);

        header.Add(CreateSectionTitle("보드 캔버스"));

        Label stateLabel = new Label(GetBoardHeaderText());
        stateLabel.style.marginLeft = 10f;
        stateLabel.style.color = new StyleColor(new Color(0.75f, 0.78f, 0.82f));
        header.Add(stateLabel);
    }

    private string GetBoardHeaderText()
    {
        if (_levelSO == null)
        {
            return "선택된 레벨 없음";
        }

        return $"{_levelSO.RowCount} x {_levelSO.ColumnCount}  |  툴: {GetToolLabel(_currentTool)}  |  ColorId: {_selectedColorId}";
    }

    private string GetToolLabel(ELevelEditorTool tool)
    {
        switch (tool)
        {
            case ELevelEditorTool.Select:
                return "선택";

            case ELevelEditorTool.AddProcess:
                return "프로세스 추가";

            case ELevelEditorTool.AddResource:
                return "리소스 추가";

            case ELevelEditorTool.AddRelay:
                return "Relay 추가";

            case ELevelEditorTool.Erase:
                return "지우기";

            default:
                return tool.ToString();
        }
    }

    private VisualElement CreateCell(int row, int column)
    {
        LevelProcessData processData = FindProcessData(row, column);
        LevelResourceData resourceData = FindResourceData(row, column);
        bool isSelected = _selectedRow == row && _selectedColumn == column;
        bool hasOverlap = processData != null && resourceData != null;

        VisualElement cell = new VisualElement();
        cell.style.width = CellSize;
        cell.style.height = CellSize;
        cell.style.marginRight = 6f;
        cell.style.marginBottom = 6f;
        cell.style.alignItems = Align.Center;
        cell.style.justifyContent = Justify.Center;
        cell.style.backgroundColor = new StyleColor(new Color(0.14f, 0.15f, 0.17f));
        cell.style.borderTopLeftRadius = 8f;
        cell.style.borderTopRightRadius = 8f;
        cell.style.borderBottomLeftRadius = 8f;
        cell.style.borderBottomRightRadius = 8f;
        SetBorder(cell, isSelected ? 2f : 1f, isSelected ? SelectedColor : GridColor);
        cell.RegisterCallback<ClickEvent>(_ => HandleCellClick(row, column));

        Label coordinateLabel = new Label($"{row},{column}");
        coordinateLabel.style.position = Position.Absolute;
        coordinateLabel.style.left = 4f;
        coordinateLabel.style.top = 3f;
        coordinateLabel.style.fontSize = 10f;
        coordinateLabel.style.color = new StyleColor(new Color(0.48f, 0.50f, 0.54f));
        cell.Add(coordinateLabel);

        if (hasOverlap)
        {
            cell.style.backgroundColor = new StyleColor(new Color(0.28f, 0.12f, 0.12f));
            cell.Add(CreateWarningBadge("겹침"));
        }

        if (processData != null)
        {
            cell.Add(CreateNodeVisual(true,
                                      GetProcessColorIdList(processData),
                                      HasProcessWarning(processData)));
        }
        else if (resourceData != null)
        {
            cell.Add(CreateNodeVisual(false,
                                      GetResourceColorIdList(resourceData),
                                      HasResourceWarning(resourceData)));
        }

        return cell;
    }

    private VisualElement CreateNodeVisual(bool isProcess, IReadOnlyList<int> colorIdList, bool hasWarning)
    {
        VisualElement node = new VisualElement();
        node.style.width = NodeSize;
        node.style.height = NodeSize;
        node.style.alignItems = Align.Center;
        node.style.justifyContent = Justify.Center;
        node.style.backgroundColor = new StyleColor(NodeBaseColor);
        node.style.borderTopLeftRadius = isProcess ? NodeSize : 8f;
        node.style.borderTopRightRadius = isProcess ? NodeSize : 8f;
        node.style.borderBottomLeftRadius = isProcess ? NodeSize : 8f;
        node.style.borderBottomRightRadius = isProcess ? NodeSize : 8f;
        SetBorder(node, 2f, hasWarning ? WarningColor : new Color(0.82f, 0.82f, 0.78f));

        Label kindLabel = new Label(isProcess ? "P" : "R");
        kindLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        kindLabel.style.fontSize = 16f;
        kindLabel.style.color = new StyleColor(Color.black);
        node.Add(kindLabel);

        node.Add(CreateNodeColorStrip(colorIdList));

        if (hasWarning)
        {
            node.Add(CreateWarningDot());
        }

        return node;
    }

    private VisualElement CreateNodeColorStrip(IReadOnlyList<int> colorIdList)
    {
        VisualElement strip = new VisualElement();
        strip.style.position = Position.Absolute;
        strip.style.bottom = 5f;
        strip.style.flexDirection = FlexDirection.Row;
        strip.style.justifyContent = Justify.Center;
        strip.style.alignItems = Align.Center;

        int visibleCount = Mathf.Min(colorIdList.Count, 4);

        for (int i = 0; i < visibleCount; i++)
        {
            strip.Add(CreateMiniColorChip(colorIdList[i]));
        }

        if (colorIdList.Count > visibleCount)
        {
            Label moreLabel = new Label($"+{colorIdList.Count - visibleCount}");
            moreLabel.style.fontSize = 9f;
            moreLabel.style.color = new StyleColor(Color.black);
            moreLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            strip.Add(moreLabel);
        }

        return strip;
    }

    private VisualElement CreateMiniColorChip(int colorId)
    {
        VisualElement chip = new VisualElement();
        chip.style.width = 9f;
        chip.style.height = 9f;
        chip.style.marginLeft = 1f;
        chip.style.marginRight = 1f;
        chip.style.borderTopLeftRadius = 9f;
        chip.style.borderTopRightRadius = 9f;
        chip.style.borderBottomLeftRadius = 9f;
        chip.style.borderBottomRightRadius = 9f;
        chip.style.backgroundColor = new StyleColor(_colorMap.GetColor(colorId));
        SetBorder(chip, 1f, Color.black);
        return chip;
    }

    private VisualElement CreateWarningDot()
    {
        VisualElement dot = new VisualElement();
        dot.style.position = Position.Absolute;
        dot.style.right = -4f;
        dot.style.top = -4f;
        dot.style.width = 14f;
        dot.style.height = 14f;
        dot.style.backgroundColor = new StyleColor(WarningColor);
        dot.style.borderTopLeftRadius = 14f;
        dot.style.borderTopRightRadius = 14f;
        dot.style.borderBottomLeftRadius = 14f;
        dot.style.borderBottomRightRadius = 14f;
        SetBorder(dot, 1f, Color.white);
        return dot;
    }

    private Label CreateWarningBadge(string text)
    {
        Label label = new Label(text);
        label.style.position = Position.Absolute;
        label.style.right = 4f;
        label.style.bottom = 4f;
        label.style.fontSize = 10f;
        label.style.color = new StyleColor(Color.white);
        label.style.backgroundColor = new StyleColor(WarningColor);
        label.style.paddingLeft = 4f;
        label.style.paddingRight = 4f;
        label.style.paddingTop = 2f;
        label.style.paddingBottom = 2f;
        return label;
    }

    internal bool HasProcessIssue(LevelProcessData processData)
    {
        return HasProcessWarning(processData);
    }

    private bool HasProcessWarning(LevelProcessData processData)
    {
        if (processData.SlotDataList.Count == 0)
        {
            return true;
        }

        for (int i = 0; i < processData.SlotDataList.Count; i++)
        {
            LevelProcessSlotData slotData = processData.SlotDataList[i];

            if (slotData == null || slotData.RequiredColorId <= 0)
            {
                return true;
            }
        }

        return false;
    }

    internal List<LevelNodeColorChipData> GetProcessColorChips(LevelProcessData processData)
    {
        List<LevelNodeColorChipData> colorChipDataList = new List<LevelNodeColorChipData>();

        for (int i = 0; i < processData.SlotDataList.Count; i++)
        {
            LevelProcessSlotData slotData = processData.SlotDataList[i];

            if (slotData == null)
            {
                continue;
            }

            int processExecutionOrder = GetProcessExecutionOrder(processData.Id, slotData.Id);
            bool isAssigned = processExecutionOrder > 0;
            bool isSelected = IsSelectedTestSlot(processData.Id, slotData.Id);
            bool isFocused = !isAssigned ||
                             isSelected ||
                             IsAssignmentConnectedToSelectedResource(processData.Id, slotData.Id);

            colorChipDataList.Add(new LevelNodeColorChipData(slotData.RequiredColorId,
                                                             slotData.Id,
                                                             processExecutionOrder,
                                                             isAssigned,
                                                             isSelected,
                                                             isFocused));
        }

        return colorChipDataList;
    }

    private List<int> GetProcessColorIdList(LevelProcessData processData)
    {
        List<int> colorIdList = new List<int>();

        for (int i = 0; i < processData.SlotDataList.Count; i++)
        {
            LevelProcessSlotData slotData = processData.SlotDataList[i];

            if (slotData != null)
            {
                colorIdList.Add(slotData.RequiredColorId);
            }
        }

        return colorIdList;
    }

    internal List<LevelNodeColorChipData> GetResourceColorChips(LevelResourceData resourceData)
    {
        List<int> colorIdList = GetResourceColorIdList(resourceData);
        List<LevelNodeColorChipData> colorChipDataList = new List<LevelNodeColorChipData>();

        for (int i = 0; i < colorIdList.Count; i++)
        {
            colorChipDataList.Add(new LevelNodeColorChipData(colorIdList[i],
                                                             -1,
                                                             0,
                                                             false,
                                                             false,
                                                             true));
        }

        return colorChipDataList;
    }

    private List<int> GetResourceColorIdList(LevelResourceData resourceData)
    {
        List<int> colorIdList = new List<int>
        {
            resourceData.InitialColorId,
        };

        for (int i = 0; i < resourceData.RuleDataList.Count; i++)
        {
            LevelResourceRuleData ruleData = resourceData.RuleDataList[i];

            if (ruleData == null || ruleData.RuleType != ELevelResourceRuleType.ColorSwitch)
            {
                continue;
            }

            for (int j = 0; j < ruleData.ColorIdList.Count; j++)
            {
                colorIdList.Add(ruleData.ColorIdList[j]);
            }
        }

        return colorIdList;
    }

    internal IReadOnlyList<LevelTestAssignmentPreviewData> GetActiveTestAssignmentPreviewList()
    {
        return CreateActiveTestAssignmentPreviewList();
    }

    internal bool IsSelectedTestSlot(int processId, int slotId)
    {
        return _selectedTestProcessId == processId && _selectedTestSlotId == slotId;
    }

    private List<LevelTestAssignmentPreviewData> CreateActiveTestAssignmentPreviewList()
    {
        List<LevelTestAssignmentPreviewData> previewDataList = new List<LevelTestAssignmentPreviewData>();
        int testCaseIndex = GetVisualizedTestCaseIndex();

        if (_levelSO == null || testCaseIndex < 0 || testCaseIndex >= _levelSO.TestCaseDataList.Count)
        {
            return previewDataList;
        }

        LevelTestCaseData testCaseData = _levelSO.TestCaseDataList[testCaseIndex];

        if (testCaseData == null)
        {
            return previewDataList;
        }

        Dictionary<int, int> nextSelectionOrderByProcessIdDict = new Dictionary<int, int>();

        for (int i = 0; i < testCaseData.AssignedConnectionDataList.Count; i++)
        {
            LevelAssignedConnectionData assignedConnectionData = testCaseData.AssignedConnectionDataList[i];

            if (assignedConnectionData == null ||
                !TryGetProcessDataById(assignedConnectionData.ProcessId, out LevelProcessData processData) ||
                !TryGetResourceDataById(assignedConnectionData.ResourceId, out LevelResourceData resourceData) ||
                !TryGetSlotData(processData, assignedConnectionData.SlotId, out LevelProcessSlotData slotData))
            {
                continue;
            }

            int distance = Mathf.Abs(processData.Row - resourceData.Row) + Mathf.Abs(processData.Column - resourceData.Column);
            int selectionOrder = GetNextSelectionOrder(nextSelectionOrderByProcessIdDict, assignedConnectionData.ProcessId);
            previewDataList.Add(new LevelTestAssignmentPreviewData(assignedConnectionData.ProcessId,
                                                                   assignedConnectionData.SlotId,
                                                                   assignedConnectionData.ResourceId,
                                                                   0,
                                                                   selectionOrder,
                                                                   distance));
        }

        previewDataList.Sort(CompareAssignmentPreview);
        return BuildOrderedAssignmentPreviewList(previewDataList);
    }

    private int GetNextSelectionOrder(Dictionary<int, int> nextSelectionOrderByProcessIdDict, int processId)
    {
        if (!nextSelectionOrderByProcessIdDict.TryGetValue(processId, out int nextSelectionOrder))
        {
            nextSelectionOrder = 0;
        }

        nextSelectionOrderByProcessIdDict[processId] = nextSelectionOrder + 1;
        return nextSelectionOrder;
    }

    private int GetVisualizedTestCaseIndex()
    {
        if (_activeTestCaseIndex >= 0)
        {
            return _activeTestCaseIndex;
        }

        return _lastRunTestCaseIndex;
    }

    private List<LevelTestAssignmentPreviewData> BuildOrderedAssignmentPreviewList(List<LevelTestAssignmentPreviewData> sortedPreviewDataList)
    {
        List<LevelTestAssignmentPreviewData> orderedPreviewDataList = new List<LevelTestAssignmentPreviewData>();
        int currentResourceId = -1;
        int order = 0;

        for (int i = 0; i < sortedPreviewDataList.Count; i++)
        {
            LevelTestAssignmentPreviewData previewData = sortedPreviewDataList[i];

            if (previewData.ResourceId != currentResourceId)
            {
                currentResourceId = previewData.ResourceId;
                order = 1;
            }
            else
            {
                order++;
            }

            orderedPreviewDataList.Add(new LevelTestAssignmentPreviewData(previewData.ProcessId,
                                                                          previewData.SlotId,
                                                                          previewData.ResourceId,
                                                                          order,
                                                                          previewData.SelectionOrder,
                                                                          previewData.Distance));
        }

        return orderedPreviewDataList;
    }

    private int CompareAssignmentPreview(LevelTestAssignmentPreviewData a, LevelTestAssignmentPreviewData b)
    {
        int result = a.ResourceId.CompareTo(b.ResourceId);

        if (result != 0)
        {
            return result;
        }

        result = a.Distance.CompareTo(b.Distance);

        if (result != 0)
        {
            return result;
        }

        result = a.SelectionOrder.CompareTo(b.SelectionOrder);

        if (result != 0)
        {
            return result;
        }

        result = a.ProcessId.CompareTo(b.ProcessId);

        if (result != 0)
        {
            return result;
        }

        return a.SlotId.CompareTo(b.SlotId);
    }

    private int GetAssignmentOrder(int processId, int slotId)
    {
        IReadOnlyList<LevelTestAssignmentPreviewData> assignmentList = GetActiveTestAssignmentPreviewList();

        for (int i = 0; i < assignmentList.Count; i++)
        {
            LevelTestAssignmentPreviewData assignment = assignmentList[i];

            if (assignment.ProcessId == processId && assignment.SlotId == slotId)
            {
                return assignment.Order;
            }
        }

        return 0;
    }

    private int GetProcessExecutionOrder(int processId, int slotId)
    {
        IReadOnlyList<LevelTestAssignmentPreviewData> assignmentList = GetActiveTestAssignmentPreviewList();

        for (int i = 0; i < assignmentList.Count; i++)
        {
            LevelTestAssignmentPreviewData assignment = assignmentList[i];

            if (assignment.ProcessId == processId && assignment.SlotId == slotId)
            {
                return assignment.SelectionOrder + 1;
            }
        }

        return 0;
    }

    private bool IsAssignmentConnectedToSelectedResource(int processId, int slotId)
    {
        if (SelectedResourceId < 0)
        {
            return true;
        }

        IReadOnlyList<LevelTestAssignmentPreviewData> assignmentList = GetActiveTestAssignmentPreviewList();

        for (int i = 0; i < assignmentList.Count; i++)
        {
            LevelTestAssignmentPreviewData assignment = assignmentList[i];

            if (assignment.ProcessId == processId &&
                assignment.SlotId == slotId &&
                assignment.ResourceId == SelectedResourceId)
            {
                return true;
            }
        }

        return false;
    }

    internal ELevelTestConnectionVisualState GetTestConnectionVisualState(int processId, int slotId, int resourceId)
    {
        if (_lastTestSimulationReport == null ||
            GetVisualizedTestCaseIndex() != _lastRunTestCaseIndex ||
            _selectedTestRoundIndex < 0 ||
            _selectedTestRoundIndex >= _lastTestSimulationReport.RoundResultList.Length)
        {
            return ELevelTestConnectionVisualState.None;
        }

        if (!TryGetLastRunConnectionId(processId, slotId, resourceId, out int connectionId))
        {
            return ELevelTestConnectionVisualState.None;
        }

        RoundResult roundResult = _lastTestSimulationReport.RoundResultList[_selectedTestRoundIndex];

        if (ContainsId(roundResult.BlockedConnectionIdList, connectionId))
        {
            return ELevelTestConnectionVisualState.Blocked;
        }

        if (ContainsId(roundResult.WaitingConnectionIdList, connectionId))
        {
            return ELevelTestConnectionVisualState.Waiting;
        }

        if (ContainsId(roundResult.RequeuedConnectionIdList, connectionId))
        {
            return ELevelTestConnectionVisualState.Waiting;
        }

        if (ContainsId(roundResult.OccupiedConnectionIdList, connectionId))
        {
            return ELevelTestConnectionVisualState.Occupied;
        }

        if (ContainsId(roundResult.ReleasedConnectionIdList, connectionId))
        {
            return ELevelTestConnectionVisualState.None;
        }

        return ELevelTestConnectionVisualState.None;
    }

    internal bool IsProcessCompletedBySelectedRound(int processId)
    {
        if (_lastTestSimulationReport == null ||
            GetVisualizedTestCaseIndex() != _lastRunTestCaseIndex ||
            _selectedTestRoundIndex < 0)
        {
            return false;
        }

        int maxRoundIndex = Mathf.Min(_selectedTestRoundIndex, _lastTestSimulationReport.RoundResultList.Length - 1);

        for (int i = 0; i <= maxRoundIndex; i++)
        {
            RoundResult roundResult = _lastTestSimulationReport.RoundResultList[i];

            if (ContainsId(roundResult.CompletedProcessIdList, processId))
            {
                return true;
            }
        }

        return false;
    }

    internal bool IsConnectionVisibleBySelectedRound(int processId, int slotId, int resourceId)
    {
        if (_lastTestSimulationReport == null ||
            GetVisualizedTestCaseIndex() != _lastRunTestCaseIndex ||
            _selectedTestRoundIndex < 0)
        {
            return true;
        }

        if (!TryGetLastRunConnectionId(processId, slotId, resourceId, out int connectionId))
        {
            return true;
        }

        int maxRoundIndex = Mathf.Min(_selectedTestRoundIndex, _lastTestSimulationReport.RoundResultList.Length - 1);

        for (int i = 0; i <= maxRoundIndex; i++)
        {
            RoundResult roundResult = _lastTestSimulationReport.RoundResultList[i];

            if (HasConnectionAppearedInRound(roundResult, connectionId))
            {
                return true;
            }
        }

        return false;
    }

    private bool TryGetLastRunConnectionId(int processId, int slotId, int resourceId, out int connectionId)
    {
        for (int i = 0; i < _lastTestConnectionRunDataList.Count; i++)
        {
            LevelTestConnectionRunData runData = _lastTestConnectionRunDataList[i];

            if (runData.ProcessId == processId &&
                runData.SlotId == slotId &&
                runData.ResourceId == resourceId)
            {
                connectionId = runData.ConnectionId;
                return true;
            }
        }

        connectionId = -1;
        return false;
    }

    private bool HasConnectionAppearedInRound(RoundResult roundResult, int connectionId)
    {
        return ContainsId(roundResult.OccupiedConnectionIdList, connectionId) ||
               ContainsId(roundResult.WaitingConnectionIdList, connectionId) ||
               ContainsId(roundResult.RequeuedConnectionIdList, connectionId) ||
               ContainsId(roundResult.ReleasedConnectionIdList, connectionId) ||
               ContainsId(roundResult.BlockedConnectionIdList, connectionId);
    }

    private bool ContainsId(IReadOnlyList<int> idList, int id)
    {
        for (int i = 0; i < idList.Count; i++)
        {
            if (idList[i] == id)
            {
                return true;
            }
        }

        return false;
    }

    internal bool HasResourceIssue(LevelResourceData resourceData)
    {
        return HasResourceWarning(resourceData);
    }

    private bool HasResourceWarning(LevelResourceData resourceData)
    {
        return resourceData.InitialColorId <= 0 ||
               resourceData.Capacity <= 0 ||
               resourceData.RuleDataList.Count == 0;
    }

    private Label CreateEmptyState(string text)
    {
        Label label = new Label(text);
        label.style.flexGrow = 1f;
        label.style.unityTextAlign = TextAnchor.MiddleCenter;
        label.style.color = new StyleColor(new Color(0.75f, 0.78f, 0.82f));
        return label;
    }

    private void HandleCellClick(int row, int column)
    {
        if (_levelSO == null || _serializedObject == null)
        {
            return;
        }

        if (_currentTool == ELevelEditorTool.Select)
        {
            SelectCell(row, column);
            return;
        }

        if (_currentTool == ELevelEditorTool.AddProcess)
        {
            AddProcess(row, column);
            return;
        }

        if (_currentTool == ELevelEditorTool.AddResource)
        {
            AddResource(row, column);
            return;
        }

        if (_currentTool == ELevelEditorTool.AddRelay)
        {
            _relayDraftMessage = "Relay는 보드의 Resource 두 개를 순서대로 선택해 생성합니다.";
            RefreshInspector();
            return;
        }

        if (_currentTool == ELevelEditorTool.Erase)
        {
            EraseCell(row, column);
        }
    }

    private void SelectCell(int row, int column)
    {
        _selectedRow = row;
        _selectedColumn = column;
        _selectedRelayIndex = -1;
        RefreshBoard();
        RefreshInspector();
    }

    private void RefreshInspector()
    {
        if (_inspectorContainer == null)
        {
            return;
        }

        _inspectorContainer.Clear();
        _inspectorContainer.Add(CreateSectionTitle("인스펙터"));

        if (_levelSO == null || _serializedObject == null)
        {
            _inspectorContainer.Add(CreateEmptyState("선택된 LevelSO가 없습니다."));
            return;
        }

        _serializedObject.Update();
        BuildLevelInspector();
        BuildSelectedColorInspector();

        if (_selectedRow < 0 || _selectedColumn < 0)
        {
            _inspectorContainer.Add(new Label("보드 칸을 선택해 주세요."));
            BuildRelaySummary();
            BuildTestCaseSummary();
            return;
        }

        _inspectorContainer.Add(CreateSectionTitle($"칸 {_selectedRow}, {_selectedColumn}"));

        SerializedProperty processProperty = FindProcessProperty(_selectedRow, _selectedColumn, out int processIndex);
        SerializedProperty resourceProperty = FindResourceProperty(_selectedRow, _selectedColumn, out int resourceIndex);

        if (processProperty == null && resourceProperty == null)
        {
            _inspectorContainer.Add(new Label("빈 칸입니다."));
            BuildQuickAddButtons();
        }

        if (processProperty != null)
        {
            BuildProcessInspector(processProperty, processIndex);
        }

        if (resourceProperty != null)
        {
            BuildResourceInspector(resourceProperty, resourceIndex);
        }

        BuildRelaySummary();
        BuildTestCaseSummary();
    }

    private void BuildLevelInspector()
    {
        VisualElement box = CreateBox();
        _inspectorContainer.Add(box);

        box.Add(CreateSectionTitle("레벨"));
        AddIntegerField(box, "ID", "_id");
        AddIntegerField(box, "행", "_rowCount");
        AddIntegerField(box, "열", "_columnCount");
    }

    private void BuildSelectedColorInspector()
    {
        VisualElement box = CreateBox();
        _inspectorContainer.Add(box);

        box.Add(CreateSectionTitle("현재 색"));

        VisualElement row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.alignItems = Align.Center;
        box.Add(row);

        row.Add(CreateColorSwatch(_selectedColorId, 22f));

        Label label = new Label(GetColorLabel(_selectedColorId));
        label.style.color = new StyleColor(Color.white);
        row.Add(label);
    }

    private void BuildQuickAddButtons()
    {
        VisualElement row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        _inspectorContainer.Add(row);

        row.Add(CreateHeaderButton("프로세스 추가", () => AddProcess(_selectedRow, _selectedColumn)));
        row.Add(CreateHeaderButton("리소스 추가", () => AddResource(_selectedRow, _selectedColumn)));
    }

    private void BuildProcessInspector(SerializedProperty processProperty, int processIndex)
    {
        VisualElement box = CreateBox();
        _inspectorContainer.Add(box);

        box.Add(CreateSectionTitle("프로세스"));
        box.Add(CreateNodePreview(true,
                                  GetProcessColorIdList(processProperty)));
        AddReadOnlyIntegerField(box, "위치 ID", processProperty.FindPropertyRelative("_id").propertyPath);
        AddNodePositionField(box, "행", processProperty.propertyPath, "_row");
        AddNodePositionField(box, "열", processProperty.propertyPath, "_column");

        SerializedProperty slotListProperty = processProperty.FindPropertyRelative("_slotDataList");
        box.Add(CreateSectionTitle("슬롯"));

        for (int i = 0; i < slotListProperty.arraySize; i++)
        {
            SerializedProperty slotProperty = slotListProperty.GetArrayElementAtIndex(i);
            BuildSlotInspector(box, slotProperty, slotListProperty.propertyPath, i);
        }

        box.Add(CreateHeaderButton("슬롯 추가", () => AddSlot(slotListProperty.propertyPath)));
        box.Add(CreateHeaderButton("프로세스 삭제", () => DeleteArrayElement("_processDataList", processIndex)));
    }

    private void BuildSlotInspector(VisualElement parent, SerializedProperty slotProperty, string slotListPath, int slotIndex)
    {
        VisualElement box = CreateBox();
        parent.Add(box);

        box.Add(new Label($"슬롯 {slotIndex}"));
        AddIntegerField(box, "ID", slotProperty.FindPropertyRelative("_id").propertyPath);
        AddColorIntegerField(box, "필요 색", slotProperty.FindPropertyRelative("_requiredColorId").propertyPath);
        AddIntegerField(box, "선택 순서", slotProperty.FindPropertyRelative("_selectionOrder").propertyPath);
        box.Add(CreateHeaderButton("슬롯 제거", () => DeleteArrayElement(slotListPath, slotIndex)));
    }

    private List<int> GetProcessColorIdList(SerializedProperty processProperty)
    {
        List<int> colorIdList = new List<int>();
        SerializedProperty slotListProperty = processProperty.FindPropertyRelative("_slotDataList");

        for (int i = 0; i < slotListProperty.arraySize; i++)
        {
            SerializedProperty slotProperty = slotListProperty.GetArrayElementAtIndex(i);
            colorIdList.Add(slotProperty.FindPropertyRelative("_requiredColorId").intValue);
        }

        return colorIdList;
    }

    private void BuildResourceInspector(SerializedProperty resourceProperty, int resourceIndex)
    {
        VisualElement box = CreateBox();
        _inspectorContainer.Add(box);

        box.Add(CreateSectionTitle("리소스"));
        box.Add(CreateNodePreview(false,
                                  GetResourceColorIdList(resourceProperty)));
        AddReadOnlyIntegerField(box, "위치 ID", resourceProperty.FindPropertyRelative("_id").propertyPath);
        AddNodePositionField(box, "행", resourceProperty.propertyPath, "_row");
        AddNodePositionField(box, "열", resourceProperty.propertyPath, "_column");
        AddColorIntegerField(box, "초기 색", resourceProperty.FindPropertyRelative("_initialColorId").propertyPath);
        AddIntegerField(box, "수용량", resourceProperty.FindPropertyRelative("_capacity").propertyPath);

        SerializedProperty ruleListProperty = resourceProperty.FindPropertyRelative("_ruleDataList");
        box.Add(CreateSectionTitle("규칙"));

        for (int i = 0; i < ruleListProperty.arraySize; i++)
        {
            SerializedProperty ruleProperty = ruleListProperty.GetArrayElementAtIndex(i);
            BuildRuleInspector(box, ruleProperty, ruleListProperty.propertyPath, i);
        }

        box.Add(CreateHeaderButton("규칙 추가", () => AddRule(ruleListProperty.propertyPath)));
        box.Add(CreateHeaderButton("리소스 삭제", () => DeleteArrayElement("_resourceDataList", resourceIndex)));
    }

    private VisualElement CreateNodePreview(bool isProcess, IReadOnlyList<int> colorIdList)
    {
        VisualElement row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.alignItems = Align.Center;
        row.style.marginBottom = 8f;
        row.Add(CreateNodeVisual(isProcess, colorIdList, false));

        Label label = new Label(isProcess ? "프로세스는 원형으로 표시됩니다." : "리소스는 사각형으로 표시됩니다.");
        label.style.marginLeft = 10f;
        label.style.color = new StyleColor(new Color(0.75f, 0.78f, 0.82f));
        label.style.whiteSpace = WhiteSpace.Normal;
        row.Add(label);
        return row;
    }

    private List<int> GetResourceColorIdList(SerializedProperty resourceProperty)
    {
        List<int> colorIdList = new List<int>
        {
            resourceProperty.FindPropertyRelative("_initialColorId").intValue,
        };

        SerializedProperty ruleListProperty = resourceProperty.FindPropertyRelative("_ruleDataList");

        for (int i = 0; i < ruleListProperty.arraySize; i++)
        {
            SerializedProperty ruleProperty = ruleListProperty.GetArrayElementAtIndex(i);
            ELevelResourceRuleType ruleType = (ELevelResourceRuleType)ruleProperty.FindPropertyRelative("_ruleType").enumValueIndex;

            if (ruleType != ELevelResourceRuleType.ColorSwitch)
            {
                continue;
            }

            SerializedProperty colorIdListProperty = ruleProperty.FindPropertyRelative("_colorIdList");

            for (int j = 0; j < colorIdListProperty.arraySize; j++)
            {
                colorIdList.Add(colorIdListProperty.GetArrayElementAtIndex(j).intValue);
            }
        }

        return colorIdList;
    }

    private void BuildRuleInspector(VisualElement parent, SerializedProperty ruleProperty, string ruleListPath, int ruleIndex)
    {
        VisualElement box = CreateBox();
        parent.Add(box);

        box.Add(new Label($"규칙 {ruleIndex}"));
        AddEnumField<ELevelResourceRuleType>(box, "종류", ruleProperty.FindPropertyRelative("_ruleType").propertyPath);

        ELevelResourceRuleType ruleType = (ELevelResourceRuleType)ruleProperty.FindPropertyRelative("_ruleType").enumValueIndex;

        if (ruleType == ELevelResourceRuleType.ColorSwitch)
        {
            BuildColorIdListInspector(box, ruleProperty.FindPropertyRelative("_colorIdList"));
        }

        if (ruleType == ELevelResourceRuleType.Clock)
        {
            AddEnumField<EClockMode>(box, "Clock 모드", ruleProperty.FindPropertyRelative("_clockMode").propertyPath);
            AddIntegerField(box, "Clock 라운드", ruleProperty.FindPropertyRelative("_clockRoundCount").propertyPath);
        }

        VisualElement buttonRow = new VisualElement();
        buttonRow.style.flexDirection = FlexDirection.Row;
        box.Add(buttonRow);

        buttonRow.Add(CreateHeaderButton("위로", () => MoveArrayElement(ruleListPath, ruleIndex, ruleIndex - 1)));
        buttonRow.Add(CreateHeaderButton("아래로", () => MoveArrayElement(ruleListPath, ruleIndex, ruleIndex + 1)));
        buttonRow.Add(CreateHeaderButton("제거", () => DeleteArrayElement(ruleListPath, ruleIndex)));
    }

    private void BuildColorIdListInspector(VisualElement parent, SerializedProperty colorIdListProperty)
    {
        parent.Add(CreateSectionTitle("ColorSwitch 색 목록"));

        for (int i = 0; i < colorIdListProperty.arraySize; i++)
        {
            int colorIndex = i;
            SerializedProperty colorProperty = colorIdListProperty.GetArrayElementAtIndex(i);
            VisualElement row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            parent.Add(row);

            row.Add(CreateColorSwatch(colorProperty.intValue, 18f));
            AddIntegerField(row, $"색 {i}", colorProperty.propertyPath);
            row.Add(CreateHeaderButton("선택 색 적용", () => SetIntProperty(colorProperty.propertyPath, _selectedColorId)));
            row.Add(CreateHeaderButton("제거", () => DeleteArrayElement(colorIdListProperty.propertyPath, colorIndex)));
        }

        parent.Add(CreateHeaderButton("선택 색 추가", () => AddColorId(colorIdListProperty.propertyPath)));
    }

    private void BuildRelaySummary()
    {
        VisualElement box = CreateBox();
        _inspectorContainer.Add(box);
        box.Add(CreateSectionTitle("Relay 편집"));

        BuildRelayDraftPanel(box);

        if (_levelSO.RelayDataList.Count == 0)
        {
            box.Add(new Label("생성된 Relay가 없습니다."));
            return;
        }

        for (int i = 0; i < _levelSO.RelayDataList.Count; i++)
        {
            BuildRelayInspector(box, i);
        }
    }

    private void BuildTestCaseSummary()
    {
        VisualElement box = CreateBox();
        _inspectorContainer.Add(box);
        box.Add(CreateSectionTitle("테스트 케이스"));
        BuildSolutionFinderPanel(box);

        SerializedProperty testCaseListProperty = _serializedObject.FindProperty("_testCaseDataList");

        if (testCaseListProperty == null)
        {
            box.Add(new Label("테스트 케이스 데이터를 찾을 수 없습니다."));
            return;
        }

        if (testCaseListProperty.arraySize == 0)
        {
            box.Add(new Label("생성된 테스트 케이스가 없습니다."));
        }

        for (int i = 0; i < testCaseListProperty.arraySize; i++)
        {
            BuildTestCaseInspector(box,
                                   testCaseListProperty.GetArrayElementAtIndex(i),
                                   testCaseListProperty.propertyPath,
                                   i);
        }

        box.Add(CreateHeaderButton("테스트 케이스 추가", AddTestCase));
    }

    private void BuildSolutionFinderPanel(VisualElement parent)
    {
        VisualElement box = CreateNestedBox();
        parent.Add(box);

        box.Add(CreateSectionTitle("자동 해 찾기 / 별 기준"));

        SerializedProperty starThresholdProperty = _serializedObject.FindProperty("_starThresholdData");

        if (starThresholdProperty == null)
        {
            box.Add(new Label("별 기준 데이터를 찾을 수 없습니다."));
        }
        else
        {
            AddIntegerField(box, "3별 라운드", starThresholdProperty.FindPropertyRelative("_threeStarRoundCount").propertyPath);
            AddIntegerField(box, "2별 라운드", starThresholdProperty.FindPropertyRelative("_twoStarRoundCount").propertyPath);
            AddIntegerField(box, "1별 라운드", starThresholdProperty.FindPropertyRelative("_oneStarRoundCount").propertyPath);
        }

        if (_lastSolveReport != null)
        {
            Label reportLabel = new Label(BuildSolveReportSummary(_lastSolveReport));
            reportLabel.style.whiteSpace = WhiteSpace.Normal;
            reportLabel.style.color = new StyleColor(new Color(0.78f, 0.82f, 0.88f));
            box.Add(reportLabel);
        }

        if (_lastDifficultyReport != null)
        {
            Label difficultyLabel = new Label(BuildDifficultySummary(_lastDifficultyReport));
            difficultyLabel.style.whiteSpace = WhiteSpace.Normal;
            difficultyLabel.style.color = new StyleColor(new Color(0.88f, 0.82f, 0.62f));
            box.Add(difficultyLabel);
        }

        VisualElement buttonRow = CreateButtonRow();
        buttonRow.Add(CreateHeaderButton("최적 해 찾기", RunSolutionFinder));

        Button saveStarButton = CreateHeaderButton("별 기준 저장", SaveLastStarThresholdRecommendation);
        saveStarButton.SetEnabled(HasUsableSolveRecommendation());
        buttonRow.Add(saveStarButton);

        Button updateTestButton = CreateHeaderButton("Auto Optimal 테스트 갱신", UpdateAutoOptimalTestCaseFromLastSolve);
        updateTestButton.SetEnabled(HasUsableSolveCandidate());
        buttonRow.Add(updateTestButton);

        box.Add(buttonRow);
    }

    private void BuildTestCaseInspector(VisualElement parent,
                                        SerializedProperty testCaseProperty,
                                        string testCaseListPath,
                                        int testCaseIndex)
    {
        VisualElement box = CreateNestedBox();
        parent.Add(box);

        SerializedProperty nameProperty = testCaseProperty.FindPropertyRelative("_name");
        string title = string.IsNullOrEmpty(nameProperty.stringValue) ?
            $"테스트 {testCaseIndex + 1}" :
            nameProperty.stringValue;

        box.Add(CreateSectionTitle(title));
        AddStringField(box, "이름", nameProperty.propertyPath);
        AddIntegerField(box, "최대 라운드", testCaseProperty.FindPropertyRelative("_maxRoundCount").propertyPath);
        AddEnumField<ESimulationEndState>(box, "예상 결과", testCaseProperty.FindPropertyRelative("_expectedEndState").propertyPath);
        BuildTestCaseEditControls(box, testCaseIndex);

        SerializedProperty assignedConnectionListProperty = testCaseProperty.FindPropertyRelative("_assignedConnectionDataList");
        box.Add(CreateSectionTitle("예약 연결"));

        if (assignedConnectionListProperty.arraySize == 0)
        {
            box.Add(new Label("예약 연결이 없습니다. 모든 슬롯이 예약되어야 시뮬레이션이 정상 실행됩니다."));
        }

        for (int i = 0; i < assignedConnectionListProperty.arraySize; i++)
        {
            BuildAssignedConnectionInspector(box,
                                             assignedConnectionListProperty.GetArrayElementAtIndex(i),
                                             assignedConnectionListProperty.propertyPath,
                                             i);
        }

        VisualElement buttonRow = CreateButtonRow();
        buttonRow.Add(CreateHeaderButton("실행", () => RunTestCase(testCaseIndex)));
        buttonRow.Add(CreateHeaderButton("제거", () => DeleteTestCase(testCaseListPath, testCaseIndex)));
        box.Add(buttonRow);
    }

    private void BuildTestCaseEditControls(VisualElement parent, int testCaseIndex)
    {
        bool isActive = _activeTestCaseIndex == testCaseIndex;
        VisualElement controlBox = CreateNestedBox();
        parent.Add(controlBox);

        Label guideLabel = new Label(isActive ?
            GetActiveTestCaseGuideText() :
            "편집 시작 후 보드에서 Process의 색 슬롯을 누르고 Resource를 클릭해 예약 연결을 만듭니다.");
        guideLabel.style.whiteSpace = WhiteSpace.Normal;
        guideLabel.style.color = new StyleColor(new Color(0.78f, 0.82f, 0.88f));
        controlBox.Add(guideLabel);

        if (isActive && _selectedTestProcessId >= 0 && _selectedTestSlotId >= 0)
        {
            controlBox.Add(new Label($"선택 슬롯: {GetProcessDisplayName(_selectedTestProcessId)} / 슬롯 {_selectedTestSlotId}"));
        }

        VisualElement buttonRow = CreateButtonRow();

        if (isActive)
        {
            buttonRow.Add(CreateHeaderButton("편집 종료", DeactivateTestCaseEdit));
            buttonRow.Add(CreateHeaderButton("슬롯 선택 해제", ClearSelectedTestSlot));
        }
        else
        {
            buttonRow.Add(CreateHeaderButton("편집 시작", () => ActivateTestCaseEdit(testCaseIndex)));
        }

        controlBox.Add(buttonRow);
    }

    private string GetActiveTestCaseGuideText()
    {
        if (!string.IsNullOrEmpty(_testCaseEditMessage))
        {
            return _testCaseEditMessage;
        }

        return "Process 색 슬롯을 클릭한 뒤 연결할 Resource를 클릭하세요. Resource를 선택하면 연결된 슬롯과 순서가 보드에 강조됩니다.";
    }

    private void BuildAssignedConnectionInspector(VisualElement parent,
                                                  SerializedProperty assignedConnectionProperty,
                                                  string assignedConnectionListPath,
                                                  int assignedConnectionIndex)
    {
        VisualElement box = CreateNestedBox();
        parent.Add(box);

        int processId = assignedConnectionProperty.FindPropertyRelative("_processId").intValue;
        int slotId = assignedConnectionProperty.FindPropertyRelative("_slotId").intValue;
        int resourceId = assignedConnectionProperty.FindPropertyRelative("_resourceId").intValue;
        int order = GetAssignmentOrder(processId, slotId);

        Label label = new Label(order > 0 ?
            $"#{order}  {CreateConnectionLabel(processId, slotId, resourceId)}" :
            CreateConnectionLabel(processId, slotId, resourceId));
        label.style.whiteSpace = WhiteSpace.Normal;
        box.Add(label);
        box.Add(CreateHeaderButton("연결 제거", () => DeleteArrayElement(assignedConnectionListPath, assignedConnectionIndex)));
    }

    private void BuildRelayDraftPanel(VisualElement parent)
    {
        VisualElement draftBox = CreateNestedBox();
        parent.Add(draftBox);
        draftBox.Add(CreateSectionTitle("새 Relay"));

        string message = string.IsNullOrEmpty(_relayDraftMessage) ?
            "Relay 추가 툴을 선택하고 Resource 두 개를 순서대로 누르세요." :
            _relayDraftMessage;

        Label messageLabel = new Label(message);
        messageLabel.style.color = new StyleColor(new Color(0.78f, 0.82f, 0.88f));
        messageLabel.style.whiteSpace = WhiteSpace.Normal;
        draftBox.Add(messageLabel);

        if (_relayDraftFirstResourceId >= 0)
        {
            draftBox.Add(new Label($"첫 번째: {GetResourceDisplayName(_relayDraftFirstResourceId)}"));
        }

        if (_relayDraftSecondResourceId >= 0)
        {
            draftBox.Add(new Label($"두 번째: {GetResourceDisplayName(_relayDraftSecondResourceId)}"));
        }

        if (!HasRelayDraftPair)
        {
            draftBox.Add(CreateHeaderButton("선택 초기화", () =>
            {
                ClearRelayDraft();
                RefreshAll();
            }));
            return;
        }

        EnumField relayTypeField = new EnumField("타입", _relayDraftType);
        relayTypeField.RegisterValueChangedCallback(changeEvent =>
        {
            _relayDraftType = (ERelayType)changeEvent.newValue;
            EnsureRelayDraftSender();
            RefreshAll();
        });
        relayTypeField.style.marginTop = 6f;
        draftBox.Add(relayTypeField);

        if (_relayDraftType == ERelayType.Transfer)
        {
            BuildDraftSenderSelector(draftBox);
        }

        bool canCreate = CanCreateRelayDraft(out string reason);

        if (!canCreate)
        {
            Label warningLabel = new Label(reason);
            warningLabel.style.color = new StyleColor(WarningColor);
            warningLabel.style.whiteSpace = WhiteSpace.Normal;
            draftBox.Add(warningLabel);
        }

        VisualElement buttonRow = CreateButtonRow();
        Button createButton = CreateHeaderButton("Relay 생성", AddRelayFromDraft);
        createButton.SetEnabled(canCreate);
        buttonRow.Add(createButton);
        buttonRow.Add(CreateHeaderButton("선택 초기화", () =>
        {
            ClearRelayDraft();
            RefreshAll();
        }));
        draftBox.Add(buttonRow);
    }

    private void BuildDraftSenderSelector(VisualElement parent)
    {
        parent.Add(CreateSectionTitle("Sender"));

        VisualElement row = CreateButtonRow();
        row.Add(CreateSenderButton(_relayDraftFirstResourceId, () =>
        {
            _relayDraftSenderResourceId = _relayDraftFirstResourceId;
            RefreshAll();
        }));
        row.Add(CreateSenderButton(_relayDraftSecondResourceId, () =>
        {
            _relayDraftSenderResourceId = _relayDraftSecondResourceId;
            RefreshAll();
        }));
        parent.Add(row);
    }

    private Button CreateSenderButton(int resourceId, Action action)
    {
        Button button = CreateHeaderButton(GetResourceDisplayName(resourceId), action);
        bool isValidSender = IsCapacityOneResource(resourceId);
        button.SetEnabled(isValidSender);

        if (_relayDraftSenderResourceId == resourceId)
        {
            button.style.backgroundColor = new StyleColor(new Color(0.30f, 0.42f, 0.58f));
        }

        return button;
    }

    private void BuildRelayInspector(VisualElement parent, int relayIndex)
    {
        SerializedProperty relayProperty = FindRelayProperty(relayIndex);

        if (relayProperty == null)
        {
            return;
        }

        VisualElement relayBox = CreateNestedBox();
        parent.Add(relayBox);

        if (_selectedRelayIndex == relayIndex)
        {
            SetBorder(relayBox, 1f, SelectedColor);
        }

        int relayId = relayProperty.FindPropertyRelative("_id").intValue;
        int firstResourceId = relayProperty.FindPropertyRelative("_firstResourceId").intValue;
        int secondResourceId = relayProperty.FindPropertyRelative("_secondResourceId").intValue;
        int senderResourceId = relayProperty.FindPropertyRelative("_senderResourceId").intValue;
        ERelayType relayType = (ERelayType)relayProperty.FindPropertyRelative("_relayType").enumValueIndex;

        relayBox.Add(CreateSectionTitle($"Relay {relayId}"));
        relayBox.Add(new Label($"{GetResourceDisplayName(firstResourceId)} ↔ {GetResourceDisplayName(secondResourceId)}"));

        VisualElement headerRow = CreateButtonRow();
        headerRow.Add(CreateHeaderButton("선택", () =>
        {
            _selectedRelayIndex = relayIndex;
            ClearRelayDraft();
            RefreshAll();
        }));
        headerRow.Add(CreateHeaderButton("삭제", () => DeleteRelay(relayIndex)));
        relayBox.Add(headerRow);

        EnumField relayTypeField = new EnumField("타입", relayType);
        relayTypeField.RegisterValueChangedCallback(changeEvent => SetRelayType(relayIndex, (ERelayType)changeEvent.newValue));
        relayBox.Add(relayTypeField);

        if (relayType == ERelayType.Link)
        {
            relayBox.Add(new Label("Link는 방향 없이 두 Resource 사용을 서로 막습니다."));
            return;
        }

        relayBox.Add(new Label($"Sender: {GetResourceDisplayName(senderResourceId)}"));
        BuildExistingRelaySenderSelector(relayBox, relayIndex, firstResourceId, secondResourceId, senderResourceId);
    }

    private void BuildExistingRelaySenderSelector(VisualElement parent,
                                                  int relayIndex,
                                                  int firstResourceId,
                                                  int secondResourceId,
                                                  int senderResourceId)
    {
        VisualElement row = CreateButtonRow();
        row.Add(CreateExistingSenderButton(firstResourceId, senderResourceId, () => SetRelaySender(relayIndex, firstResourceId)));
        row.Add(CreateExistingSenderButton(secondResourceId, senderResourceId, () => SetRelaySender(relayIndex, secondResourceId)));
        parent.Add(row);

        if (!IsCapacityOneResource(firstResourceId) &&
            !IsCapacityOneResource(secondResourceId))
        {
            Label warningLabel = new Label("Transfer Sender 후보가 없습니다. Sender Resource의 Capacity는 1이어야 합니다.");
            warningLabel.style.color = new StyleColor(WarningColor);
            warningLabel.style.whiteSpace = WhiteSpace.Normal;
            parent.Add(warningLabel);
        }
    }

    private Button CreateExistingSenderButton(int resourceId, int senderResourceId, Action action)
    {
        Button button = CreateHeaderButton(GetResourceDisplayName(resourceId), action);
        bool isValidSender = IsCapacityOneResource(resourceId);
        button.SetEnabled(isValidSender);

        if (senderResourceId == resourceId)
        {
            button.style.backgroundColor = new StyleColor(new Color(0.30f, 0.42f, 0.58f));
        }

        return button;
    }

    private bool CanCreateRelayDraft(out string reason)
    {
        if (!HasRelayDraftPair)
        {
            reason = "Resource 두 개를 먼저 선택해야 합니다.";
            return false;
        }

        if (IsDuplicateRelay(_relayDraftType,
                             _relayDraftFirstResourceId,
                             _relayDraftSecondResourceId,
                             GetDraftStoredSenderResourceId(),
                             -1))
        {
            reason = "같은 설정의 Relay가 이미 있습니다.";
            return false;
        }

        if (_relayDraftType == ERelayType.Link)
        {
            reason = string.Empty;
            return true;
        }

        if (_relayDraftSenderResourceId != _relayDraftFirstResourceId &&
            _relayDraftSenderResourceId != _relayDraftSecondResourceId)
        {
            reason = "Transfer Sender는 선택한 두 Resource 중 하나여야 합니다.";
            return false;
        }

        if (!IsCapacityOneResource(_relayDraftSenderResourceId))
        {
            reason = "Transfer Sender Resource의 Capacity는 1이어야 합니다.";
            return false;
        }

        reason = string.Empty;
        return true;
    }

    private int GetDraftStoredSenderResourceId()
    {
        return _relayDraftType == ERelayType.Link ? 0 : _relayDraftSenderResourceId;
    }

    private void AddRelayFromDraft()
    {
        if (!CanCreateRelayDraft(out string reason))
        {
            _relayDraftMessage = reason;
            RefreshInspector();
            return;
        }

        BeginEdit("Add Relay");
        SerializedProperty relayListProperty = _serializedObject.FindProperty("_relayDataList");
        int arrayIndex = relayListProperty.arraySize;
        relayListProperty.InsertArrayElementAtIndex(arrayIndex);

        SerializedProperty relayProperty = relayListProperty.GetArrayElementAtIndex(arrayIndex);
        relayProperty.FindPropertyRelative("_id").intValue = GetNextRelayId();
        relayProperty.FindPropertyRelative("_relayType").enumValueIndex = (int)_relayDraftType;
        relayProperty.FindPropertyRelative("_firstResourceId").intValue = _relayDraftFirstResourceId;
        relayProperty.FindPropertyRelative("_secondResourceId").intValue = _relayDraftSecondResourceId;
        relayProperty.FindPropertyRelative("_senderResourceId").intValue = GetDraftStoredSenderResourceId();

        _selectedRelayIndex = arrayIndex;
        ClearRelayDraft();
        EndEdit(clearSolutionFinderState: false);
    }

    private void SetRelayType(int relayIndex, ERelayType relayType)
    {
        SerializedProperty relayProperty = FindRelayProperty(relayIndex);

        if (relayProperty == null)
        {
            return;
        }

        int firstResourceId = relayProperty.FindPropertyRelative("_firstResourceId").intValue;
        int secondResourceId = relayProperty.FindPropertyRelative("_secondResourceId").intValue;
        int senderResourceId = GetDefaultRelaySender(relayType, firstResourceId, secondResourceId);

        if (IsDuplicateRelay(relayType, firstResourceId, secondResourceId, senderResourceId, relayIndex))
        {
            _relayDraftMessage = "같은 설정의 Relay가 이미 있어 타입을 변경할 수 없습니다.";
            RefreshInspector();
            return;
        }

        BeginEdit("Edit Relay Type");
        relayProperty = FindRelayProperty(relayIndex);
        relayProperty.FindPropertyRelative("_relayType").enumValueIndex = (int)relayType;
        relayProperty.FindPropertyRelative("_senderResourceId").intValue = senderResourceId;
        _selectedRelayIndex = relayIndex;
        EndEdit(clearSolutionFinderState: false);
    }

    private int GetDefaultRelaySender(ERelayType relayType, int firstResourceId, int secondResourceId)
    {
        if (relayType == ERelayType.Link)
        {
            return 0;
        }

        if (IsCapacityOneResource(firstResourceId))
        {
            return firstResourceId;
        }

        if (IsCapacityOneResource(secondResourceId))
        {
            return secondResourceId;
        }

        return firstResourceId;
    }

    private void SetRelaySender(int relayIndex, int senderResourceId)
    {
        SerializedProperty relayProperty = FindRelayProperty(relayIndex);

        if (relayProperty == null || !IsCapacityOneResource(senderResourceId))
        {
            return;
        }

        ERelayType relayType = (ERelayType)relayProperty.FindPropertyRelative("_relayType").enumValueIndex;
        int firstResourceId = relayProperty.FindPropertyRelative("_firstResourceId").intValue;
        int secondResourceId = relayProperty.FindPropertyRelative("_secondResourceId").intValue;

        if (relayType != ERelayType.Transfer ||
            (senderResourceId != firstResourceId && senderResourceId != secondResourceId))
        {
            return;
        }

        if (IsDuplicateRelay(relayType, firstResourceId, secondResourceId, senderResourceId, relayIndex))
        {
            _relayDraftMessage = "같은 설정의 Relay가 이미 있어 Sender를 변경할 수 없습니다.";
            RefreshInspector();
            return;
        }

        BeginEdit("Edit Relay Sender");
        relayProperty = FindRelayProperty(relayIndex);
        relayProperty.FindPropertyRelative("_senderResourceId").intValue = senderResourceId;
        _selectedRelayIndex = relayIndex;
        EndEdit(clearSolutionFinderState: false);
    }

    private void DeleteRelay(int relayIndex)
    {
        BeginEdit("Delete Relay");
        SerializedProperty relayListProperty = _serializedObject.FindProperty("_relayDataList");

        if (relayListProperty != null &&
            relayIndex >= 0 &&
            relayIndex < relayListProperty.arraySize)
        {
            relayListProperty.DeleteArrayElementAtIndex(relayIndex);
        }

        if (_selectedRelayIndex == relayIndex)
        {
            _selectedRelayIndex = -1;
        }
        else if (_selectedRelayIndex > relayIndex)
        {
            _selectedRelayIndex--;
        }

        EndEdit(clearSolutionFinderState: false);
    }

    private SerializedProperty FindRelayProperty(int relayIndex)
    {
        if (_serializedObject == null)
        {
            return null;
        }

        SerializedProperty relayListProperty = _serializedObject.FindProperty("_relayDataList");

        if (relayListProperty == null ||
            relayIndex < 0 ||
            relayIndex >= relayListProperty.arraySize)
        {
            return null;
        }

        return relayListProperty.GetArrayElementAtIndex(relayIndex);
    }

    private int GetNextRelayId()
    {
        int maxId = -1;

        for (int i = 0; i < _levelSO.RelayDataList.Count; i++)
        {
            LevelRelayData relayData = _levelSO.RelayDataList[i];

            if (relayData != null && relayData.Id > maxId)
            {
                maxId = relayData.Id;
            }
        }

        return maxId + 1;
    }

    private bool IsDuplicateRelay(ERelayType relayType,
                                  int firstResourceId,
                                  int secondResourceId,
                                  int senderResourceId,
                                  int skipRelayIndex)
    {
        if (_levelSO == null)
        {
            return false;
        }

        for (int i = 0; i < _levelSO.RelayDataList.Count; i++)
        {
            if (i == skipRelayIndex)
            {
                continue;
            }

            LevelRelayData relayData = _levelSO.RelayDataList[i];

            if (relayData == null ||
                relayData.RelayType != relayType ||
                !IsSameResourcePair(relayData.FirstResourceId, relayData.SecondResourceId, firstResourceId, secondResourceId))
            {
                continue;
            }

            if (relayType == ERelayType.Link || relayData.SenderResourceId == senderResourceId)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsSameResourcePair(int firstA, int secondA, int firstB, int secondB)
    {
        return (firstA == firstB && secondA == secondB) ||
               (firstA == secondB && secondA == firstB);
    }

    private string GetProcessDisplayName(int processId)
    {
        return $"Process {processId}";
    }

    private string GetResourceDisplayName(int resourceId)
    {
        return $"Resource {resourceId}";
    }

    private bool IsCapacityOneResource(int resourceId)
    {
        LevelResourceData resourceData = FindResourceDataById(resourceId);
        return resourceData != null && resourceData.Capacity == 1;
    }

    private LevelResourceData FindResourceDataById(int resourceId)
    {
        if (_levelSO == null)
        {
            return null;
        }

        for (int i = 0; i < _levelSO.ResourceDataList.Count; i++)
        {
            LevelResourceData resourceData = _levelSO.ResourceDataList[i];

            if (resourceData != null && resourceData.Id == resourceId)
            {
                return resourceData;
            }
        }

        return null;
    }

    private int GetSelectedResourceId()
    {
        LevelResourceData resourceData = FindResourceData(_selectedRow, _selectedColumn);
        return resourceData == null ? -1 : resourceData.Id;
    }

    private void AddIntegerField(VisualElement parent, string label, string propertyPath)
    {
        SerializedProperty property = _serializedObject.FindProperty(propertyPath);

        if (property == null)
        {
            return;
        }

        IntegerField field = new IntegerField(label);
        field.SetValueWithoutNotify(property.intValue);
        field.RegisterValueChangedCallback(changeEvent => SetIntProperty(propertyPath, changeEvent.newValue));
        field.style.marginBottom = 4f;
        parent.Add(field);
    }

    private void AddStringField(VisualElement parent, string label, string propertyPath)
    {
        SerializedProperty property = _serializedObject.FindProperty(propertyPath);

        if (property == null)
        {
            return;
        }

        TextField field = new TextField(label);
        field.SetValueWithoutNotify(property.stringValue);
        field.RegisterValueChangedCallback(changeEvent => SetStringProperty(propertyPath, changeEvent.newValue));
        field.style.marginBottom = 4f;
        parent.Add(field);
    }

    private void AddReadOnlyIntegerField(VisualElement parent, string label, string propertyPath)
    {
        SerializedProperty property = _serializedObject.FindProperty(propertyPath);

        if (property == null)
        {
            return;
        }

        IntegerField field = new IntegerField(label);
        field.SetValueWithoutNotify(property.intValue);
        field.SetEnabled(false);
        field.style.marginBottom = 4f;
        parent.Add(field);
    }

    private void AddNodePositionField(VisualElement parent, string label, string nodePropertyPath, string positionPropertyName)
    {
        SerializedProperty nodeProperty = _serializedObject.FindProperty(nodePropertyPath);

        if (nodeProperty == null)
        {
            return;
        }

        SerializedProperty positionProperty = nodeProperty.FindPropertyRelative(positionPropertyName);

        if (positionProperty == null)
        {
            return;
        }

        IntegerField field = new IntegerField(label);
        field.SetValueWithoutNotify(positionProperty.intValue);
        field.RegisterValueChangedCallback(changeEvent => SetNodePositionProperty(nodePropertyPath, positionPropertyName, changeEvent.newValue));
        field.style.marginBottom = 4f;
        parent.Add(field);
    }

    private void AddColorIntegerField(VisualElement parent, string label, string propertyPath)
    {
        SerializedProperty property = _serializedObject.FindProperty(propertyPath);

        if (property == null)
        {
            return;
        }

        VisualElement row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.alignItems = Align.Center;
        parent.Add(row);

        row.Add(CreateColorSwatch(property.intValue, 18f));

        IntegerField field = new IntegerField(label);
        field.SetValueWithoutNotify(property.intValue);
        field.RegisterValueChangedCallback(changeEvent => SetIntProperty(propertyPath, changeEvent.newValue));
        field.style.flexGrow = 1f;
        row.Add(field);

        row.Add(CreateHeaderButton("선택 색 적용", () => SetIntProperty(propertyPath, _selectedColorId)));
    }

    private void AddEnumField<TEnum>(VisualElement parent, string label, string propertyPath)
        where TEnum : Enum
    {
        SerializedProperty property = _serializedObject.FindProperty(propertyPath);

        if (property == null)
        {
            return;
        }

        TEnum value = (TEnum)Enum.ToObject(typeof(TEnum), property.enumValueIndex);
        EnumField field = new EnumField(label, value);
        field.RegisterValueChangedCallback(changeEvent => SetEnumProperty<TEnum>(propertyPath, (TEnum)changeEvent.newValue));
        field.style.marginBottom = 4f;
        parent.Add(field);
    }

    private VisualElement CreateColorSwatch(int colorId, float size)
    {
        VisualElement swatch = new VisualElement();
        swatch.style.width = size;
        swatch.style.height = size;
        swatch.style.marginRight = 7f;
        swatch.style.borderTopLeftRadius = 4f;
        swatch.style.borderTopRightRadius = 4f;
        swatch.style.borderBottomLeftRadius = 4f;
        swatch.style.borderBottomRightRadius = 4f;
        SetBorder(swatch, 1f, Color.black);
        swatch.style.backgroundColor = new StyleColor(_colorMap.GetColor(colorId));
        return swatch;
    }

    private VisualElement CreateBox()
    {
        VisualElement box = new VisualElement();
        box.style.marginBottom = 8f;
        box.style.paddingLeft = 8f;
        box.style.paddingRight = 8f;
        box.style.paddingTop = 8f;
        box.style.paddingBottom = 8f;
        box.style.backgroundColor = new StyleColor(new Color(0.13f, 0.14f, 0.16f));
        box.style.borderTopLeftRadius = 8f;
        box.style.borderTopRightRadius = 8f;
        box.style.borderBottomLeftRadius = 8f;
        box.style.borderBottomRightRadius = 8f;
        SetBorder(box, 1f, new Color(0.25f, 0.27f, 0.30f));
        return box;
    }

    private VisualElement CreateNestedBox()
    {
        VisualElement box = new VisualElement();
        box.style.marginTop = 6f;
        box.style.marginBottom = 8f;
        box.style.paddingLeft = 8f;
        box.style.paddingRight = 8f;
        box.style.paddingTop = 8f;
        box.style.paddingBottom = 8f;
        box.style.backgroundColor = new StyleColor(new Color(0.10f, 0.11f, 0.13f));
        SetBorder(box, 1f, new Color(0.22f, 0.24f, 0.27f));
        return box;
    }

    private VisualElement CreateButtonRow()
    {
        VisualElement row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.flexWrap = Wrap.Wrap;
        row.style.marginTop = 6f;
        return row;
    }

    private Label CreateSectionTitle(string text)
    {
        Label label = new Label(text);
        label.style.unityFontStyleAndWeight = FontStyle.Bold;
        label.style.marginTop = 4f;
        label.style.marginBottom = 6f;
        label.style.color = new StyleColor(Color.white);
        return label;
    }

    private void SetBorder(VisualElement element, float width, Color color)
    {
        element.style.borderTopWidth = width;
        element.style.borderBottomWidth = width;
        element.style.borderLeftWidth = width;
        element.style.borderRightWidth = width;
        element.style.borderTopColor = new StyleColor(color);
        element.style.borderBottomColor = new StyleColor(color);
        element.style.borderLeftColor = new StyleColor(color);
        element.style.borderRightColor = new StyleColor(color);
    }

    private void AddProcess(int row, int column)
    {
        if (!CanAddNode(row, column))
        {
            SelectCell(row, column);
            return;
        }

        BeginEdit("Add Process");
        SerializedProperty processListProperty = _serializedObject.FindProperty("_processDataList");
        int arrayIndex = processListProperty.arraySize;
        processListProperty.InsertArrayElementAtIndex(arrayIndex);

        SerializedProperty processProperty = processListProperty.GetArrayElementAtIndex(arrayIndex);
        processProperty.FindPropertyRelative("_id").intValue = GetDefaultNodeId(row, column);
        processProperty.FindPropertyRelative("_row").intValue = row;
        processProperty.FindPropertyRelative("_column").intValue = column;

        SerializedProperty slotListProperty = processProperty.FindPropertyRelative("_slotDataList");
        slotListProperty.ClearArray();
        slotListProperty.InsertArrayElementAtIndex(0);
        SerializedProperty slotProperty = slotListProperty.GetArrayElementAtIndex(0);
        slotProperty.FindPropertyRelative("_id").intValue = 0;
        slotProperty.FindPropertyRelative("_requiredColorId").intValue = _selectedColorId;
        slotProperty.FindPropertyRelative("_selectionOrder").intValue = 0;

        EndEdit();
        SelectCell(row, column);
    }

    internal void AddNodeFromGraph(ELevelEditorNodeKind nodeKind, int row, int column)
    {
        if (nodeKind == ELevelEditorNodeKind.Process)
        {
            AddProcess(row, column);
            return;
        }

        AddResource(row, column);
    }

    internal void SelectNodeFromGraph(ELevelEditorNodeKind nodeKind, int dataIndex)
    {
        if (_levelSO == null)
        {
            return;
        }

        _selectedRelayIndex = -1;

        if (nodeKind == ELevelEditorNodeKind.Process)
        {
            if (dataIndex < 0 || dataIndex >= _levelSO.ProcessDataList.Count)
            {
                return;
            }

            LevelProcessData processData = _levelSO.ProcessDataList[dataIndex];

            if (processData != null)
            {
                SelectCellFromGraph(processData.Row, processData.Column);
            }

            return;
        }

        if (dataIndex < 0 || dataIndex >= _levelSO.ResourceDataList.Count)
        {
            return;
        }

        LevelResourceData resourceData = _levelSO.ResourceDataList[dataIndex];

        if (resourceData != null)
        {
            SelectCellFromGraph(resourceData.Row, resourceData.Column);
        }
    }

    internal void SelectProcessSlotForTestCaseFromGraph(int processIndex, int slotId)
    {
        if (_levelSO == null)
        {
            return;
        }

        if (processIndex < 0 || processIndex >= _levelSO.ProcessDataList.Count)
        {
            return;
        }

        LevelProcessData processData = _levelSO.ProcessDataList[processIndex];

        if (processData == null)
        {
            return;
        }

        _selectedRow = processData.Row;
        _selectedColumn = processData.Column;
        _selectedRelayIndex = -1;

        if (!IsTestCaseEditActive)
        {
            _testCaseEditMessage = "테스트 케이스 편집을 먼저 시작해 주세요.";
            RefreshAll();
            return;
        }

        if (!TryGetSlotData(processData, slotId, out LevelProcessSlotData slotData))
        {
            _testCaseEditMessage = "선택한 슬롯 데이터를 찾을 수 없습니다.";
            RefreshAll();
            return;
        }

        _selectedTestProcessId = processData.Id;
        _selectedTestSlotId = slotData.Id;
        _testCaseEditMessage = $"{GetProcessDisplayName(processData.Id)} 슬롯 {slotData.Id} 선택됨. 연결할 Resource를 클릭하세요.";
        RefreshAll();
    }

    internal void SelectResourceForTestAssignmentFromGraph(int resourceIndex)
    {
        if (_levelSO == null)
        {
            return;
        }

        if (resourceIndex < 0 || resourceIndex >= _levelSO.ResourceDataList.Count)
        {
            return;
        }

        LevelResourceData resourceData = _levelSO.ResourceDataList[resourceIndex];

        if (resourceData == null)
        {
            return;
        }

        _selectedRow = resourceData.Row;
        _selectedColumn = resourceData.Column;
        _selectedRelayIndex = -1;

        if (!IsTestCaseEditActive)
        {
            RefreshAll();
            return;
        }

        if (_selectedTestProcessId < 0 || _selectedTestSlotId < 0)
        {
            _testCaseEditMessage = "먼저 보드의 Process 색 슬롯을 클릭한 뒤 Resource를 선택하세요.";
            RefreshAll();
            return;
        }

        _testCaseEditMessage = $"{CreateConnectionLabel(_selectedTestProcessId, _selectedTestSlotId, resourceData.Id)} 연결됨.";
        UpsertTestAssignment(_activeTestCaseIndex, _selectedTestProcessId, _selectedTestSlotId, resourceData.Id);
    }

    internal void SelectNodeForRelayFromGraph(ELevelEditorNodeKind nodeKind, int dataIndex)
    {
        if (_levelSO == null)
        {
            return;
        }

        if (nodeKind != ELevelEditorNodeKind.Resource)
        {
            _relayDraftMessage = "Relay는 Resource끼리만 연결할 수 있습니다.";
            RefreshInspector();
            return;
        }

        if (dataIndex < 0 || dataIndex >= _levelSO.ResourceDataList.Count)
        {
            return;
        }

        LevelResourceData resourceData = _levelSO.ResourceDataList[dataIndex];

        if (resourceData == null)
        {
            return;
        }

        SelectResourceForRelay(resourceData);
    }

    internal void RejectRelaySelectionFromGraph(string message)
    {
        _relayDraftMessage = message;
        RefreshInspector();
    }

    internal void SelectGridPointFromGraph(int row, int column)
    {
        _selectedRelayIndex = -1;
        SelectCellFromGraph(row, column);
    }

    private void SelectResourceForRelay(LevelResourceData resourceData)
    {
        _selectedRow = resourceData.Row;
        _selectedColumn = resourceData.Column;
        _selectedRelayIndex = -1;

        if (_relayDraftFirstResourceId < 0)
        {
            _relayDraftFirstResourceId = resourceData.Id;
            _relayDraftSecondResourceId = -1;
            _relayDraftSenderResourceId = -1;
            _relayDraftMessage = "두 번째 Resource를 선택해 주세요.";
            RefreshAll();
            return;
        }

        if (_relayDraftFirstResourceId == resourceData.Id)
        {
            _relayDraftMessage = "같은 Resource는 Relay로 연결할 수 없습니다. 다른 Resource를 선택해 주세요.";
            RefreshAll();
            return;
        }

        _relayDraftSecondResourceId = resourceData.Id;
        EnsureRelayDraftSender();
        _relayDraftMessage = "Relay 설정을 선택하고 생성하세요.";
        RefreshAll();
    }

    private void ClearRelayDraft()
    {
        _relayDraftFirstResourceId = -1;
        _relayDraftSecondResourceId = -1;
        _relayDraftSenderResourceId = -1;
        _relayDraftType = ERelayType.Link;
        _relayDraftMessage = string.Empty;
    }

    private void EnsureRelayDraftSender()
    {
        if (_relayDraftType == ERelayType.Link)
        {
            _relayDraftSenderResourceId = 0;
            return;
        }

        if ((_relayDraftSenderResourceId == _relayDraftFirstResourceId ||
             _relayDraftSenderResourceId == _relayDraftSecondResourceId) &&
            IsCapacityOneResource(_relayDraftSenderResourceId))
        {
            return;
        }

        if (IsCapacityOneResource(_relayDraftFirstResourceId))
        {
            _relayDraftSenderResourceId = _relayDraftFirstResourceId;
            return;
        }

        if (IsCapacityOneResource(_relayDraftSecondResourceId))
        {
            _relayDraftSenderResourceId = _relayDraftSecondResourceId;
            return;
        }

        _relayDraftSenderResourceId = _relayDraftFirstResourceId;
    }

    private void SelectCellFromGraph(int row, int column)
    {
        _selectedRow = row;
        _selectedColumn = column;
        _selectedRelayIndex = -1;
        RefreshInspector();
    }

    internal bool TryMoveNodeFromGraph(ELevelEditorNodeKind nodeKind, int dataIndex, int row, int column)
    {
        if (_levelSO == null ||
            _serializedObject == null ||
            !IsInsideBoard(row, column) ||
            IsOccupiedByOtherNode(nodeKind, dataIndex, row, column))
        {
            return false;
        }

        string listPath = GetNodeListPath(nodeKind);
        BeginEdit("Move Level Node");
        SerializedProperty listProperty = _serializedObject.FindProperty(listPath);

        if (listProperty == null || dataIndex < 0 || dataIndex >= listProperty.arraySize)
        {
            return false;
        }

        SerializedProperty nodeProperty = listProperty.GetArrayElementAtIndex(dataIndex);
        int oldProcessId = nodeKind == ELevelEditorNodeKind.Process ?
            nodeProperty.FindPropertyRelative("_id").intValue :
            -1;
        int oldResourceId = nodeKind == ELevelEditorNodeKind.Resource ?
            nodeProperty.FindPropertyRelative("_id").intValue :
            -1;

        WriteNodePosition(nodeProperty, row, column);

        if (nodeKind == ELevelEditorNodeKind.Process)
        {
            int newProcessId = nodeProperty.FindPropertyRelative("_id").intValue;
            UpdateTestCaseProcessReferences(oldProcessId, newProcessId);
        }

        if (nodeKind == ELevelEditorNodeKind.Resource)
        {
            int newResourceId = nodeProperty.FindPropertyRelative("_id").intValue;
            UpdateRelayResourceReferences(oldResourceId, newResourceId);
            UpdateTestCaseResourceReferences(oldResourceId, newResourceId);
        }

        ApplyGraphEdit(row, column);
        return true;
    }

    internal void DeleteNodesFromGraph(IReadOnlyList<LevelBoardNodeView> nodeViewList)
    {
        if (_levelSO == null || _serializedObject == null)
        {
            return;
        }

        List<int> processIndexList = new List<int>();
        List<int> resourceIndexList = new List<int>();

        for (int i = 0; i < nodeViewList.Count; i++)
        {
            LevelBoardNodeView nodeView = nodeViewList[i];

            if (nodeView.NodeKind == ELevelEditorNodeKind.Process)
            {
                processIndexList.Add(nodeView.DataIndex);
            }
            else
            {
                resourceIndexList.Add(nodeView.DataIndex);
            }
        }

        processIndexList.Sort();
        resourceIndexList.Sort();
        BeginEdit("Delete Level Node");
        DeleteNodeIndices("_processDataList", processIndexList);
        DeleteNodeIndices("_resourceDataList", resourceIndexList);
        _selectedRow = -1;
        _selectedColumn = -1;
        EndEdit();
    }

    internal void DuplicateNodeFromGraph(ELevelEditorNodeKind nodeKind, int dataIndex)
    {
        if (_levelSO == null || _serializedObject == null)
        {
            return;
        }

        string listPath = GetNodeListPath(nodeKind);
        SerializedProperty currentListProperty = _serializedObject.FindProperty(listPath);

        if (currentListProperty == null ||
            dataIndex < 0 ||
            dataIndex >= currentListProperty.arraySize)
        {
            return;
        }

        SerializedProperty currentSourceProperty = currentListProperty.GetArrayElementAtIndex(dataIndex);
        int sourceRow = currentSourceProperty.FindPropertyRelative("_row").intValue;
        int sourceColumn = currentSourceProperty.FindPropertyRelative("_column").intValue;

        if (!TryFindAvailablePoint(sourceRow + 1, sourceColumn + 1, out int row, out int column))
        {
            return;
        }

        BeginEdit("Duplicate Level Node");
        SerializedProperty listProperty = _serializedObject.FindProperty(listPath);
        SerializedProperty sourceProperty = listProperty.GetArrayElementAtIndex(dataIndex);
        int arrayIndex = listProperty.arraySize;
        listProperty.InsertArrayElementAtIndex(arrayIndex);
        SerializedProperty targetProperty = listProperty.GetArrayElementAtIndex(arrayIndex);

        if (nodeKind == ELevelEditorNodeKind.Process)
        {
            CopyProcessProperty(sourceProperty, targetProperty, row, column);
        }
        else
        {
            CopyResourceProperty(sourceProperty, targetProperty, row, column);
        }

        EndEdit();
        SelectCell(row, column);
    }

    internal int ClampRow(int row)
    {
        if (_levelSO == null)
        {
            return 0;
        }

        return Mathf.Clamp(row, 0, Mathf.Max(0, _levelSO.RowCount - 1));
    }

    internal int ClampColumn(int column)
    {
        if (_levelSO == null)
        {
            return 0;
        }

        return Mathf.Clamp(column, 0, Mathf.Max(0, _levelSO.ColumnCount - 1));
    }

    private void AddResource(int row, int column)
    {
        if (!CanAddNode(row, column))
        {
            SelectCell(row, column);
            return;
        }

        BeginEdit("Add Resource");
        SerializedProperty resourceListProperty = _serializedObject.FindProperty("_resourceDataList");
        int arrayIndex = resourceListProperty.arraySize;
        resourceListProperty.InsertArrayElementAtIndex(arrayIndex);

        SerializedProperty resourceProperty = resourceListProperty.GetArrayElementAtIndex(arrayIndex);
        resourceProperty.FindPropertyRelative("_id").intValue = GetDefaultNodeId(row, column);
        resourceProperty.FindPropertyRelative("_row").intValue = row;
        resourceProperty.FindPropertyRelative("_column").intValue = column;
        resourceProperty.FindPropertyRelative("_initialColorId").intValue = _selectedColorId;
        resourceProperty.FindPropertyRelative("_capacity").intValue = 1;

        SerializedProperty ruleListProperty = resourceProperty.FindPropertyRelative("_ruleDataList");
        ruleListProperty.ClearArray();
        ruleListProperty.InsertArrayElementAtIndex(0);
        WriteDefaultRule(ruleListProperty.GetArrayElementAtIndex(0));

        EndEdit();
        SelectCell(row, column);
    }

    private bool CanAddNode(int row, int column)
    {
        return IsInsideBoard(row, column) &&
               FindProcessData(row, column) == null &&
               FindResourceData(row, column) == null;
    }

    private bool IsInsideBoard(int row, int column)
    {
        return _levelSO != null &&
               row >= 0 &&
               column >= 0 &&
               row < _levelSO.RowCount &&
               column < _levelSO.ColumnCount;
    }

    private bool IsOccupiedByOtherNode(ELevelEditorNodeKind nodeKind, int dataIndex, int row, int column)
    {
        for (int i = 0; i < _levelSO.ProcessDataList.Count; i++)
        {
            LevelProcessData processData = _levelSO.ProcessDataList[i];

            if (nodeKind == ELevelEditorNodeKind.Process && i == dataIndex)
            {
                continue;
            }

            if (processData != null && processData.Row == row && processData.Column == column)
            {
                return true;
            }
        }

        for (int i = 0; i < _levelSO.ResourceDataList.Count; i++)
        {
            LevelResourceData resourceData = _levelSO.ResourceDataList[i];

            if (nodeKind == ELevelEditorNodeKind.Resource && i == dataIndex)
            {
                continue;
            }

            if (resourceData != null && resourceData.Row == row && resourceData.Column == column)
            {
                return true;
            }
        }

        return false;
    }

    private string GetNodeListPath(ELevelEditorNodeKind nodeKind)
    {
        return nodeKind == ELevelEditorNodeKind.Process ? "_processDataList" : "_resourceDataList";
    }

    private void WriteNodePosition(SerializedProperty nodeProperty, int row, int column)
    {
        nodeProperty.FindPropertyRelative("_id").intValue = GetDefaultNodeId(row, column);
        nodeProperty.FindPropertyRelative("_row").intValue = row;
        nodeProperty.FindPropertyRelative("_column").intValue = column;
    }

    private void UpdateRelayResourceReferences(int oldResourceId, int newResourceId)
    {
        if (oldResourceId == newResourceId)
        {
            return;
        }

        SerializedProperty relayListProperty = _serializedObject.FindProperty("_relayDataList");

        if (relayListProperty != null)
        {
            for (int i = 0; i < relayListProperty.arraySize; i++)
            {
                SerializedProperty relayProperty = relayListProperty.GetArrayElementAtIndex(i);
                ReplaceIntId(relayProperty.FindPropertyRelative("_firstResourceId"), oldResourceId, newResourceId);
                ReplaceIntId(relayProperty.FindPropertyRelative("_secondResourceId"), oldResourceId, newResourceId);
                ReplaceIntId(relayProperty.FindPropertyRelative("_senderResourceId"), oldResourceId, newResourceId);
            }
        }

        if (_relayDraftFirstResourceId == oldResourceId)
        {
            _relayDraftFirstResourceId = newResourceId;
        }

        if (_relayDraftSecondResourceId == oldResourceId)
        {
            _relayDraftSecondResourceId = newResourceId;
        }

        if (_relayDraftSenderResourceId == oldResourceId)
        {
            _relayDraftSenderResourceId = newResourceId;
        }
    }

    private void UpdateTestCaseProcessReferences(int oldProcessId, int newProcessId)
    {
        if (oldProcessId == newProcessId)
        {
            return;
        }

        UpdateAssignedConnectionReferences("_processId", oldProcessId, newProcessId);
    }

    private void UpdateTestCaseResourceReferences(int oldResourceId, int newResourceId)
    {
        if (oldResourceId == newResourceId)
        {
            return;
        }

        UpdateAssignedConnectionReferences("_resourceId", oldResourceId, newResourceId);
    }

    private void UpdateAssignedConnectionReferences(string propertyName, int oldId, int newId)
    {
        SerializedProperty testCaseListProperty = _serializedObject.FindProperty("_testCaseDataList");

        if (testCaseListProperty == null)
        {
            return;
        }

        for (int i = 0; i < testCaseListProperty.arraySize; i++)
        {
            SerializedProperty testCaseProperty = testCaseListProperty.GetArrayElementAtIndex(i);
            SerializedProperty assignedConnectionListProperty = testCaseProperty.FindPropertyRelative("_assignedConnectionDataList");

            for (int j = 0; j < assignedConnectionListProperty.arraySize; j++)
            {
                SerializedProperty assignedConnectionProperty = assignedConnectionListProperty.GetArrayElementAtIndex(j);
                ReplaceIntId(assignedConnectionProperty.FindPropertyRelative(propertyName), oldId, newId);
            }
        }
    }

    private void ReplaceIntId(SerializedProperty property, int oldId, int newId)
    {
        if (property != null && property.intValue == oldId)
        {
            property.intValue = newId;
        }
    }

    private void ApplyGraphEdit(int row, int column)
    {
        _serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(_levelSO);
        _selectedRow = row;
        _selectedColumn = column;
        _finalValidationText = string.Empty;
        RefreshInspector();
        RefreshValidationLog();
    }

    private void DeleteNodeIndices(string listPath, List<int> indexList)
    {
        SerializedProperty listProperty = _serializedObject.FindProperty(listPath);

        for (int i = indexList.Count - 1; i >= 0; i--)
        {
            int index = indexList[i];

            if (listProperty != null && index >= 0 && index < listProperty.arraySize)
            {
                listProperty.DeleteArrayElementAtIndex(index);
            }
        }
    }

    private bool TryFindAvailablePoint(int preferredRow, int preferredColumn, out int row, out int column)
    {
        row = ClampRow(preferredRow);
        column = ClampColumn(preferredColumn);

        if (CanAddNode(row, column))
        {
            return true;
        }

        for (int r = 0; r < _levelSO.RowCount; r++)
        {
            for (int c = 0; c < _levelSO.ColumnCount; c++)
            {
                if (CanAddNode(r, c))
                {
                    row = r;
                    column = c;
                    return true;
                }
            }
        }

        row = -1;
        column = -1;
        return false;
    }

    private void CopyProcessProperty(SerializedProperty sourceProperty,
                                     SerializedProperty targetProperty,
                                     int row,
                                     int column)
    {
        WriteNodePosition(targetProperty, row, column);
        SerializedProperty sourceSlotListProperty = sourceProperty.FindPropertyRelative("_slotDataList");
        SerializedProperty targetSlotListProperty = targetProperty.FindPropertyRelative("_slotDataList");
        targetSlotListProperty.ClearArray();

        for (int i = 0; i < sourceSlotListProperty.arraySize; i++)
        {
            targetSlotListProperty.InsertArrayElementAtIndex(i);
            SerializedProperty sourceSlotProperty = sourceSlotListProperty.GetArrayElementAtIndex(i);
            SerializedProperty targetSlotProperty = targetSlotListProperty.GetArrayElementAtIndex(i);
            targetSlotProperty.FindPropertyRelative("_id").intValue = sourceSlotProperty.FindPropertyRelative("_id").intValue;
            targetSlotProperty.FindPropertyRelative("_requiredColorId").intValue = sourceSlotProperty.FindPropertyRelative("_requiredColorId").intValue;
            targetSlotProperty.FindPropertyRelative("_selectionOrder").intValue = sourceSlotProperty.FindPropertyRelative("_selectionOrder").intValue;
        }
    }

    private void CopyResourceProperty(SerializedProperty sourceProperty,
                                      SerializedProperty targetProperty,
                                      int row,
                                      int column)
    {
        WriteNodePosition(targetProperty, row, column);
        targetProperty.FindPropertyRelative("_initialColorId").intValue = sourceProperty.FindPropertyRelative("_initialColorId").intValue;
        targetProperty.FindPropertyRelative("_capacity").intValue = sourceProperty.FindPropertyRelative("_capacity").intValue;

        SerializedProperty sourceRuleListProperty = sourceProperty.FindPropertyRelative("_ruleDataList");
        SerializedProperty targetRuleListProperty = targetProperty.FindPropertyRelative("_ruleDataList");
        targetRuleListProperty.ClearArray();

        for (int i = 0; i < sourceRuleListProperty.arraySize; i++)
        {
            targetRuleListProperty.InsertArrayElementAtIndex(i);
            SerializedProperty sourceRuleProperty = sourceRuleListProperty.GetArrayElementAtIndex(i);
            SerializedProperty targetRuleProperty = targetRuleListProperty.GetArrayElementAtIndex(i);
            targetRuleProperty.FindPropertyRelative("_ruleType").enumValueIndex = sourceRuleProperty.FindPropertyRelative("_ruleType").enumValueIndex;
            targetRuleProperty.FindPropertyRelative("_clockMode").enumValueIndex = sourceRuleProperty.FindPropertyRelative("_clockMode").enumValueIndex;
            targetRuleProperty.FindPropertyRelative("_clockRoundCount").intValue = sourceRuleProperty.FindPropertyRelative("_clockRoundCount").intValue;
            CopyIntListProperty(sourceRuleProperty.FindPropertyRelative("_colorIdList"),
                                targetRuleProperty.FindPropertyRelative("_colorIdList"));
        }
    }

    private void CopyIntListProperty(SerializedProperty sourceListProperty, SerializedProperty targetListProperty)
    {
        targetListProperty.ClearArray();

        for (int i = 0; i < sourceListProperty.arraySize; i++)
        {
            targetListProperty.InsertArrayElementAtIndex(i);
            targetListProperty.GetArrayElementAtIndex(i).intValue = sourceListProperty.GetArrayElementAtIndex(i).intValue;
        }
    }

    private void EraseCell(int row, int column)
    {
        BeginEdit("Erase Cell");
        DeleteNodeAtPosition("_processDataList", row, column);
        DeleteNodeAtPosition("_resourceDataList", row, column);
        EndEdit();
        SelectCell(row, column);
    }

    private void DeleteNodeAtPosition(string listPath, int row, int column)
    {
        SerializedProperty listProperty = _serializedObject.FindProperty(listPath);

        for (int i = listProperty.arraySize - 1; i >= 0; i--)
        {
            SerializedProperty elementProperty = listProperty.GetArrayElementAtIndex(i);

            if (elementProperty.FindPropertyRelative("_row").intValue == row &&
                elementProperty.FindPropertyRelative("_column").intValue == column)
            {
                listProperty.DeleteArrayElementAtIndex(i);
            }
        }
    }

    private void AddSlot(string slotListPath)
    {
        BeginEdit("Add Slot");
        SerializedProperty slotListProperty = _serializedObject.FindProperty(slotListPath);
        int arrayIndex = slotListProperty.arraySize;
        slotListProperty.InsertArrayElementAtIndex(arrayIndex);
        SerializedProperty slotProperty = slotListProperty.GetArrayElementAtIndex(arrayIndex);
        slotProperty.FindPropertyRelative("_id").intValue = arrayIndex;
        slotProperty.FindPropertyRelative("_requiredColorId").intValue = _selectedColorId;
        slotProperty.FindPropertyRelative("_selectionOrder").intValue = arrayIndex;
        EndEdit();
    }

    private void AddRule(string ruleListPath)
    {
        BeginEdit("Add Rule");
        SerializedProperty ruleListProperty = _serializedObject.FindProperty(ruleListPath);
        int arrayIndex = ruleListProperty.arraySize;
        ruleListProperty.InsertArrayElementAtIndex(arrayIndex);
        WriteDefaultRule(ruleListProperty.GetArrayElementAtIndex(arrayIndex));
        EndEdit();
    }

    private void AddColorId(string colorIdListPath)
    {
        BeginEdit("Add ColorId");
        SerializedProperty colorIdListProperty = _serializedObject.FindProperty(colorIdListPath);
        int arrayIndex = colorIdListProperty.arraySize;
        colorIdListProperty.InsertArrayElementAtIndex(arrayIndex);
        colorIdListProperty.GetArrayElementAtIndex(arrayIndex).intValue = _selectedColorId;
        EndEdit();
    }

    private void AddTestCase()
    {
        BeginEdit("Add Test Case");
        SerializedProperty testCaseListProperty = _serializedObject.FindProperty("_testCaseDataList");
        int arrayIndex = testCaseListProperty.arraySize;
        testCaseListProperty.InsertArrayElementAtIndex(arrayIndex);
        WriteDefaultTestCase(testCaseListProperty.GetArrayElementAtIndex(arrayIndex), arrayIndex);
        EndEdit();
    }

    private void ActivateTestCaseEdit(int testCaseIndex)
    {
        _activeTestCaseIndex = testCaseIndex;
        _selectedTestProcessId = -1;
        _selectedTestSlotId = -1;
        _testCaseEditMessage = "Process 색 슬롯을 클릭한 뒤 연결할 Resource를 클릭하세요.";
        RefreshAll();
    }

    private void DeactivateTestCaseEdit()
    {
        _activeTestCaseIndex = -1;
        _selectedTestProcessId = -1;
        _selectedTestSlotId = -1;
        _testCaseEditMessage = string.Empty;
        RefreshAll();
    }

    private void ClearSelectedTestSlot()
    {
        _selectedTestProcessId = -1;
        _selectedTestSlotId = -1;
        _testCaseEditMessage = "Process 색 슬롯 선택이 해제되었습니다.";
        RefreshAll();
    }

    private void DeleteTestCase(string testCaseListPath, int testCaseIndex)
    {
        if (_activeTestCaseIndex == testCaseIndex)
        {
            _activeTestCaseIndex = -1;
            _selectedTestProcessId = -1;
            _selectedTestSlotId = -1;
            _testCaseEditMessage = string.Empty;
        }
        else if (_activeTestCaseIndex > testCaseIndex)
        {
            _activeTestCaseIndex--;
        }

        DeleteArrayElement(testCaseListPath, testCaseIndex);
    }

    private void UpsertTestAssignment(int testCaseIndex, int processId, int slotId, int resourceId)
    {
        if (_serializedObject == null)
        {
            return;
        }

        BeginEdit("Edit Test Assignment");
        SerializedProperty testCaseProperty = FindTestCaseProperty(testCaseIndex);

        if (testCaseProperty == null)
        {
            EndEdit();
            return;
        }

        SerializedProperty assignedConnectionListProperty = testCaseProperty.FindPropertyRelative("_assignedConnectionDataList");

        for (int i = 0; i < assignedConnectionListProperty.arraySize; i++)
        {
            SerializedProperty assignedConnectionProperty = assignedConnectionListProperty.GetArrayElementAtIndex(i);

            if (assignedConnectionProperty.FindPropertyRelative("_processId").intValue == processId &&
                assignedConnectionProperty.FindPropertyRelative("_slotId").intValue == slotId)
            {
                assignedConnectionProperty.FindPropertyRelative("_resourceId").intValue = resourceId;
                EndEdit();
                return;
            }
        }

        int arrayIndex = assignedConnectionListProperty.arraySize;
        assignedConnectionListProperty.InsertArrayElementAtIndex(arrayIndex);
        SerializedProperty newAssignedConnectionProperty = assignedConnectionListProperty.GetArrayElementAtIndex(arrayIndex);
        newAssignedConnectionProperty.FindPropertyRelative("_processId").intValue = processId;
        newAssignedConnectionProperty.FindPropertyRelative("_slotId").intValue = slotId;
        newAssignedConnectionProperty.FindPropertyRelative("_resourceId").intValue = resourceId;
        EndEdit();
    }

    private void WriteDefaultTestCase(SerializedProperty testCaseProperty, int index)
    {
        testCaseProperty.FindPropertyRelative("_name").stringValue = $"Test {index + 1}";
        testCaseProperty.FindPropertyRelative("_maxRoundCount").intValue = 20;
        testCaseProperty.FindPropertyRelative("_expectedEndState").enumValueIndex = (int)ESimulationEndState.Succeeded;
        testCaseProperty.FindPropertyRelative("_assignedConnectionDataList").ClearArray();
    }

    private void WriteDefaultStarThreshold(SerializedProperty starThresholdProperty)
    {
        if (starThresholdProperty == null)
        {
            return;
        }

        starThresholdProperty.FindPropertyRelative("_threeStarRoundCount").intValue = 0;
        starThresholdProperty.FindPropertyRelative("_twoStarRoundCount").intValue = 0;
        starThresholdProperty.FindPropertyRelative("_oneStarRoundCount").intValue = 0;
    }

    private void RunSolutionFinder()
    {
        if (_levelSO == null)
        {
            ClearSolutionFinderState();
            _solutionFinderText = "자동 해 찾기 실패: LevelSO가 선택되지 않았습니다.";
            RefreshAll();
            return;
        }

        LevelSolutionFinder finder = new LevelSolutionFinder();
        LevelSolveSettings settings = new LevelSolveSettings(GetSolutionFinderMaxRoundCount(),
                                                             LevelSolveSettings.DefaultMaxSearchNodeCount,
                                                             LevelSolveSettings.DefaultMaxEvaluatedCandidateCount,
                                                             true);

        _lastSolveReport = finder.FindBestSolution(_levelSO, settings);
        _lastDifficultyReport = _difficultyAnalyzer.Analyze(_levelSO, _lastSolveReport);
        _solutionFinderText = BuildSolveReportText(_lastSolveReport, _lastDifficultyReport);
        RefreshAll();
    }

    private int GetSolutionFinderMaxRoundCount()
    {
        if (TryGetStarThreshold(out int _, out int _, out int oneStarRoundCount))
        {
            return Math.Max(LevelSolveSettings.DefaultMaxRoundCount, oneStarRoundCount);
        }

        return LevelSolveSettings.DefaultMaxRoundCount;
    }

    private bool HasUsableSolveCandidate()
    {
        return _lastSolveReport?.BestCandidate != null &&
               _lastSolveReport.StarThresholdRecommendation != null;
    }

    private bool HasUsableSolveRecommendation()
    {
        return _lastSolveReport?.StarThresholdRecommendation != null;
    }

    private void SaveLastStarThresholdRecommendation()
    {
        if (!HasUsableSolveRecommendation())
        {
            return;
        }

        LevelSolveReport report = _lastSolveReport;
        LevelStarThresholdRecommendation recommendation = report.StarThresholdRecommendation;
        BeginEdit("Save Star Threshold");
        SerializedProperty starThresholdProperty = _serializedObject.FindProperty("_starThresholdData");

        if (starThresholdProperty != null)
        {
            starThresholdProperty.FindPropertyRelative("_threeStarRoundCount").intValue = recommendation.ThreeStarRoundCount;
            starThresholdProperty.FindPropertyRelative("_twoStarRoundCount").intValue = recommendation.TwoStarRoundCount;
            starThresholdProperty.FindPropertyRelative("_oneStarRoundCount").intValue = recommendation.OneStarRoundCount;
        }

        EndEdit(clearSolutionFinderState: false);
        _lastSolveReport = report;
        _solutionFinderText = BuildSolveReportText(report, _lastDifficultyReport) + "\n별 기준 저장 완료.";
        RefreshAll();
    }

    private void UpdateAutoOptimalTestCaseFromLastSolve()
    {
        if (!HasUsableSolveCandidate())
        {
            return;
        }

        LevelSolveReport report = _lastSolveReport;
        int testCaseIndex = FindAutoOptimalTestCaseIndex();

        BeginEdit("Update Auto Optimal Test Case");
        SerializedProperty testCaseListProperty = _serializedObject.FindProperty("_testCaseDataList");

        if (testCaseIndex < 0)
        {
            testCaseIndex = testCaseListProperty.arraySize;
            testCaseListProperty.InsertArrayElementAtIndex(testCaseIndex);
        }

        WriteAutoOptimalTestCase(testCaseListProperty.GetArrayElementAtIndex(testCaseIndex),
                                 report.BestCandidate,
                                 report.StarThresholdRecommendation);
        EndEdit(clearSolutionFinderState: false);

        _lastSolveReport = report;
        _solutionFinderText = BuildSolveReportText(report, _lastDifficultyReport) + "\nAuto Optimal 테스트 케이스 갱신 완료.";
        RunTestCase(testCaseIndex);
    }

    private int FindAutoOptimalTestCaseIndex()
    {
        SerializedProperty testCaseListProperty = _serializedObject.FindProperty("_testCaseDataList");

        if (testCaseListProperty == null)
        {
            return -1;
        }

        for (int i = 0; i < testCaseListProperty.arraySize; i++)
        {
            SerializedProperty testCaseProperty = testCaseListProperty.GetArrayElementAtIndex(i);
            string testCaseName = testCaseProperty.FindPropertyRelative("_name").stringValue;

            if (string.Equals(testCaseName, AutoOptimalTestCaseName, StringComparison.Ordinal))
            {
                return i;
            }
        }

        return -1;
    }

    private void WriteAutoOptimalTestCase(
        SerializedProperty testCaseProperty,
        LevelSolveCandidate candidate,
        LevelStarThresholdRecommendation recommendation)
    {
        testCaseProperty.FindPropertyRelative("_name").stringValue = AutoOptimalTestCaseName;
        testCaseProperty.FindPropertyRelative("_maxRoundCount").intValue = recommendation.OneStarRoundCount;
        testCaseProperty.FindPropertyRelative("_expectedEndState").enumValueIndex = (int)ESimulationEndState.Succeeded;

        SerializedProperty assignedConnectionListProperty = testCaseProperty.FindPropertyRelative("_assignedConnectionDataList");
        assignedConnectionListProperty.ClearArray();

        for (int i = 0; i < candidate.AssignmentArray.Length; i++)
        {
            LevelSolveAssignment assignment = candidate.AssignmentArray[i];
            assignedConnectionListProperty.InsertArrayElementAtIndex(i);
            SerializedProperty assignmentProperty = assignedConnectionListProperty.GetArrayElementAtIndex(i);
            assignmentProperty.FindPropertyRelative("_processId").intValue = assignment.ProcessId;
            assignmentProperty.FindPropertyRelative("_slotId").intValue = assignment.SlotId;
            assignmentProperty.FindPropertyRelative("_resourceId").intValue = assignment.ResourceId;
        }
    }

    private string BuildSolveReportSummary(LevelSolveReport report)
    {
        if (report.BestCandidate == null)
        {
            return $"{report.EndState}: {report.Message}";
        }

        LevelStarThresholdRecommendation recommendation = report.StarThresholdRecommendation;
        string provenText = report.IsOptimalProven ? "최적 증명됨" : "최적 미증명";
        return $"{provenText} / 최선 {report.BestCandidate.ClearRoundCount}R / " +
               $"3별 {recommendation.ThreeStarRoundCount}R, " +
               $"2별 {recommendation.TwoStarRoundCount}R, " +
               $"1별 {recommendation.OneStarRoundCount}R";
    }

    private string BuildDifficultySummary(LevelDifficultyReport report)
    {
        if (report == null)
        {
            return string.Empty;
        }

        if (!report.IsAvailable)
        {
            return $"난이도 분석 불가: {report.UnavailableReason}";
        }

        return report.Summary;
    }

    private string BuildSolveReportText(LevelSolveReport report, LevelDifficultyReport difficultyReport)
    {
        if (report == null)
        {
            return string.Empty;
        }

        List<string> lineList = new List<string>
        {
            $"자동 해 찾기: {report.EndState}",
            report.Message,
            $"탐색: 노드 {report.ExploredNodeCount:N0}, 후보 평가 {report.EvaluatedCandidateCount:N0}, 성공 해 {report.FoundSolutionCount:N0}",
            $"이론상 최소 라운드: {report.MinimumPossibleRoundCount}",
        };

        if (report.ValidationResult != null && !report.ValidationResult.IsValid)
        {
            for (int i = 0; i < report.ValidationResult.ErrorList.Length; i++)
            {
                lineList.Add("- " + report.ValidationResult.ErrorList[i].Message);
            }
        }

        if (report.BestCandidate == null)
        {
            AddDifficultyReportLines(lineList, difficultyReport);
            return string.Join("\n", lineList);
        }

        LevelSolveCandidate candidate = report.BestCandidate;
        LevelStarThresholdRecommendation recommendation = report.StarThresholdRecommendation;
        lineList.Add($"최선 해: {candidate.ClearRoundCount} 라운드 / waiting {candidate.WaitingCount}, 재투입 {candidate.RequeuedCount}, 차단 {candidate.BlockedCount}");
        lineList.Add($"별 기준 추천: 3별 {recommendation.ThreeStarRoundCount}R, 2별 {recommendation.TwoStarRoundCount}R, 1별 {recommendation.OneStarRoundCount}R");
        lineList.Add(recommendation.Reason);
        AddDifficultyReportLines(lineList, difficultyReport);
        lineList.Add("예약 연결:");

        for (int i = 0; i < candidate.AssignmentArray.Length; i++)
        {
            LevelSolveAssignment assignment = candidate.AssignmentArray[i];
            lineList.Add($"- {CreateConnectionLabel(assignment.ProcessId, assignment.SlotId, assignment.ResourceId)}");
        }

        return string.Join("\n", lineList);
    }

    private void AddDifficultyReportLines(List<string> lineList, LevelDifficultyReport report)
    {
        if (report == null)
        {
            return;
        }

        if (!report.IsAvailable)
        {
            lineList.Add($"난이도 분석 불가: {report.UnavailableReason}");
            return;
        }

        lineList.Add(report.Summary);

        for (int i = 0; i < report.ReasonList.Length; i++)
        {
            lineList.Add("- " + report.ReasonList[i]);
        }
    }

    private void WriteDefaultRule(SerializedProperty ruleProperty)
    {
        ruleProperty.FindPropertyRelative("_ruleType").enumValueIndex = (int)ELevelResourceRuleType.Basic;
        ruleProperty.FindPropertyRelative("_clockMode").enumValueIndex = (int)EClockMode.OnToOff;
        ruleProperty.FindPropertyRelative("_clockRoundCount").intValue = 1;
        ruleProperty.FindPropertyRelative("_colorIdList").ClearArray();
    }

    private void DeleteArrayElement(string listPath, int index)
    {
        BeginEdit("Delete Element");
        SerializedProperty listProperty = _serializedObject.FindProperty(listPath);

        if (listProperty != null && index >= 0 && index < listProperty.arraySize)
        {
            listProperty.DeleteArrayElementAtIndex(index);
        }

        EndEdit();
    }

    private void MoveArrayElement(string listPath, int oldIndex, int newIndex)
    {
        SerializedProperty listProperty = _serializedObject.FindProperty(listPath);

        if (listProperty == null ||
            newIndex < 0 ||
            oldIndex < 0 ||
            oldIndex >= listProperty.arraySize ||
            newIndex >= listProperty.arraySize)
        {
            return;
        }

        BeginEdit("Move Element");
        listProperty = _serializedObject.FindProperty(listPath);
        listProperty.MoveArrayElement(oldIndex, newIndex);
        EndEdit();
    }

    private void SetIntProperty(string propertyPath, int value)
    {
        BeginEdit("Edit Level");
        SerializedProperty property = _serializedObject.FindProperty(propertyPath);

        if (property != null)
        {
            property.intValue = value;
        }

        EndEdit();
    }

    private void SetStringProperty(string propertyPath, string value)
    {
        BeginEdit("Edit Level");
        SerializedProperty property = _serializedObject.FindProperty(propertyPath);

        if (property != null)
        {
            property.stringValue = value ?? string.Empty;
        }

        EndEdit();
    }

    private void SetNodePositionProperty(string nodePropertyPath, string positionPropertyName, int value)
    {
        BeginEdit("Edit Node Position");
        SerializedProperty nodeProperty = _serializedObject.FindProperty(nodePropertyPath);

        if (nodeProperty != null)
        {
            bool isProcessNode = nodePropertyPath.StartsWith("_processDataList", StringComparison.Ordinal);
            bool isResourceNode = nodePropertyPath.StartsWith("_resourceDataList", StringComparison.Ordinal);
            int oldProcessId = isProcessNode ? nodeProperty.FindPropertyRelative("_id").intValue : -1;
            int oldResourceId = isResourceNode ? nodeProperty.FindPropertyRelative("_id").intValue : -1;

            nodeProperty.FindPropertyRelative(positionPropertyName).intValue = value;

            int row = nodeProperty.FindPropertyRelative("_row").intValue;
            int column = nodeProperty.FindPropertyRelative("_column").intValue;
            nodeProperty.FindPropertyRelative("_id").intValue = GetDefaultNodeId(row, column);

            if (isProcessNode)
            {
                int newProcessId = nodeProperty.FindPropertyRelative("_id").intValue;
                UpdateTestCaseProcessReferences(oldProcessId, newProcessId);
            }

            if (isResourceNode)
            {
                int newResourceId = nodeProperty.FindPropertyRelative("_id").intValue;
                UpdateRelayResourceReferences(oldResourceId, newResourceId);
                UpdateTestCaseResourceReferences(oldResourceId, newResourceId);
            }
        }

        EndEdit();
    }

    private void SetEnumProperty<TEnum>(string propertyPath, TEnum value)
        where TEnum : Enum
    {
        BeginEdit("Edit Level");
        SerializedProperty property = _serializedObject.FindProperty(propertyPath);

        if (property != null)
        {
            property.enumValueIndex = Convert.ToInt32(value);
        }

        EndEdit();
    }

    private void BeginEdit(string undoName)
    {
        if (_levelSO == null || _serializedObject == null)
        {
            return;
        }

        Undo.RecordObject(_levelSO, undoName);
        _serializedObject.Update();
    }

    private void EndEdit(bool clearSolutionFinderState = true)
    {
        if (_levelSO == null || _serializedObject == null)
        {
            return;
        }

        _serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(_levelSO);
        _finalValidationText = string.Empty;
        ClearTestRunState();
        if (clearSolutionFinderState)
        {
            ClearSolutionFinderState();
        }

        RefreshAll();
    }

    private void ClearTestRunState()
    {
        _testRunText = string.Empty;
        _lastTestSimulationReport = null;
        _lastTestConnectionRunDataList.Clear();
        _lastRunTestCaseIndex = -1;
        _selectedTestRoundIndex = -1;
    }

    private void ClearSolutionFinderState()
    {
        _solutionFinderText = string.Empty;
        _lastSolveReport = null;
        _lastDifficultyReport = null;
    }

    private void CreateNewLevel()
    {
        LevelSO levelSO = ScriptableObject.CreateInstance<LevelSO>();
        string assetPath = AssetDatabase.GenerateUniqueAssetPath(NewLevelAssetPath);
        AssetDatabase.CreateAsset(levelSO, assetPath);

        SerializedObject serializedObject = new SerializedObject(levelSO);
        serializedObject.FindProperty("_id").intValue = 0;
        serializedObject.FindProperty("_rowCount").intValue = 3;
        serializedObject.FindProperty("_columnCount").intValue = 5;
        serializedObject.FindProperty("_processDataList").ClearArray();
        serializedObject.FindProperty("_resourceDataList").ClearArray();
        serializedObject.FindProperty("_relayDataList").ClearArray();
        WriteDefaultStarThreshold(serializedObject.FindProperty("_starThresholdData"));
        serializedObject.FindProperty("_testCaseDataList").ClearArray();
        serializedObject.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(levelSO);
        AssetDatabase.SaveAssets();
        SetLevel(levelSO);
    }

    private void ImportGeneratedCandidate()
    {
        string jsonFilePath = EditorUtility.OpenFilePanel("AI 후보 가져오기", LevelGenerationCandidateImportMenu.GetDefaultCandidateFolderPath(), "json");

        if (string.IsNullOrEmpty(jsonFilePath))
        {
            return;
        }

        LevelGenerationCandidateImportService service = new LevelGenerationCandidateImportService();
        LevelGenerationCandidateImportReport report = service.Import(jsonFilePath, LevelGenerationCandidateImportMenu.DefaultOutputAssetFolder);
        service.WriteReport(report, LevelGenerationCandidateImportMenu.DefaultReportFolder);
        Debug.Log(report.ToText());

        if (report.IsSaved)
        {
            LevelSO levelSO = AssetDatabase.LoadAssetAtPath<LevelSO>(report.CreatedAssetPath);

            if (levelSO != null)
            {
                SetLevel(levelSO);
                Selection.activeObject = levelSO;
                EditorGUIUtility.PingObject(levelSO);
            }
        }

        _generationImportText = report.ToText();
        RefreshAll();
        EditorUtility.DisplayDialog("AI 후보 가져오기", report.GetDialogMessage(), "OK");
    }

    private void SaveLevel()
    {
        if (_levelSO != null)
        {
            EditorUtility.SetDirty(_levelSO);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private void RunFinalValidation()
    {
        LevelSOMapper mapper = new LevelSOMapper();
        LevelDefinitionValidator validator = new LevelDefinitionValidator();
        LevelDefinition definition = mapper.ToLevelDefinition(_levelSO);
        LevelValidationResult result = validator.Validate(definition);

        if (result.IsValid)
        {
            _finalValidationText = "최종 검증 성공.";
        }
        else
        {
            _finalValidationText = "최종 검증 실패:\n";

            for (int i = 0; i < result.ErrorList.Length; i++)
            {
                _finalValidationText += "- " + result.ErrorList[i].Message + "\n";
            }
        }

        RefreshValidationLog();
    }

    private void RunTestCase(int testCaseIndex)
    {
        if (_levelSO == null || _serializedObject == null)
        {
            ClearTestRunState();
            _testRunText = "테스트 실행 실패: LevelSO가 선택되지 않았습니다.";
            RefreshAll();
            return;
        }

        _serializedObject.Update();
        SerializedProperty testCaseProperty = FindTestCaseProperty(testCaseIndex);

        if (testCaseProperty == null)
        {
            ClearTestRunState();
            _testRunText = $"테스트 실행 실패: 테스트 케이스 {testCaseIndex}를 찾을 수 없습니다.";
            RefreshAll();
            return;
        }

        LevelSOMapper mapper = new LevelSOMapper();
        LevelDefinitionValidator validator = new LevelDefinitionValidator();
        LevelDefinition definition = mapper.ToLevelDefinition(_levelSO);
        LevelValidationResult validationResult = validator.Validate(definition);

        if (!validationResult.IsValid)
        {
            ClearTestRunState();
            _testRunText = BuildValidationFailureText(validationResult);
            RefreshAll();
            return;
        }

        string testName = testCaseProperty.FindPropertyRelative("_name").stringValue;
        int maxRoundCount = testCaseProperty.FindPropertyRelative("_maxRoundCount").intValue;
        ESimulationEndState expectedEndState = (ESimulationEndState)testCaseProperty.FindPropertyRelative("_expectedEndState").enumValueIndex;
        SerializedProperty assignedConnectionListProperty = testCaseProperty.FindPropertyRelative("_assignedConnectionDataList");
        LevelSolveAssignment[] orderedAssignmentArray = CreateAssignmentArray(assignedConnectionListProperty);
        LevelDefinition orderedDefinition = LevelDefinitionSelectionOrderUtility.CreateWithAssignmentOrder(definition,
                                                                                                          orderedAssignmentArray);
        LevelBoardFactory factory = new LevelBoardFactory();
        Board board = factory.CreateBoard(orderedDefinition);
        Dictionary<int, string> connectionLabelByIdDict = new Dictionary<int, string>();
        List<LevelTestConnectionRunData> connectionRunDataList = new List<LevelTestConnectionRunData>();
        List<string> lineList = new List<string>();

        lineList.Add($"테스트: {GetTestCaseDisplayName(testName, testCaseIndex)}");
        lineList.Add($"예약 연결 수: {assignedConnectionListProperty.arraySize}");

        bool hasAssignFailure = AssignTestConnections(board,
                                                       assignedConnectionListProperty,
                                                       connectionLabelByIdDict,
                                                       connectionRunDataList,
                                                       lineList);

        if (hasAssignFailure)
        {
            ClearTestRunState();
            _testRunText = string.Join("\n", lineList);
            RefreshAll();
            return;
        }

        SimulationReport report = board.RunSimulation(maxRoundCount);
        bool passed = report.EndState == expectedEndState;

        lineList.Add($"결과: {report.EndState} / 예상: {expectedEndState} / {(passed ? "통과" : "실패")}");
        lineList.Add(BuildStarEvaluationLine(report));
        lineList.Add($"완료 Process: {FormatIdArray(report.CompletedProcessIdList)}");
        lineList.Add($"차단 Connection: {FormatConnectionIdArray(report.BlockedConnectionIdList, connectionLabelByIdDict)}");
        AddRoundResultLines(lineList, report, connectionLabelByIdDict);

        SetTestRunState(testCaseIndex, report, connectionRunDataList);
        _testRunText = string.Join("\n", lineList);
        RefreshAll();
    }

    private LevelSolveAssignment[] CreateAssignmentArray(SerializedProperty assignedConnectionListProperty)
    {
        if (assignedConnectionListProperty == null)
        {
            return new LevelSolveAssignment[0];
        }

        LevelSolveAssignment[] assignmentArray = new LevelSolveAssignment[assignedConnectionListProperty.arraySize];

        for (int i = 0; i < assignedConnectionListProperty.arraySize; i++)
        {
            SerializedProperty assignedConnectionProperty = assignedConnectionListProperty.GetArrayElementAtIndex(i);
            assignmentArray[i] = new LevelSolveAssignment(assignedConnectionProperty.FindPropertyRelative("_processId").intValue,
                                                          assignedConnectionProperty.FindPropertyRelative("_slotId").intValue,
                                                          assignedConnectionProperty.FindPropertyRelative("_resourceId").intValue);
        }

        return assignmentArray;
    }

    private bool AssignTestConnections(Board board,
                                       SerializedProperty assignedConnectionListProperty,
                                       Dictionary<int, string> connectionLabelByIdDict,
                                       List<LevelTestConnectionRunData> connectionRunDataList,
                                       List<string> lineList)
    {
        bool hasAssignFailure = false;

        for (int i = 0; i < assignedConnectionListProperty.arraySize; i++)
        {
            SerializedProperty assignedConnectionProperty = assignedConnectionListProperty.GetArrayElementAtIndex(i);
            int processId = assignedConnectionProperty.FindPropertyRelative("_processId").intValue;
            int slotId = assignedConnectionProperty.FindPropertyRelative("_slotId").intValue;
            int resourceId = assignedConnectionProperty.FindPropertyRelative("_resourceId").intValue;
            string label = CreateConnectionLabel(processId, slotId, resourceId);

            AssignConnectionResult result = board.AssignConnection(processId, slotId, resourceId);

            if (result.Success)
            {
                connectionLabelByIdDict[result.ConnectionId] = label;
                connectionRunDataList.Add(new LevelTestConnectionRunData(result.ConnectionId,
                                                                         processId,
                                                                         slotId,
                                                                         resourceId));
                lineList.Add($"예약 성공: C{result.ConnectionId} {label}");
                continue;
            }

            hasAssignFailure = true;
            lineList.Add($"예약 실패: {label} / {result.Error}");
        }

        return hasAssignFailure;
    }

    private void SetTestRunState(int testCaseIndex,
                                 SimulationReport report,
                                 IReadOnlyList<LevelTestConnectionRunData> connectionRunDataList)
    {
        _lastRunTestCaseIndex = testCaseIndex;
        _lastTestSimulationReport = report;
        _lastTestConnectionRunDataList.Clear();

        for (int i = 0; i < connectionRunDataList.Count; i++)
        {
            _lastTestConnectionRunDataList.Add(connectionRunDataList[i]);
        }

        _selectedTestRoundIndex = report.RoundResultList.Length > 0 ? 0 : -1;
    }

    private void AddRoundResultLines(List<string> lineList,
                                     SimulationReport report,
                                     Dictionary<int, string> connectionLabelByIdDict)
    {
        if (report.RoundResultList.Length == 0)
        {
            lineList.Add("라운드 결과 없음.");
            return;
        }

        for (int i = 0; i < report.RoundResultList.Length; i++)
        {
            RoundResult roundResult = report.RoundResultList[i];
            lineList.Add($"라운드 {roundResult.RoundIndex + 1}");

            bool hasAnyLine = false;
            hasAnyLine |= AddConnectionListLine(lineList, "점유", roundResult.OccupiedConnectionIdList, connectionLabelByIdDict);
            hasAnyLine |= AddConnectionListLine(lineList, "대기", roundResult.WaitingConnectionIdList, connectionLabelByIdDict);
            hasAnyLine |= AddConnectionListLine(lineList, "재투입", roundResult.RequeuedConnectionIdList, connectionLabelByIdDict);
            hasAnyLine |= AddConnectionListLine(lineList, "이월", roundResult.DeferredConnectionIdList, connectionLabelByIdDict);
            hasAnyLine |= AddIdListLine(lineList, "완료 Process", roundResult.CompletedProcessIdList);
            hasAnyLine |= AddConnectionListLine(lineList, "반환", roundResult.ReleasedConnectionIdList, connectionLabelByIdDict);
            hasAnyLine |= AddIdListLine(lineList, "실패 Process", roundResult.FailedProcessIdList);
            hasAnyLine |= AddConnectionListLine(lineList, "차단", roundResult.BlockedConnectionIdList, connectionLabelByIdDict);

            if (!hasAnyLine)
            {
                lineList.Add("- 변화 없음");
            }
        }
    }

    private bool AddConnectionListLine(List<string> lineList,
                                       string label,
                                       IReadOnlyList<int> connectionIdList,
                                       Dictionary<int, string> connectionLabelByIdDict)
    {
        if (connectionIdList.Count == 0)
        {
            return false;
        }

        lineList.Add($"- {label}: {FormatConnectionIdList(connectionIdList, connectionLabelByIdDict)}");
        return true;
    }

    private bool AddIdListLine(List<string> lineList, string label, IReadOnlyList<int> idList)
    {
        if (idList.Count == 0)
        {
            return false;
        }

        lineList.Add($"- {label}: {FormatIdList(idList)}");
        return true;
    }

    private string BuildStarEvaluationLine(SimulationReport report)
    {
        if (!TryGetStarThreshold(out int threeStarRoundCount,
                                 out int twoStarRoundCount,
                                 out int oneStarRoundCount))
        {
            return "별 평가: 별 기준 없음";
        }

        if (report.EndState != ESimulationEndState.Succeeded)
        {
            return $"별 평가: 0별 / 기준 3별 {threeStarRoundCount}R, 2별 {twoStarRoundCount}R, 1별 {oneStarRoundCount}R";
        }

        int clearRoundCount = report.RoundResultList.Length;
        int starCount = 0;

        if (clearRoundCount <= threeStarRoundCount)
        {
            starCount = 3;
        }
        else if (clearRoundCount <= twoStarRoundCount)
        {
            starCount = 2;
        }
        else if (clearRoundCount <= oneStarRoundCount)
        {
            starCount = 1;
        }

        return $"별 평가: {starCount}별 / 클리어 {clearRoundCount}R / 기준 3별 {threeStarRoundCount}R, 2별 {twoStarRoundCount}R, 1별 {oneStarRoundCount}R";
    }

    private bool TryGetStarThreshold(out int threeStarRoundCount,
                                     out int twoStarRoundCount,
                                     out int oneStarRoundCount)
    {
        threeStarRoundCount = 0;
        twoStarRoundCount = 0;
        oneStarRoundCount = 0;

        if (_serializedObject == null)
        {
            return false;
        }

        SerializedProperty starThresholdProperty = _serializedObject.FindProperty("_starThresholdData");

        if (starThresholdProperty == null)
        {
            return false;
        }

        threeStarRoundCount = starThresholdProperty.FindPropertyRelative("_threeStarRoundCount").intValue;
        twoStarRoundCount = starThresholdProperty.FindPropertyRelative("_twoStarRoundCount").intValue;
        oneStarRoundCount = starThresholdProperty.FindPropertyRelative("_oneStarRoundCount").intValue;

        return threeStarRoundCount > 0 &&
               twoStarRoundCount >= threeStarRoundCount &&
               oneStarRoundCount >= twoStarRoundCount;
    }

    private string BuildValidationFailureText(LevelValidationResult validationResult)
    {
        List<string> lineList = new List<string>
        {
            "테스트 실행 실패: 최종 검증 오류가 있습니다.",
        };

        for (int i = 0; i < validationResult.ErrorList.Length; i++)
        {
            lineList.Add("- " + validationResult.ErrorList[i].Message);
        }

        return string.Join("\n", lineList);
    }

    private SerializedProperty FindTestCaseProperty(int testCaseIndex)
    {
        SerializedProperty testCaseListProperty = _serializedObject.FindProperty("_testCaseDataList");

        if (testCaseListProperty == null ||
            testCaseIndex < 0 ||
            testCaseIndex >= testCaseListProperty.arraySize)
        {
            return null;
        }

        return testCaseListProperty.GetArrayElementAtIndex(testCaseIndex);
    }

    private string GetTestCaseDisplayName(string testName, int testCaseIndex)
    {
        return string.IsNullOrEmpty(testName) ? $"Test {testCaseIndex + 1}" : testName;
    }

    private string CreateConnectionLabel(int processId, int slotId, int resourceId)
    {
        return $"P{processId}:S{slotId} -> R{resourceId}";
    }

    private string FormatConnectionIdArray(int[] connectionIdArray, Dictionary<int, string> connectionLabelByIdDict)
    {
        return FormatConnectionIdList(connectionIdArray, connectionLabelByIdDict);
    }

    private string FormatConnectionIdList(IReadOnlyList<int> connectionIdList,
                                          Dictionary<int, string> connectionLabelByIdDict)
    {
        if (connectionIdList.Count == 0)
        {
            return "없음";
        }

        List<string> labelList = new List<string>();

        for (int i = 0; i < connectionIdList.Count; i++)
        {
            int connectionId = connectionIdList[i];

            if (connectionLabelByIdDict.TryGetValue(connectionId, out string label))
            {
                labelList.Add($"C{connectionId} {label}");
            }
            else
            {
                labelList.Add($"C{connectionId}");
            }
        }

        return string.Join(", ", labelList);
    }

    private string FormatIdArray(int[] idArray)
    {
        return FormatIdList(idArray);
    }

    private string FormatIdList(IReadOnlyList<int> idList)
    {
        if (idList.Count == 0)
        {
            return "없음";
        }

        List<string> labelList = new List<string>();

        for (int i = 0; i < idList.Count; i++)
        {
            labelList.Add(idList[i].ToString());
        }

        return string.Join(", ", labelList);
    }

    private void ReloadColors()
    {
        _colorMap.Reload();
        RefreshAll();
    }

    private void RefreshValidationLog()
    {
        if (_validationContainer == null)
        {
            return;
        }

        _validationContainer.Clear();
        _validationContainer.Add(CreateSectionTitle("검증"));

        string[] messageArray = _validationUtility.Validate(_levelSO);

        for (int i = 0; i < messageArray.Length; i++)
        {
            Label messageLabel = new Label(messageArray[i]);
            messageLabel.style.color = messageArray[i].StartsWith("즉시 검증 문제 없음") ?
                new StyleColor(new Color(0.52f, 0.86f, 0.55f)) :
                new StyleColor(new Color(1f, 0.78f, 0.42f));
            _validationContainer.Add(messageLabel);
        }

        if (!string.IsNullOrEmpty(_finalValidationText))
        {
            _validationContainer.Add(CreateSectionTitle("최종 검증"));
            _validationContainer.Add(new Label(_finalValidationText));
        }

        if (!string.IsNullOrEmpty(_solutionFinderText))
        {
            _validationContainer.Add(CreateSectionTitle("자동 해 찾기"));
            Label solutionLabel = new Label(_solutionFinderText);
            solutionLabel.style.whiteSpace = WhiteSpace.Normal;
            _validationContainer.Add(solutionLabel);
        }

        if (!string.IsNullOrEmpty(_generationImportText))
        {
            _validationContainer.Add(CreateSectionTitle("AI 후보 가져오기"));
            _validationContainer.Add(CreateHeaderButton("리포트 복사", CopyGenerationImportReport));
            Label importLabel = new Label(_generationImportText);
            importLabel.style.whiteSpace = WhiteSpace.Normal;
            _validationContainer.Add(importLabel);
        }

        if (!string.IsNullOrEmpty(_testRunText))
        {
            _validationContainer.Add(CreateSectionTitle("테스트 실행"));
            BuildRoundVisualizationControls();
            Label testRunLabel = new Label(_testRunText);
            testRunLabel.style.whiteSpace = WhiteSpace.Normal;
            _validationContainer.Add(testRunLabel);
        }
    }

    private void BuildRoundVisualizationControls()
    {
        if (_lastTestSimulationReport == null || _lastTestSimulationReport.RoundResultList.Length == 0)
        {
            return;
        }

        VisualElement box = CreateNestedBox();
        _validationContainer.Add(box);

        Label titleLabel = new Label("라운드 보드 표시");
        titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        box.Add(titleLabel);

        VisualElement controlRow = CreateButtonRow();
        Button previousButton = CreateHeaderButton("이전", () => SetSelectedTestRoundIndex(_selectedTestRoundIndex - 1));
        previousButton.SetEnabled(_selectedTestRoundIndex > 0);
        controlRow.Add(previousButton);

        IntegerField roundField = new IntegerField("라운드");
        roundField.SetValueWithoutNotify(_selectedTestRoundIndex + 1);
        roundField.style.width = 120f;
        roundField.RegisterValueChangedCallback(changeEvent =>
        {
            SetSelectedTestRoundIndex(changeEvent.newValue - 1);
        });
        controlRow.Add(roundField);

        Button nextButton = CreateHeaderButton("다음", () => SetSelectedTestRoundIndex(_selectedTestRoundIndex + 1));
        nextButton.SetEnabled(_selectedTestRoundIndex < _lastTestSimulationReport.RoundResultList.Length - 1);
        controlRow.Add(nextButton);

        Label countLabel = new Label($"/ {_lastTestSimulationReport.RoundResultList.Length}");
        countLabel.style.alignSelf = Align.Center;
        countLabel.style.marginLeft = 4f;
        controlRow.Add(countLabel);
        box.Add(controlRow);

        Label legendLabel = new Label("색상: 미출발=숨김, 점유=초록, waiting=노랑, 차단=빨강, 완료 Process=반투명");
        legendLabel.style.color = new StyleColor(new Color(0.78f, 0.82f, 0.88f));
        legendLabel.style.whiteSpace = WhiteSpace.Normal;
        box.Add(legendLabel);
    }

    private void CopyGenerationImportReport()
    {
        if (string.IsNullOrEmpty(_generationImportText))
        {
            return;
        }

        EditorGUIUtility.systemCopyBuffer = _generationImportText;
    }

    private void SetSelectedTestRoundIndex(int roundIndex)
    {
        if (_lastTestSimulationReport == null || _lastTestSimulationReport.RoundResultList.Length == 0)
        {
            _selectedTestRoundIndex = -1;
            RefreshAll();
            return;
        }

        _selectedTestRoundIndex = Mathf.Clamp(roundIndex, 0, _lastTestSimulationReport.RoundResultList.Length - 1);
        RefreshAll();
    }

    private LevelProcessData FindProcessData(int row, int column)
    {
        if (_levelSO == null)
        {
            return null;
        }

        for (int i = 0; i < _levelSO.ProcessDataList.Count; i++)
        {
            LevelProcessData processData = _levelSO.ProcessDataList[i];

            if (processData != null &&
                processData.Row == row &&
                processData.Column == column)
            {
                return processData;
            }
        }

        return null;
    }

    private bool TryGetProcessDataById(int processId, out LevelProcessData processData)
    {
        processData = null;

        if (_levelSO == null)
        {
            return false;
        }

        for (int i = 0; i < _levelSO.ProcessDataList.Count; i++)
        {
            LevelProcessData currentProcessData = _levelSO.ProcessDataList[i];

            if (currentProcessData != null && currentProcessData.Id == processId)
            {
                processData = currentProcessData;
                return true;
            }
        }

        return false;
    }

    private LevelResourceData FindResourceData(int row, int column)
    {
        if (_levelSO == null)
        {
            return null;
        }

        for (int i = 0; i < _levelSO.ResourceDataList.Count; i++)
        {
            LevelResourceData resourceData = _levelSO.ResourceDataList[i];

            if (resourceData != null &&
                resourceData.Row == row &&
                resourceData.Column == column)
            {
                return resourceData;
            }
        }

        return null;
    }

    private bool TryGetResourceDataById(int resourceId, out LevelResourceData resourceData)
    {
        resourceData = null;

        if (_levelSO == null)
        {
            return false;
        }

        for (int i = 0; i < _levelSO.ResourceDataList.Count; i++)
        {
            LevelResourceData currentResourceData = _levelSO.ResourceDataList[i];

            if (currentResourceData != null && currentResourceData.Id == resourceId)
            {
                resourceData = currentResourceData;
                return true;
            }
        }

        return false;
    }

    private bool TryGetSlotData(LevelProcessData processData, int slotId, out LevelProcessSlotData slotData)
    {
        slotData = null;

        if (processData == null)
        {
            return false;
        }

        for (int i = 0; i < processData.SlotDataList.Count; i++)
        {
            LevelProcessSlotData currentSlotData = processData.SlotDataList[i];

            if (currentSlotData != null && currentSlotData.Id == slotId)
            {
                slotData = currentSlotData;
                return true;
            }
        }

        return false;
    }

    private SerializedProperty FindProcessProperty(int row, int column, out int processIndex)
    {
        return FindNodeProperty("_processDataList", row, column, out processIndex);
    }

    private SerializedProperty FindResourceProperty(int row, int column, out int resourceIndex)
    {
        return FindNodeProperty("_resourceDataList", row, column, out resourceIndex);
    }

    private SerializedProperty FindNodeProperty(string listPath, int row, int column, out int index)
    {
        index = -1;

        if (_serializedObject == null)
        {
            return null;
        }

        SerializedProperty listProperty = _serializedObject.FindProperty(listPath);

        for (int i = 0; i < listProperty.arraySize; i++)
        {
            SerializedProperty elementProperty = listProperty.GetArrayElementAtIndex(i);

            if (elementProperty.FindPropertyRelative("_row").intValue == row &&
                elementProperty.FindPropertyRelative("_column").intValue == column)
            {
                index = i;
                return elementProperty;
            }
        }

        return null;
    }

    private int GetDefaultNodeId(int row, int column)
    {
        int safeColumnCount = Mathf.Max(1, _levelSO.ColumnCount);
        return row * safeColumnCount + column;
    }
}

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

internal sealed class LevelBoardGraphView : GraphView
{
    private const string ClipboardHeader = "DeadLockLevelEditorNodesV1";
    private const float GridUnit = 96f;
    private const float CanvasPadding = 80f;

    private static readonly Color BasicBadgeColor = new Color(0.33f, 0.35f, 0.39f);
    private static readonly Color ColorSwitchBadgeColor = new Color(0.23f, 0.46f, 0.73f);
    private static readonly Color EmptyColorBadgeColor = new Color(0.42f, 0.44f, 0.49f);
    private static readonly Color ClockBadgeColor = new Color(0.73f, 0.47f, 0.18f);
    private static readonly Color SimultaneousBadgeColor = new Color(0.38f, 0.54f, 0.25f);
    private static readonly Color RelayLinkBadgeColor = new Color(0.44f, 0.32f, 0.64f);
    private static readonly Color RelayTransferBadgeColor = new Color(0.70f, 0.28f, 0.34f);

    private readonly LevelEditorWindow _window;
    private readonly LevelEditorColorMap _colorMap;
    private readonly List<LevelBoardNodeView> _selectedNodeViewList = new List<LevelBoardNodeView>();
    private LevelSO _levelSO;
    private LevelBoardBoundsElement _boundsElement;
    private LevelRelayLineElement _relayLineElement;
    private LevelBoardNodeView _draggingNodeView;
    private Vector2 _dragStartMousePosition;
    private Vector2 _dragStartNodePosition;
    private bool _isRefreshing;

    public LevelBoardGraphView(LevelEditorWindow window, LevelEditorColorMap colorMap)
    {
        _window = window;
        _colorMap = colorMap;

        focusable = true;
        style.flexGrow = 1f;
        style.backgroundColor = new StyleColor(new Color(0.10f, 0.11f, 0.13f));

        Insert(0, new GridBackground());
        SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
        this.AddManipulator(new ContentDragger());
        this.AddManipulator(new SelectionDragger());
        this.AddManipulator(new RectangleSelector());

        graphViewChanged = OnGraphViewChanged;
        RegisterCallback<MouseDownEvent>(OnMouseDown, TrickleDown.TrickleDown);
        RegisterCallback<KeyDownEvent>(OnKeyDown);
    }

    public void Populate(LevelSO levelSO)
    {
        _isRefreshing = true;
        _levelSO = levelSO;
        ClearNodeSelection();
        List<GraphElement> elementList = new List<GraphElement>();

        foreach (GraphElement graphElement in graphElements)
        {
            elementList.Add(graphElement);
        }

        DeleteElements(elementList);
        _boundsElement?.RemoveFromHierarchy();
        _boundsElement = null;
        _relayLineElement?.RemoveFromHierarchy();
        _relayLineElement = null;

        if (levelSO != null)
        {
            AddBoardBounds(levelSO);
            AddRelayLines(levelSO);
            AddProcessNodes(levelSO);
            AddResourceNodes(levelSO);
        }

        _isRefreshing = false;
    }

    private void AddBoardBounds(LevelSO levelSO)
    {
        _boundsElement = new LevelBoardBoundsElement(levelSO.RowCount,
                                                     levelSO.ColumnCount,
                                                     GridUnit);
        _boundsElement.style.left = CanvasPadding;
        _boundsElement.style.top = CanvasPadding;
        contentViewContainer.Insert(0, _boundsElement);
        _boundsElement.SendToBack();
    }

    private void AddRelayLines(LevelSO levelSO)
    {
        List<LevelRelayLineData> lineDataList = CreateRelayLineDataList(levelSO);

        if (lineDataList.Count == 0)
        {
            return;
        }

        float width = CanvasPadding * 2f + levelSO.ColumnCount * GridUnit;
        float height = CanvasPadding * 2f + levelSO.RowCount * GridUnit;
        _relayLineElement = new LevelRelayLineElement(lineDataList, width, height);
        contentViewContainer.Insert(1, _relayLineElement);
    }

    private List<LevelRelayLineData> CreateRelayLineDataList(LevelSO levelSO)
    {
        Dictionary<int, Vector2> resourceCenterByIdDict = CreateResourceCenterByIdDict(levelSO);
        List<LevelRelayLineData> lineDataList = new List<LevelRelayLineData>();

        for (int i = 0; i < levelSO.RelayDataList.Count; i++)
        {
            LevelRelayData relayData = levelSO.RelayDataList[i];

            if (relayData == null ||
                !TryGetRelayLinePositions(relayData,
                                          resourceCenterByIdDict,
                                          out Vector2 startPosition,
                                          out Vector2 endPosition))
            {
                continue;
            }

            bool isHighlighted = _window.SelectedRelayIndex == i;
            bool isFocused = IsRelayConnectedToResource(relayData, _window.SelectedResourceId);

            lineDataList.Add(new LevelRelayLineData(startPosition,
                                                    endPosition,
                                                    relayData.RelayType,
                                                    isHighlighted,
                                                    isFocused,
                                                    false));
        }

        if (_window.HasRelayDraftPair &&
            TryGetDraftRelayLinePositions(resourceCenterByIdDict, out Vector2 draftStart, out Vector2 draftEnd))
        {
            lineDataList.Add(new LevelRelayLineData(draftStart,
                                                    draftEnd,
                                                    _window.RelayDraftType,
                                                    true,
                                                    true,
                                                    true));
        }

        return lineDataList;
    }

    private void RefreshRelayLines()
    {
        _relayLineElement?.RemoveFromHierarchy();
        _relayLineElement = null;

        if (_levelSO != null)
        {
            AddRelayLines(_levelSO);
        }
    }

    private bool IsRelayConnectedToResource(LevelRelayData relayData, int resourceId)
    {
        return resourceId >= 0 &&
               (relayData.FirstResourceId == resourceId || relayData.SecondResourceId == resourceId);
    }

    private Dictionary<int, Vector2> CreateResourceCenterByIdDict(LevelSO levelSO)
    {
        Dictionary<int, Vector2> resourceCenterByIdDict = new Dictionary<int, Vector2>();

        for (int i = 0; i < levelSO.ResourceDataList.Count; i++)
        {
            LevelResourceData resourceData = levelSO.ResourceDataList[i];

            if (resourceData != null)
            {
                resourceCenterByIdDict[resourceData.Id] = GetCellCenterPosition(resourceData.Row, resourceData.Column);
            }
        }

        return resourceCenterByIdDict;
    }

    private bool TryGetRelayLinePositions(LevelRelayData relayData,
                                          Dictionary<int, Vector2> resourceCenterByIdDict,
                                          out Vector2 startPosition,
                                          out Vector2 endPosition)
    {
        int startResourceId = relayData.FirstResourceId;
        int endResourceId = relayData.SecondResourceId;

        if (relayData.RelayType == ERelayType.Transfer &&
            (relayData.SenderResourceId == relayData.FirstResourceId ||
             relayData.SenderResourceId == relayData.SecondResourceId))
        {
            startResourceId = relayData.SenderResourceId;
            endResourceId = relayData.SenderResourceId == relayData.FirstResourceId ?
                relayData.SecondResourceId :
                relayData.FirstResourceId;
        }

        return TryGetLinePositions(resourceCenterByIdDict,
                                   startResourceId,
                                   endResourceId,
                                   out startPosition,
                                   out endPosition);
    }

    private bool TryGetDraftRelayLinePositions(Dictionary<int, Vector2> resourceCenterByIdDict,
                                               out Vector2 startPosition,
                                               out Vector2 endPosition)
    {
        int startResourceId = _window.RelayDraftFirstResourceId;
        int endResourceId = _window.RelayDraftSecondResourceId;

        if (_window.RelayDraftType == ERelayType.Transfer &&
            (_window.RelayDraftSenderResourceId == _window.RelayDraftFirstResourceId ||
             _window.RelayDraftSenderResourceId == _window.RelayDraftSecondResourceId))
        {
            startResourceId = _window.RelayDraftSenderResourceId;
            endResourceId = _window.RelayDraftSenderResourceId == _window.RelayDraftFirstResourceId ?
                _window.RelayDraftSecondResourceId :
                _window.RelayDraftFirstResourceId;
        }

        return TryGetLinePositions(resourceCenterByIdDict,
                                   startResourceId,
                                   endResourceId,
                                   out startPosition,
                                   out endPosition);
    }

    private bool TryGetLinePositions(Dictionary<int, Vector2> resourceCenterByIdDict,
                                     int startResourceId,
                                     int endResourceId,
                                     out Vector2 startPosition,
                                     out Vector2 endPosition)
    {
        if (!resourceCenterByIdDict.TryGetValue(startResourceId, out startPosition) ||
            !resourceCenterByIdDict.TryGetValue(endResourceId, out endPosition))
        {
            startPosition = Vector2.zero;
            endPosition = Vector2.zero;
            return false;
        }

        return true;
    }

    private void AddProcessNodes(LevelSO levelSO)
    {
        for (int i = 0; i < levelSO.ProcessDataList.Count; i++)
        {
            LevelProcessData processData = levelSO.ProcessDataList[i];

            if (processData == null)
            {
                continue;
            }

            LevelBoardNodeView nodeView = new LevelBoardNodeView(ELevelEditorNodeKind.Process,
                                                                 i,
                                                                 processData.Row,
                                                                 processData.Column,
                                                                 _window.GetProcessColorIds(processData),
                                                                 null,
                                                                 _window.HasProcessIssue(processData),
                                                                 _colorMap);
            AddNode(nodeView, processData.Row, processData.Column);
        }
    }

    private void AddResourceNodes(LevelSO levelSO)
    {
        for (int i = 0; i < levelSO.ResourceDataList.Count; i++)
        {
            LevelResourceData resourceData = levelSO.ResourceDataList[i];

            if (resourceData == null)
            {
                continue;
            }

            LevelBoardNodeView nodeView = new LevelBoardNodeView(ELevelEditorNodeKind.Resource,
                                                                 i,
                                                                 resourceData.Row,
                                                                 resourceData.Column,
                                                                 _window.GetResourceColorIds(resourceData),
                                                                 GetResourceBadgeDataList(levelSO, resourceData),
                                                                 _window.HasResourceIssue(resourceData),
                                                                 _colorMap);
            AddNode(nodeView, resourceData.Row, resourceData.Column);
        }
    }

    private List<LevelResourceBadgeData> GetResourceBadgeDataList(LevelSO levelSO, LevelResourceData resourceData)
    {
        List<LevelResourceBadgeData> badgeDataList = new List<LevelResourceBadgeData>();
        AddRuleBadges(resourceData, badgeDataList);
        AddRelayBadges(levelSO, resourceData, badgeDataList);
        return badgeDataList;
    }

    private void AddRuleBadges(LevelResourceData resourceData, List<LevelResourceBadgeData> badgeDataList)
    {
        for (int i = 0; i < resourceData.RuleDataList.Count; i++)
        {
            LevelResourceRuleData ruleData = resourceData.RuleDataList[i];

            if (ruleData == null)
            {
                continue;
            }

            switch (ruleData.RuleType)
            {
                case ELevelResourceRuleType.Basic:
                    if (resourceData.RuleDataList.Count == 1)
                    {
                        badgeDataList.Add(new LevelResourceBadgeData("B", "Basic: 현재 색과 슬롯 색이 같아야 점유 가능", BasicBadgeColor));
                    }

                    break;

                case ELevelResourceRuleType.ColorSwitch:
                    badgeDataList.Add(new LevelResourceBadgeData("SW", "ColorSwitch: 반환/idle 라운드마다 색 전환", ColorSwitchBadgeColor));
                    break;

                case ELevelResourceRuleType.EmptyColor:
                    badgeDataList.Add(new LevelResourceBadgeData("EM", "EmptyColor: 첫 점유 색으로 고정", EmptyColorBadgeColor));
                    break;

                case ELevelResourceRuleType.Clock:
                    badgeDataList.Add(new LevelResourceBadgeData("CK", $"Clock: {ruleData.ClockMode}, {ruleData.ClockRoundCount}라운드", ClockBadgeColor));
                    break;

                case ELevelResourceRuleType.Simultaneous:
                    badgeDataList.Add(new LevelResourceBadgeData($"x{resourceData.Capacity}", "Simultaneous: capacity 수만큼 동시 점유 필요", SimultaneousBadgeColor));
                    break;
            }
        }
    }

    private void AddRelayBadges(LevelSO levelSO, LevelResourceData resourceData, List<LevelResourceBadgeData> badgeDataList)
    {
        for (int i = 0; i < levelSO.RelayDataList.Count; i++)
        {
            LevelRelayData relayData = levelSO.RelayDataList[i];

            if (relayData == null ||
                (relayData.FirstResourceId != resourceData.Id && relayData.SecondResourceId != resourceData.Id))
            {
                continue;
            }

            if (relayData.RelayType == ERelayType.Link)
            {
                badgeDataList.Add(new LevelResourceBadgeData("L", "RelayLink: 반대편 점유 중 사용 제한", RelayLinkBadgeColor));
                continue;
            }

            if (relayData.RelayType == ERelayType.Transfer)
            {
                string label = relayData.SenderResourceId == resourceData.Id ? "TX" : "RX";
                string tooltip = relayData.SenderResourceId == resourceData.Id ?
                    "RelayTransfer Sender: 점유 색을 Receiver로 전달" :
                    "RelayTransfer Receiver: Sender 점유 색을 임시 색으로 받음";
                badgeDataList.Add(new LevelResourceBadgeData(label, tooltip, RelayTransferBadgeColor));
            }
        }
    }

    private void AddNode(LevelBoardNodeView nodeView, int row, int column)
    {
        AddElement(nodeView);
        nodeView.RegisterCallback<MouseDownEvent>(mouseDownEvent => HandleNodeMouseDown(mouseDownEvent, nodeView));
        nodeView.RegisterCallback<MouseMoveEvent>(mouseMoveEvent => HandleNodeMouseMove(mouseMoveEvent, nodeView));
        nodeView.RegisterCallback<MouseUpEvent>(mouseUpEvent => HandleNodeMouseUp(mouseUpEvent, nodeView));
        nodeView.SetPosition(new Rect(GetNodePosition(row, column), Vector2.zero));
    }

    private void HandleNodeMouseDown(MouseDownEvent mouseDownEvent, LevelBoardNodeView nodeView)
    {
        Focus();

        if (mouseDownEvent.button != 0)
        {
            return;
        }

        if (_window.CurrentTool == ELevelEditorTool.Erase)
        {
            _window.DeleteNodesFromGraph(new List<LevelBoardNodeView> { nodeView });
            mouseDownEvent.StopPropagation();
            return;
        }

        if (_window.CurrentTool == ELevelEditorTool.AddRelay)
        {
            _window.SelectNodeForRelayFromGraph(nodeView.NodeKind, nodeView.DataIndex);
            mouseDownEvent.StopPropagation();
            return;
        }

        SelectNodeView(nodeView, mouseDownEvent.ctrlKey || mouseDownEvent.commandKey);
        _window.SelectNodeFromGraph(nodeView.NodeKind, nodeView.DataIndex);
        RefreshRelayLines();
        BeginNodeDrag(nodeView, mouseDownEvent);
        mouseDownEvent.StopPropagation();
    }

    private void SelectNodeView(LevelBoardNodeView nodeView, bool additive)
    {
        if (!additive)
        {
            ClearNodeSelection();
        }

        if (!_selectedNodeViewList.Contains(nodeView))
        {
            _selectedNodeViewList.Add(nodeView);
            nodeView.SetEditorSelected(true);
            AddToSelection(nodeView);
        }
    }

    private void ClearNodeSelection()
    {
        for (int i = 0; i < _selectedNodeViewList.Count; i++)
        {
            _selectedNodeViewList[i].SetEditorSelected(false);
        }

        _selectedNodeViewList.Clear();
        ClearSelection();
    }

    private void BeginNodeDrag(LevelBoardNodeView nodeView, MouseDownEvent mouseDownEvent)
    {
        _draggingNodeView = nodeView;
        _dragStartMousePosition = GetGraphPosition(mouseDownEvent.localMousePosition, nodeView);
        _dragStartNodePosition = nodeView.GetPosition().position;
        nodeView.CaptureMouse();
    }

    private void HandleNodeMouseMove(MouseMoveEvent mouseMoveEvent, LevelBoardNodeView nodeView)
    {
        if (_draggingNodeView != nodeView || !nodeView.HasMouseCapture())
        {
            return;
        }

        Vector2 currentMousePosition = GetGraphPosition(mouseMoveEvent.localMousePosition, nodeView);
        Vector2 delta = currentMousePosition - _dragStartMousePosition;
        Rect position = nodeView.GetPosition();
        nodeView.SetPosition(new Rect(_dragStartNodePosition + delta, position.size));
        mouseMoveEvent.StopPropagation();
    }

    private void HandleNodeMouseUp(MouseUpEvent mouseUpEvent, LevelBoardNodeView nodeView)
    {
        if (_draggingNodeView != nodeView)
        {
            return;
        }

        if (nodeView.HasMouseCapture())
        {
            nodeView.ReleaseMouse();
        }

        _draggingNodeView = null;
        SnapAndStoreNode(nodeView);
        mouseUpEvent.StopPropagation();
    }

    private GraphViewChange OnGraphViewChanged(GraphViewChange change)
    {
        if (_isRefreshing)
        {
            return change;
        }

        if (change.elementsToRemove != null)
        {
            List<LevelBoardNodeView> nodeViewList = new List<LevelBoardNodeView>();

            for (int i = 0; i < change.elementsToRemove.Count; i++)
            {
                if (change.elementsToRemove[i] is LevelBoardNodeView nodeView)
                {
                    nodeViewList.Add(nodeView);
                }
            }

            if (nodeViewList.Count > 0)
            {
                _window.DeleteNodesFromGraph(nodeViewList);
                change.elementsToRemove = null;
                return change;
            }
        }

        if (change.movedElements != null)
        {
            for (int i = 0; i < change.movedElements.Count; i++)
            {
                if (change.movedElements[i] is LevelBoardNodeView nodeView)
                {
                    SnapAndStoreNode(nodeView);
                }
            }
        }

        return change;
    }

    private void SnapAndStoreNode(LevelBoardNodeView nodeView)
    {
        Rect position = nodeView.GetPosition();
        Vector2Int point = GetGridPointFromNodePosition(position.position);

        point.x = _window.ClampRow(point.x);
        point.y = _window.ClampColumn(point.y);
        Vector2 snappedPosition = GetNodePosition(point.x, point.y);

        bool moved = _window.TryMoveNodeFromGraph(nodeView.NodeKind, nodeView.DataIndex, point.x, point.y);

        if (moved)
        {
            nodeView.SetGridPoint(point.x, point.y);
            nodeView.SetPosition(new Rect(snappedPosition, position.size));
            RefreshRelayLines();
        }
        else
        {
            nodeView.SetPosition(new Rect(GetNodePosition(nodeView.Row, nodeView.Column), position.size));
        }
    }

    private void OnMouseDown(MouseDownEvent mouseDownEvent)
    {
        Focus();

        if (mouseDownEvent.button != 0)
        {
            return;
        }

        LevelBoardNodeView clickedNodeView = GetClickedNodeView(mouseDownEvent);

        if (clickedNodeView != null)
        {
            if (_window.CurrentTool == ELevelEditorTool.Erase)
            {
                _window.DeleteNodesFromGraph(new List<LevelBoardNodeView> { clickedNodeView });
                mouseDownEvent.StopPropagation();
                return;
            }

            if (_window.CurrentTool == ELevelEditorTool.AddRelay)
            {
                _window.SelectNodeForRelayFromGraph(clickedNodeView.NodeKind, clickedNodeView.DataIndex);
                mouseDownEvent.StopPropagation();
                return;
            }

            SelectNodeView(clickedNodeView, mouseDownEvent.ctrlKey || mouseDownEvent.commandKey);
            _window.SelectNodeFromGraph(clickedNodeView.NodeKind, clickedNodeView.DataIndex);
            RefreshRelayLines();
            return;
        }

        if (_window.CurrentTool != ELevelEditorTool.AddProcess &&
            _window.CurrentTool != ELevelEditorTool.AddResource)
        {
            if (_window.CurrentTool == ELevelEditorTool.AddRelay)
            {
                _window.RejectRelaySelectionFromGraph("Relay는 Resource 두 개를 선택해 생성합니다.");
                mouseDownEvent.StopPropagation();
                return;
            }

            if (_window.CurrentTool == ELevelEditorTool.Select)
            {
                Vector2Int selectedPoint = GetMouseGridPoint(mouseDownEvent);
                ClearNodeSelection();
                _window.SelectGridPointFromGraph(selectedPoint.x, selectedPoint.y);
                RefreshRelayLines();
                mouseDownEvent.StopPropagation();
            }

            return;
        }

        Vector2Int point = GetMouseGridPoint(mouseDownEvent);

        ELevelEditorNodeKind nodeKind = _window.CurrentTool == ELevelEditorTool.AddProcess ?
            ELevelEditorNodeKind.Process :
            ELevelEditorNodeKind.Resource;

        _window.AddNodeFromGraph(nodeKind, point.x, point.y);
        mouseDownEvent.StopPropagation();
    }

    private LevelBoardNodeView GetClickedNodeView(MouseDownEvent mouseDownEvent)
    {
        VisualElement targetElement = mouseDownEvent.target as VisualElement;

        while (targetElement != null)
        {
            if (targetElement is LevelBoardNodeView nodeView)
            {
                return nodeView;
            }

            targetElement = targetElement.parent;
        }

        return null;
    }

    private Vector2Int GetMouseGridPoint(MouseDownEvent mouseDownEvent)
    {
        Vector2 graphPosition = GetGraphPosition(mouseDownEvent.localMousePosition, this);
        Vector2Int point = GetGridPointFromGraphPosition(graphPosition);
        point.x = _window.ClampRow(point.x);
        point.y = _window.ClampColumn(point.y);
        return point;
    }

    private Vector2 GetGraphPosition(Vector2 localMousePosition, VisualElement sourceElement)
    {
        Vector2 worldPosition = sourceElement.LocalToWorld(localMousePosition);
        return contentViewContainer.WorldToLocal(worldPosition);
    }

    private void OnKeyDown(KeyDownEvent keyDownEvent)
    {
        if (!keyDownEvent.ctrlKey && !keyDownEvent.commandKey)
        {
            return;
        }

        if (keyDownEvent.keyCode == KeyCode.C)
        {
            CopySelection();
            keyDownEvent.StopPropagation();
            return;
        }

        if (keyDownEvent.keyCode == KeyCode.V)
        {
            PasteSelection();
            keyDownEvent.StopPropagation();
        }
    }

    private void CopySelection()
    {
        List<LevelBoardNodeView> nodeViewList = new List<LevelBoardNodeView>(_selectedNodeViewList);

        if (nodeViewList.Count == 0)
        {
            return;
        }

        List<string> lineList = new List<string>
        {
            ClipboardHeader,
        };

        for (int i = 0; i < nodeViewList.Count; i++)
        {
            LevelBoardNodeView nodeView = nodeViewList[i];
            lineList.Add($"{nodeView.NodeKind}|{nodeView.DataIndex}");
        }

        EditorGUIUtility.systemCopyBuffer = string.Join("\n", lineList);
    }

    private void PasteSelection()
    {
        string copyBuffer = EditorGUIUtility.systemCopyBuffer;

        if (string.IsNullOrEmpty(copyBuffer) || !copyBuffer.StartsWith(ClipboardHeader))
        {
            return;
        }

        string[] lineArray = copyBuffer.Split('\n');
        for (int i = 1; i < lineArray.Length; i++)
        {
            string line = lineArray[i].Trim();

            if (string.IsNullOrEmpty(line))
            {
                continue;
            }

            string[] tokenArray = line.Split('|');

            if (tokenArray.Length != 2 ||
                !TryParseNodeKind(tokenArray[0], out ELevelEditorNodeKind nodeKind) ||
                !int.TryParse(tokenArray[1], out int dataIndex))
            {
                continue;
            }

            _window.DuplicateNodeFromGraph(nodeKind, dataIndex);
        }
    }

    private bool TryParseNodeKind(string text, out ELevelEditorNodeKind nodeKind)
    {
        if (text == ELevelEditorNodeKind.Process.ToString())
        {
            nodeKind = ELevelEditorNodeKind.Process;
            return true;
        }

        if (text == ELevelEditorNodeKind.Resource.ToString())
        {
            nodeKind = ELevelEditorNodeKind.Resource;
            return true;
        }

        nodeKind = ELevelEditorNodeKind.Process;
        return false;
    }

    private Vector2Int GetGridPointFromNodePosition(Vector2 nodePosition)
    {
        Vector2 bodyCenterPosition = nodePosition + new Vector2(LevelBoardNodeView.BodyCenterX,
                                                                LevelBoardNodeView.BodyCenterY);
        return GetGridPointFromGraphPosition(bodyCenterPosition);
    }

    private Vector2Int GetGridPointFromGraphPosition(Vector2 position)
    {
        int row = Mathf.RoundToInt((position.y - CanvasPadding - GridUnit * 0.5f) / GridUnit);
        int column = Mathf.RoundToInt((position.x - CanvasPadding - GridUnit * 0.5f) / GridUnit);
        return new Vector2Int(row, column);
    }

    private Vector2 GetNodePosition(int row, int column)
    {
        float left = CanvasPadding + column * GridUnit + GridUnit * 0.5f - LevelBoardNodeView.BodyCenterX;
        float top = CanvasPadding + row * GridUnit + GridUnit * 0.5f - LevelBoardNodeView.BodyCenterY;
        return new Vector2(left, top);
    }

    private Vector2 GetCellCenterPosition(int row, int column)
    {
        return new Vector2(CanvasPadding + column * GridUnit + GridUnit * 0.5f,
                           CanvasPadding + row * GridUnit + GridUnit * 0.5f);
    }
}

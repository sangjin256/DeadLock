using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

internal sealed class LevelBoardNodeView : GraphElement
{
    internal const float ViewWidth = 104f;
    internal const float ViewHeight = 104f;
    internal const float BodyCenterX = 52f;
    internal const float BodyCenterY = 37f;
    internal const float SelectionFrameSize = 96f;

    private const float NodeVisualSize = 66f;
    private const float ChipSize = 10f;

    private static readonly Color ProcessBorderColor = new Color(0.30f, 0.56f, 0.92f);
    private static readonly Color ResourceBorderColor = new Color(0.72f, 0.72f, 0.66f);
    private static readonly Color SelectionColor = new Color(0.30f, 0.56f, 0.92f, 0.95f);
    private static readonly Color WarningColor = new Color(0.90f, 0.24f, 0.20f);

    private readonly ELevelEditorNodeKind _nodeKind;
    private readonly int _dataIndex;
    private VisualElement _selectionFrame;
    private int _row;
    private int _column;

    public ELevelEditorNodeKind NodeKind => _nodeKind;
    public int DataIndex => _dataIndex;
    public int Row => _row;
    public int Column => _column;

    public LevelBoardNodeView(ELevelEditorNodeKind nodeKind,
                              int dataIndex,
                              int row,
                              int column,
                              IReadOnlyList<int> colorIdList,
                              bool hasWarning,
                              LevelEditorColorMap colorMap)
    {
        _nodeKind = nodeKind;
        _dataIndex = dataIndex;
        _row = row;
        _column = column;

        capabilities |= Capabilities.Selectable | Capabilities.Movable | Capabilities.Deletable;

        style.width = ViewWidth;
        style.height = ViewHeight;
        style.minHeight = ViewHeight;
        style.backgroundColor = new StyleColor(Color.clear);
        style.borderTopWidth = 0f;
        style.borderBottomWidth = 0f;
        style.borderLeftWidth = 0f;
        style.borderRightWidth = 0f;
        style.borderTopLeftRadius = 0f;
        style.borderTopRightRadius = 0f;
        style.borderBottomLeftRadius = 0f;
        style.borderBottomRightRadius = 0f;
        style.paddingLeft = 0f;
        style.paddingRight = 0f;
        style.paddingTop = 0f;
        style.paddingBottom = 0f;

        _selectionFrame = CreateSelectionFrame();
        Add(_selectionFrame);
        Add(CreateNodeVisual(nodeKind, colorIdList, hasWarning, colorMap));
    }

    public override void OnSelected()
    {
        base.OnSelected();
        _selectionFrame.style.display = DisplayStyle.Flex;
    }

    public override void OnUnselected()
    {
        base.OnUnselected();
        _selectionFrame.style.display = DisplayStyle.None;
    }

    public void SetGridPoint(int row, int column)
    {
        _row = row;
        _column = column;
    }

    public void SetEditorSelected(bool isSelected)
    {
        _selectionFrame.style.display = isSelected ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private VisualElement CreateSelectionFrame()
    {
        VisualElement frame = new VisualElement();
        frame.pickingMode = PickingMode.Ignore;
        frame.style.display = DisplayStyle.None;
        frame.style.position = Position.Absolute;
        frame.style.left = BodyCenterX - SelectionFrameSize * 0.5f;
        frame.style.top = BodyCenterY - SelectionFrameSize * 0.5f;
        frame.style.width = SelectionFrameSize;
        frame.style.height = SelectionFrameSize;
        frame.style.borderTopLeftRadius = 0f;
        frame.style.borderTopRightRadius = 0f;
        frame.style.borderBottomLeftRadius = 0f;
        frame.style.borderBottomRightRadius = 0f;
        SetBorder(frame, 2f, SelectionColor);
        return frame;
    }

    private VisualElement CreateNodeVisual(ELevelEditorNodeKind nodeKind,
                                           IReadOnlyList<int> colorIdList,
                                           bool hasWarning,
                                           LevelEditorColorMap colorMap)
    {
        VisualElement root = new VisualElement();
        root.style.alignItems = Align.Center;
        root.style.justifyContent = Justify.FlexStart;
        root.style.width = ViewWidth;
        root.style.height = ViewHeight;

        VisualElement body = new VisualElement();
        body.style.width = NodeVisualSize;
        body.style.height = NodeVisualSize;
        body.style.alignItems = Align.Center;
        body.style.justifyContent = Justify.Center;
        body.style.backgroundColor = new StyleColor(new Color(0.96f, 0.96f, 0.94f));
        body.style.borderTopLeftRadius = nodeKind == ELevelEditorNodeKind.Process ? NodeVisualSize : 8f;
        body.style.borderTopRightRadius = nodeKind == ELevelEditorNodeKind.Process ? NodeVisualSize : 8f;
        body.style.borderBottomLeftRadius = nodeKind == ELevelEditorNodeKind.Process ? NodeVisualSize : 8f;
        body.style.borderBottomRightRadius = nodeKind == ELevelEditorNodeKind.Process ? NodeVisualSize : 8f;
        SetBorder(body, 2f, hasWarning ? WarningColor : GetBorderColor(nodeKind));
        root.Add(body);

        Label kindLabel = new Label(nodeKind == ELevelEditorNodeKind.Process ? "P" : "R");
        kindLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        kindLabel.style.fontSize = 16f;
        kindLabel.style.color = new StyleColor(Color.black);
        body.Add(kindLabel);

        VisualElement colorStrip = CreateColorStrip(colorIdList, colorMap);
        root.Add(colorStrip);

        if (hasWarning)
        {
            root.Add(CreateWarningDot());
        }

        return root;
    }

    private VisualElement CreateColorStrip(IReadOnlyList<int> colorIdList, LevelEditorColorMap colorMap)
    {
        VisualElement strip = new VisualElement();
        strip.style.flexDirection = FlexDirection.Row;
        strip.style.flexWrap = Wrap.Wrap;
        strip.style.justifyContent = Justify.Center;
        strip.style.alignItems = Align.Center;
        strip.style.width = 86f;
        strip.style.marginTop = 5f;

        for (int i = 0; i < colorIdList.Count; i++)
        {
            strip.Add(CreateMiniChip(colorIdList[i], colorMap));
        }

        return strip;
    }

    private VisualElement CreateMiniChip(int colorId, LevelEditorColorMap colorMap)
    {
        VisualElement chip = new VisualElement();
        chip.style.width = ChipSize;
        chip.style.height = ChipSize;
        chip.style.marginLeft = 1f;
        chip.style.marginRight = 1f;
        chip.style.marginTop = 1f;
        chip.style.marginBottom = 1f;
        chip.style.borderTopLeftRadius = ChipSize;
        chip.style.borderTopRightRadius = ChipSize;
        chip.style.borderBottomLeftRadius = ChipSize;
        chip.style.borderBottomRightRadius = ChipSize;
        chip.style.backgroundColor = new StyleColor(colorMap.GetColor(colorId));
        SetBorder(chip, 1f, Color.black);
        return chip;
    }

    private VisualElement CreateWarningDot()
    {
        VisualElement dot = new VisualElement();
        dot.style.position = Position.Absolute;
        dot.style.right = 9f;
        dot.style.top = 5f;
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

    private Color GetBorderColor(ELevelEditorNodeKind nodeKind)
    {
        return nodeKind == ELevelEditorNodeKind.Process ? ProcessBorderColor : ResourceBorderColor;
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
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

internal sealed class LevelAssignmentLineElement : VisualElement
{
    private static readonly Color LineColor = new Color(0.32f, 0.88f, 0.92f);
    private static readonly Color OccupiedColor = new Color(0.48f, 0.86f, 0.32f);
    private static readonly Color WaitingColor = new Color(1.00f, 0.78f, 0.18f);
    private static readonly Color BlockedColor = new Color(1.00f, 0.26f, 0.22f);
    private const float IdleAlpha = 0.30f;
    private const float FocusedAlpha = 0.96f;
    private const float ActiveAlpha = 0.92f;
    private const float CompletedAlpha = 0.18f;
    private const float IdleLineWidth = 2.5f;
    private const float FocusedLineWidth = 4.5f;
    private const float ActiveLineWidth = 4f;
    private const float LabelSize = 20f;

    private readonly List<LevelAssignmentLineData> _lineDataList;

    public LevelAssignmentLineElement(IReadOnlyList<LevelAssignmentLineData> lineDataList, float width, float height)
    {
        pickingMode = PickingMode.Ignore;
        style.position = Position.Absolute;
        style.left = 0f;
        style.top = 0f;
        style.width = width;
        style.height = height;

        _lineDataList = new List<LevelAssignmentLineData>(lineDataList);
        generateVisualContent += DrawLines;
        AddOrderLabels();
    }

    private void DrawLines(MeshGenerationContext context)
    {
        Painter2D painter = context.painter2D;

        for (int i = 0; i < _lineDataList.Count; i++)
        {
            LevelAssignmentLineData lineData = _lineDataList[i];

            if (!lineData.IsConnectionVisible)
            {
                continue;
            }

            Color color = GetLineColor(lineData);
            float lineWidth = GetLineWidth(lineData);

            painter.strokeColor = color;
            painter.lineWidth = lineWidth;
            painter.BeginPath();
            painter.MoveTo(lineData.StartPosition);
            painter.LineTo(lineData.EndPosition);
            painter.Stroke();
        }
    }

    private void AddOrderLabels()
    {
        for (int i = 0; i < _lineDataList.Count; i++)
        {
            LevelAssignmentLineData lineData = _lineDataList[i];

            if (!lineData.IsConnectionVisible)
            {
                continue;
            }

            Vector2 midpoint = (lineData.StartPosition + lineData.EndPosition) * 0.5f;
            Label label = new Label(lineData.Order.ToString());
            label.pickingMode = PickingMode.Ignore;
            label.style.position = Position.Absolute;
            label.style.left = midpoint.x - LabelSize * 0.5f;
            label.style.top = midpoint.y - LabelSize * 0.5f;
            label.style.width = LabelSize;
            label.style.height = LabelSize;
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.fontSize = 10f;
            label.style.color = new StyleColor(Color.black);
            label.style.backgroundColor = new StyleColor(GetLabelColor(lineData));
            label.style.borderTopLeftRadius = LabelSize;
            label.style.borderTopRightRadius = LabelSize;
            label.style.borderBottomLeftRadius = LabelSize;
            label.style.borderBottomRightRadius = LabelSize;
            SetBorder(label, 1f, Color.black);
            Add(label);
        }
    }

    private Color GetLabelColor(LevelAssignmentLineData lineData)
    {
        Color color = GetBaseColor(lineData.VisualState);
        color.a = lineData.IsProcessCompleted ? 0.34f : GetLabelAlpha(lineData);
        return color;
    }

    private Color GetLineColor(LevelAssignmentLineData lineData)
    {
        Color color = GetBaseColor(lineData.VisualState);

        if (lineData.IsProcessCompleted)
        {
            color.a = CompletedAlpha;
            return color;
        }

        if (lineData.VisualState != ELevelTestConnectionVisualState.None)
        {
            color.a = ActiveAlpha;
            return color;
        }

        color.a = lineData.IsFocused ? FocusedAlpha : IdleAlpha;
        return color;
    }

    private float GetLineWidth(LevelAssignmentLineData lineData)
    {
        if (lineData.IsProcessCompleted)
        {
            return IdleLineWidth;
        }

        if (lineData.VisualState != ELevelTestConnectionVisualState.None)
        {
            return ActiveLineWidth;
        }

        return lineData.IsFocused ? FocusedLineWidth : IdleLineWidth;
    }

    private Color GetBaseColor(ELevelTestConnectionVisualState visualState)
    {
        switch (visualState)
        {
            case ELevelTestConnectionVisualState.Occupied:
                return OccupiedColor;

            case ELevelTestConnectionVisualState.Waiting:
            case ELevelTestConnectionVisualState.Requeued:
                return WaitingColor;

            case ELevelTestConnectionVisualState.Blocked:
                return BlockedColor;

            default:
                return LineColor;
        }
    }

    private float GetLabelAlpha(LevelAssignmentLineData lineData)
    {
        if (lineData.VisualState == ELevelTestConnectionVisualState.None && !lineData.IsFocused)
        {
            return 0.64f;
        }

        return 1f;
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

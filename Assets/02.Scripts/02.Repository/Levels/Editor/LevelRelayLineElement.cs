using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

internal sealed class LevelRelayLineElement : VisualElement
{
    private static readonly Color LinkColor = new Color(0.60f, 0.44f, 0.92f);
    private static readonly Color TransferColor = new Color(1.00f, 0.52f, 0.18f);
    private static readonly Color DraftColor = new Color(1.00f, 0.78f, 0.22f, 0.92f);
    private const float IdleAlpha = 0.26f;
    private const float FocusedAlpha = 0.92f;
    private const float NormalLineWidth = 3f;
    private const float HighlightLineWidth = 5f;
    private const float TransferLineWidthOffset = 1f;
    private const float ArrowLength = 12f;
    private const float ArrowHalfWidth = 5f;
    private const float ArrowStartPadding = 26f;
    private const float ArrowEndPadding = 28f;
    private const float ArrowSpacing = 30f;

    private readonly List<LevelRelayLineData> _lineDataList;

    public LevelRelayLineElement(IReadOnlyList<LevelRelayLineData> lineDataList, float width, float height)
    {
        pickingMode = PickingMode.Ignore;
        style.position = Position.Absolute;
        style.left = 0f;
        style.top = 0f;
        style.width = width;
        style.height = height;

        _lineDataList = new List<LevelRelayLineData>(lineDataList);
        generateVisualContent += DrawRelayLines;
    }

    private void DrawRelayLines(MeshGenerationContext context)
    {
        Painter2D painter = context.painter2D;

        for (int i = 0; i < _lineDataList.Count; i++)
        {
            LevelRelayLineData lineData = _lineDataList[i];
            Color color = GetLineColor(lineData);
            float width = lineData.IsHighlighted || lineData.IsDraft ? HighlightLineWidth : NormalLineWidth;

            if (lineData.RelayType == ERelayType.Transfer)
            {
                width += TransferLineWidthOffset;
            }

            painter.strokeColor = color;
            painter.lineWidth = width;
            painter.BeginPath();
            painter.MoveTo(lineData.StartPosition);
            painter.LineTo(lineData.EndPosition);
            painter.Stroke();

            if (lineData.RelayType == ERelayType.Transfer)
            {
                DrawTransferArrows(painter, lineData.StartPosition, lineData.EndPosition, color);
            }
        }
    }

    private void DrawTransferArrows(Painter2D painter, Vector2 startPosition, Vector2 endPosition, Color color)
    {
        Vector2 direction = endPosition - startPosition;
        float length = direction.magnitude;

        if (length <= ArrowStartPadding + ArrowEndPadding)
        {
            return;
        }

        direction /= length;
        int arrowCount = Mathf.Max(1, Mathf.FloorToInt((length - ArrowStartPadding - ArrowEndPadding) / ArrowSpacing) + 1);
        float usableLength = length - ArrowStartPadding - ArrowEndPadding;

        for (int i = 0; i < arrowCount; i++)
        {
            float t = arrowCount == 1 ? 0.5f : i / (float)(arrowCount - 1);
            Vector2 tip = startPosition + direction * (ArrowStartPadding + usableLength * t);
            DrawArrow(painter, tip, direction, color);
        }
    }

    private void DrawArrow(Painter2D painter, Vector2 tip, Vector2 direction, Color color)
    {
        Vector2 normal = new Vector2(-direction.y, direction.x);
        Vector2 basePosition = tip - direction * ArrowLength;
        Vector2 left = basePosition + normal * ArrowHalfWidth;
        Vector2 right = basePosition - normal * ArrowHalfWidth;

        painter.fillColor = color;
        painter.BeginPath();
        painter.MoveTo(tip);
        painter.LineTo(left);
        painter.LineTo(right);
        painter.ClosePath();
        painter.Fill();
    }

    private Color GetLineColor(LevelRelayLineData lineData)
    {
        if (lineData.IsDraft)
        {
            return DraftColor;
        }

        Color color = lineData.RelayType == ERelayType.Link ? LinkColor : TransferColor;
        color.a = lineData.IsHighlighted || lineData.IsFocused ? FocusedAlpha : IdleAlpha;
        return color;
    }
}

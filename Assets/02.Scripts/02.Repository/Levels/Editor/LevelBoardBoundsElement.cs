using UnityEngine;
using UnityEngine.UIElements;

internal sealed class LevelBoardBoundsElement : VisualElement
{
    private static readonly Color BoundsColor = new Color(1.0f, 0.78f, 0.22f, 0.95f);
    private static readonly Color GridLineColor = new Color(1.0f, 1.0f, 1.0f, 0.16f);
    private static readonly Color CenterPointColor = new Color(0.30f, 0.56f, 0.92f, 0.95f);
    private static readonly Color FillColor = new Color(0.18f, 0.20f, 0.23f, 0.26f);
    private const float CenterPointSize = 8f;

    public LevelBoardBoundsElement(int rowCount, int columnCount, float gridUnit)
    {
        pickingMode = PickingMode.Ignore;
        style.position = Position.Absolute;
        style.width = columnCount * gridUnit;
        style.height = rowCount * gridUnit;
        style.backgroundColor = new StyleColor(FillColor);
        SetBorder(this, 2f, BoundsColor);

        AddGridLines(rowCount, columnCount, gridUnit);
        AddCenterPoint(rowCount, columnCount, gridUnit);
    }

    private void AddGridLines(int rowCount, int columnCount, float gridUnit)
    {
        for (int column = 1; column < columnCount; column++)
        {
            AddLine(column * gridUnit, 0f, 1f, rowCount * gridUnit, GridLineColor);
        }

        for (int row = 1; row < rowCount; row++)
        {
            AddLine(0f, row * gridUnit, columnCount * gridUnit, 1f, GridLineColor);
        }
    }

    private void AddCenterPoint(int rowCount, int columnCount, float gridUnit)
    {
        float width = columnCount * gridUnit;
        float height = rowCount * gridUnit;

        VisualElement point = new VisualElement();
        point.pickingMode = PickingMode.Ignore;
        point.style.position = Position.Absolute;
        point.style.left = width * 0.5f - CenterPointSize * 0.5f;
        point.style.top = height * 0.5f - CenterPointSize * 0.5f;
        point.style.width = CenterPointSize;
        point.style.height = CenterPointSize;
        point.style.borderTopLeftRadius = CenterPointSize;
        point.style.borderTopRightRadius = CenterPointSize;
        point.style.borderBottomLeftRadius = CenterPointSize;
        point.style.borderBottomRightRadius = CenterPointSize;
        point.style.backgroundColor = new StyleColor(CenterPointColor);
        Add(point);
    }

    private void AddLine(float left, float top, float width, float height, Color color)
    {
        VisualElement line = new VisualElement();
        line.pickingMode = PickingMode.Ignore;
        line.style.position = Position.Absolute;
        line.style.left = left;
        line.style.top = top;
        line.style.width = width;
        line.style.height = height;
        line.style.backgroundColor = new StyleColor(color);
        Add(line);
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

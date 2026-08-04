using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public sealed class CanvasRoundedRectGraphic : MaskableGraphic
{
    private const int MinimumCornerSegments = 2;

    [SerializeField]
    private float _cornerRadius = 16f;

    [SerializeField]
    private int _cornerSegments = 6;

    public float CornerRadius
    {
        get => _cornerRadius;
        set
        {
            _cornerRadius = value;
            SetVerticesDirty();
        }
    }

    protected override void OnPopulateMesh(VertexHelper vertexHelper)
    {
        vertexHelper.Clear();

        Rect rect = GetPixelAdjustedRect();
        float radius = Mathf.Min(_cornerRadius, rect.width * 0.5f, rect.height * 0.5f);

        if (radius <= 0f)
        {
            AddRectangle(vertexHelper, rect);
            return;
        }

        int segments = Mathf.Max(MinimumCornerSegments, _cornerSegments);
        List<Vector2> pointList = new List<Vector2>(segments * 4 + 4);
        AddCornerPoints(pointList, new Vector2(rect.xMax - radius, rect.yMax - radius), radius, 0f, 90f, segments);
        AddCornerPoints(pointList, new Vector2(rect.xMin + radius, rect.yMax - radius), radius, 90f, 180f, segments);
        AddCornerPoints(pointList, new Vector2(rect.xMin + radius, rect.yMin + radius), radius, 180f, 270f, segments);
        AddCornerPoints(pointList, new Vector2(rect.xMax - radius, rect.yMin + radius), radius, 270f, 360f, segments);

        int centerIndex = 0;
        vertexHelper.AddVert(rect.center, color, Vector2.zero);

        for (int index = 0; index < pointList.Count; index++)
        {
            vertexHelper.AddVert(pointList[index], color, Vector2.zero);
        }

        for (int index = 0; index < pointList.Count; index++)
        {
            int nextIndex = index == pointList.Count - 1 ? 0 : index + 1;
            vertexHelper.AddTriangle(centerIndex, index + 1, nextIndex + 1);
        }
    }

    private void AddRectangle(VertexHelper vertexHelper, Rect rect)
    {
        vertexHelper.AddVert(new Vector2(rect.xMin, rect.yMin), color, Vector2.zero);
        vertexHelper.AddVert(new Vector2(rect.xMin, rect.yMax), color, Vector2.zero);
        vertexHelper.AddVert(new Vector2(rect.xMax, rect.yMax), color, Vector2.zero);
        vertexHelper.AddVert(new Vector2(rect.xMax, rect.yMin), color, Vector2.zero);
        vertexHelper.AddTriangle(0, 1, 2);
        vertexHelper.AddTriangle(2, 3, 0);
    }

    private static void AddCornerPoints(List<Vector2> pointList, Vector2 center, float radius, float startDegrees, float endDegrees, int segments)
    {
        for (int index = 0; index <= segments; index++)
        {
            float angle = Mathf.Lerp(startDegrees, endDegrees, index / (float)segments) * Mathf.Deg2Rad;
            pointList.Add(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
        }
    }
}

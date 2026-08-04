using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public sealed class CanvasDiscGraphic : MaskableGraphic
{
    private const int MinimumSegments = 12;

    [SerializeField]
    private int _segments = 32;

    [SerializeField]
    [Range(0f, 0.95f)]
    private float _innerRadiusRatio;

    public float InnerRadiusRatio
    {
        get => _innerRadiusRatio;
        set
        {
            _innerRadiusRatio = Mathf.Clamp(value, 0f, 0.95f);
            SetVerticesDirty();
        }
    }

    protected override void OnPopulateMesh(VertexHelper vertexHelper)
    {
        vertexHelper.Clear();

        Rect rect = GetPixelAdjustedRect();
        Vector2 center = rect.center;
        float outerRadius = Mathf.Min(rect.width, rect.height) * 0.5f;
        int segments = Mathf.Max(MinimumSegments, _segments);

        if (_innerRadiusRatio <= 0f)
        {
            AddFilledDisc(vertexHelper, center, outerRadius, segments);
            return;
        }

        AddRing(vertexHelper, center, outerRadius, outerRadius * _innerRadiusRatio, segments);
    }

    private void AddFilledDisc(VertexHelper vertexHelper, Vector2 center, float radius, int segments)
    {
        vertexHelper.AddVert(center, color, Vector2.zero);

        for (int index = 0; index < segments; index++)
        {
            vertexHelper.AddVert(GetPoint(center, radius, index, segments), color, Vector2.zero);
        }

        for (int index = 0; index < segments; index++)
        {
            int nextIndex = index == segments - 1 ? 0 : index + 1;
            vertexHelper.AddTriangle(0, index + 1, nextIndex + 1);
        }
    }

    private void AddRing(VertexHelper vertexHelper, Vector2 center, float outerRadius, float innerRadius, int segments)
    {
        for (int index = 0; index < segments; index++)
        {
            vertexHelper.AddVert(GetPoint(center, outerRadius, index, segments), color, Vector2.zero);
            vertexHelper.AddVert(GetPoint(center, innerRadius, index, segments), color, Vector2.zero);
        }

        for (int index = 0; index < segments; index++)
        {
            int nextIndex = index == segments - 1 ? 0 : index + 1;
            int outerIndex = index * 2;
            int innerIndex = outerIndex + 1;
            int nextOuterIndex = nextIndex * 2;
            int nextInnerIndex = nextOuterIndex + 1;
            vertexHelper.AddTriangle(outerIndex, nextOuterIndex, nextInnerIndex);
            vertexHelper.AddTriangle(nextInnerIndex, innerIndex, outerIndex);
        }
    }

    private static Vector2 GetPoint(Vector2 center, float radius, int index, int segments)
    {
        float angle = index / (float)segments * Mathf.PI * 2f;
        return center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
    }
}

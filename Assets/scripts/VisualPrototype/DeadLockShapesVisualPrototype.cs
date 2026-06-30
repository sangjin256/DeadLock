using Shapes;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteAlways]
public class DeadLockShapesVisualPrototype : ImmediateModeShapeDrawer
{
    [Header("Scene")]
    [SerializeField]
    private Camera _targetCamera;

    [SerializeField]
    private Color _backgroundColor = new Color(0.075f, 0.09f, 0.105f, 1f);

    [SerializeField]
    private float _animationSpeed = 1f;

    private readonly Color _blue = new Color(0.48f, 0.64f, 0.98f, 1f);
    private readonly Color _green = new Color(0.73f, 0.82f, 0.24f, 1f);
    private readonly Color _red = new Color(0.97f, 0.39f, 0.38f, 1f);
    private readonly Color _mint = new Color(0.22f, 0.88f, 0.76f, 1f);
    private readonly Color _amber = new Color(1f, 0.72f, 0.22f, 1f);
    private readonly Color _ink = new Color(0.055f, 0.07f, 0.085f, 1f);
    private readonly Color _panel = new Color(0.12f, 0.145f, 0.165f, 1f);
    private readonly Color _lineSoft = new Color(0.52f, 0.65f, 0.78f, 0.25f);
    private readonly Color _text = new Color(0.88f, 0.92f, 0.95f, 0.88f);

    private void Reset()
    {
        _targetCamera = Camera.main;
    }

    private void OnValidate()
    {
        if (_targetCamera == null)
        {
            _targetCamera = Camera.main;
        }
    }

    private void Update()
    {
        if (_targetCamera != null)
        {
            _targetCamera.backgroundColor = _backgroundColor;
        }
    }

    public override void DrawShapes(Camera cam)
    {
        if (_targetCamera != null && cam != _targetCamera)
        {
            return;
        }

        using (Draw.Command(cam))
        {
            Draw.ResetAllDrawStates();
            Draw.Matrix = transform.localToWorldMatrix;
            Draw.ZTest = CompareFunction.Always;
            Draw.BlendMode = ShapesBlendMode.Transparent;
            Draw.LineGeometry = LineGeometry.Flat2D;
            Draw.LineEndCaps = LineEndCap.Round;
            Draw.ThicknessSpace = ThicknessSpace.Meters;
            Draw.RadiusSpace = ThicknessSpace.Meters;

            float time = Application.isPlaying ? Time.time : (float)UnityEditorSafeTime();
            float pulse = 0.5f + 0.5f * Mathf.Sin(time * _animationSpeed * ShapesMath.TAU);

            DrawBackgroundGrid();
            DrawHeader();

            Vector2 processA = new Vector2(-4.7f, 1.8f);
            Vector2 processB = new Vector2(-3.5f, -2.1f);
            Vector2 basic = new Vector2(-0.8f, 2.05f);
            Vector2 capacity = new Vector2(2.35f, 1.55f);
            Vector2 simultaneous = new Vector2(4.65f, -0.35f);
            Vector2 colorSwitch = new Vector2(1.55f, -2.15f);
            Vector2 emptyColor = new Vector2(-0.8f, -1.05f);
            Vector2 clock = new Vector2(4.75f, 2.25f);

            DrawGelConnection(processA, basic, _green, pulse, 0.18f);
            DrawGelConnection(processA, emptyColor, _blue, pulse, 0.13f);
            DrawGelConnection(processB, colorSwitch, _red, 1f - pulse, 0.16f);
            DrawGelConnection(colorSwitch, simultaneous, _red, pulse, 0.12f);
            DrawGelConnection(capacity, clock, _amber, 1f - pulse, 0.1f);

            DrawProcess(processA, "P1", new[] { _green, _blue, _red }, pulse, true);
            DrawProcess(processB, "P2", new[] { _red, _green }, 1f - pulse, false);

            DrawBasicResource(basic, _green, pulse);
            DrawCapacityResource(capacity, _blue, 3, 2, pulse);
            DrawSimultaneousResource(simultaneous, _mint, pulse);
            DrawColorSwitchResource(colorSwitch, new[] { _red, _green, _blue }, pulse);
            DrawEmptyColorResource(emptyColor, pulse);
            DrawClockResource(clock, _amber, 3, pulse);
        }
    }

    private static double UnityEditorSafeTime()
    {
#if UNITY_EDITOR
        return UnityEditor.EditorApplication.timeSinceStartup;
#else
        return Time.time;
#endif
    }

    private void DrawBackgroundGrid()
    {
        Draw.Rectangle(Vector3.zero, new Vector2(12.4f, 7f), _backgroundColor);

        Draw.Thickness = 0.012f;
        for (int x = -6; x <= 6; x++)
        {
            Draw.Line(new Vector2(x, -3.25f), new Vector2(x, 3.25f), new Color(1f, 1f, 1f, 0.025f));
        }

        for (int y = -3; y <= 3; y++)
        {
            Draw.Line(new Vector2(-6.1f, y), new Vector2(6.1f, y), new Color(1f, 1f, 1f, 0.025f));
        }
    }

    private void DrawHeader()
    {
        Draw.FontSize = 0.22f;
        Draw.Text(new Vector2(-5.85f, 3.08f), "DeadLock Shapes Visual Prototype", TextAlign.Left, _text);

        Draw.FontSize = 0.13f;
        Draw.Text(new Vector2(-5.85f, 2.75f), "process / resource variants / gel-like connections", TextAlign.Left, new Color(_text.r, _text.g, _text.b, 0.6f));
    }

    private void DrawGelConnection(Vector2 start, Vector2 end, Color color, float pulse, float wave)
    {
        Vector2 delta = end - start;
        Vector2 direction = delta.normalized;
        Vector2 normal = new Vector2(-direction.y, direction.x);
        Vector2 center = (start + end) * 0.5f + normal * Mathf.Sin(pulse * ShapesMath.TAU) * wave;
        Vector2 a = Vector2.Lerp(start, center, 0.44f);
        Vector2 b = Vector2.Lerp(center, end, 0.44f);

        Color glow = WithAlpha(color, 0.18f);
        Color core = WithAlpha(color, 0.88f);

        Draw.BlendMode = ShapesBlendMode.Additive;
        Draw.Line(start, a, 0.36f, glow);
        Draw.Line(a, b, 0.44f, glow);
        Draw.Line(b, end, 0.36f, glow);
        Draw.Disc(center, 0.32f + 0.05f * pulse, DiscColors.Radial(WithAlpha(color, 0.28f), Color.clear));

        Draw.BlendMode = ShapesBlendMode.Transparent;
        Draw.Line(start, a, 0.13f, core);
        Draw.Line(a, b, 0.16f, core);
        Draw.Line(b, end, 0.13f, core);
        Draw.Disc(center, 0.17f + 0.035f * pulse, WithAlpha(color, 0.92f));
        Draw.Disc(Vector2.Lerp(start, end, 0.18f), 0.12f + 0.025f * pulse, WithAlpha(color, 0.9f));
        Draw.Disc(Vector2.Lerp(start, end, 0.82f), 0.12f + 0.025f * (1f - pulse), WithAlpha(color, 0.9f));
    }

    private void DrawProcess(Vector2 position, string label, Color[] requestedColors, float pulse, bool selected)
    {
        float outerRadius = selected ? 0.73f + pulse * 0.035f : 0.67f;

        Draw.BlendMode = ShapesBlendMode.Additive;
        Draw.Disc(position, outerRadius + 0.18f, DiscColors.Radial(WithAlpha(_mint, selected ? 0.18f : 0.08f), Color.clear));

        Draw.BlendMode = ShapesBlendMode.Transparent;
        Draw.Disc(position, outerRadius, new Color(0.94f, 0.965f, 0.975f, 1f));
        Draw.Ring(position, outerRadius + 0.03f, 0.055f, selected ? _mint : _lineSoft);
        Draw.Disc(position, 0.19f, _ink);

        Draw.FontSize = 0.18f;
        Draw.Text(position + new Vector2(-0.13f, -0.06f), label, TextAlign.Left, new Color(1f, 1f, 1f, 0.72f));

        int colorCount = requestedColors != null ? requestedColors.Length : 0;
        float start = -(colorCount - 1) * 0.21f;
        for (int i = 0; i < colorCount; i++)
        {
            Vector2 chipPosition = position + new Vector2(start + 0.42f * i, outerRadius + 0.38f);
            Draw.Disc(chipPosition, 0.15f, requestedColors[i]);
            Draw.Ring(chipPosition, 0.17f, 0.025f, WithAlpha(Color.white, 0.32f));
        }
    }

    private void DrawBasicResource(Vector2 position, Color color, float pulse)
    {
        DrawResourceShell(position, color, "Basic");
        Draw.Disc(position, 0.22f, _ink);
        Draw.Ring(position, 0.54f + pulse * 0.035f, 0.05f, WithAlpha(color, 0.58f));
    }

    private void DrawCapacityResource(Vector2 position, Color color, int capacity, int available, float pulse)
    {
        DrawResourceShell(position, color, "Capacity");

        float start = -(capacity - 1) * 0.18f;
        for (int i = 0; i < capacity; i++)
        {
            Color slotColor = i < available ? color : WithAlpha(color, 0.22f);
            Vector2 slot = position + new Vector2(start + i * 0.36f, -0.03f);
            Draw.Disc(slot, 0.13f + (i < available ? pulse * 0.01f : 0f), slotColor);
        }

        Draw.FontSize = 0.15f;
        Draw.Text(position + new Vector2(-0.2f, -0.46f), available + "/" + capacity, TextAlign.Left, WithAlpha(Color.white, 0.7f));
    }

    private void DrawSimultaneousResource(Vector2 position, Color color, float pulse)
    {
        DrawResourceShell(position, color, "Sync");

        Draw.Line(position + new Vector2(-0.33f, 0.04f), position + new Vector2(0.33f, 0.04f), 0.08f, color);
        Draw.Line(position + new Vector2(-0.21f, -0.22f), position + new Vector2(0.21f, 0.3f), 0.08f, color);
        Draw.Line(position + new Vector2(0.21f, -0.22f), position + new Vector2(-0.21f, 0.3f), 0.08f, color);
        Draw.Disc(position + new Vector2(-0.38f, 0.04f), 0.12f + pulse * 0.02f, color);
        Draw.Disc(position + new Vector2(0.38f, 0.04f), 0.12f + pulse * 0.02f, color);
        Draw.Disc(position + new Vector2(0f, 0.38f), 0.12f + pulse * 0.02f, color);
    }

    private void DrawColorSwitchResource(Vector2 position, Color[] colors, float pulse)
    {
        if (colors == null || colors.Length == 0)
        {
            return;
        }

        DrawResourceShell(position, colors[0], "Switch");

        for (int i = 0; i < colors.Length; i++)
        {
            float angle = ShapesMath.TAU * (i / (float)colors.Length) + pulse * 0.55f;
            Vector2 direction = ShapesMath.AngToDir(angle);
            Draw.Disc(position + direction * 0.32f, i == 0 ? 0.18f : 0.14f, colors[i]);
        }

        Draw.Arc(position, 0.48f, 0.045f, pulse * ShapesMath.TAU, pulse * ShapesMath.TAU + ShapesMath.TAU * 0.72f, ArcEndCap.Round, WithAlpha(Color.white, 0.48f));
    }

    private void DrawEmptyColorResource(Vector2 position, float pulse)
    {
        Color empty = new Color(0.86f, 0.9f, 0.94f, 1f);
        DrawResourceShell(position, empty, "Empty");

        Draw.BlendMode = ShapesBlendMode.Additive;
        Draw.Disc(position, 0.38f + pulse * 0.04f, DiscColors.Radial(WithAlpha(_blue, 0.22f), Color.clear));

        Draw.BlendMode = ShapesBlendMode.Transparent;
        Draw.Ring(position, 0.32f, 0.07f, WithAlpha(empty, 0.8f));
        Draw.Line(position + new Vector2(-0.18f, -0.18f), position + new Vector2(0.18f, 0.18f), 0.055f, WithAlpha(empty, 0.78f));
        Draw.Line(position + new Vector2(-0.18f, 0.18f), position + new Vector2(0.18f, -0.18f), 0.055f, WithAlpha(empty, 0.78f));
    }

    private void DrawClockResource(Vector2 position, Color color, int count, float pulse)
    {
        DrawResourceShell(position, color, "Clock");

        Draw.Ring(position, 0.36f, 0.07f, color);
        Draw.Line(position, position + new Vector2(0f, 0.23f), 0.06f, color);
        Draw.Line(position, position + ShapesMath.AngToDir(-0.25f * ShapesMath.TAU + pulse * 0.22f) * 0.28f, 0.05f, color);

        Draw.FontSize = 0.16f;
        Draw.Text(position + new Vector2(-0.08f, -0.61f), count.ToString(), TextAlign.Left, WithAlpha(Color.white, 0.72f));
    }

    private void DrawResourceShell(Vector2 position, Color color, string label)
    {
        Draw.BlendMode = ShapesBlendMode.Additive;
        Draw.Disc(position, 0.84f, DiscColors.Radial(WithAlpha(color, 0.18f), Color.clear));

        Draw.BlendMode = ShapesBlendMode.Transparent;
        Draw.Rectangle(position, new Vector2(1.1f, 1.1f), 0.18f, _panel);
        Draw.Rectangle(position, new Vector2(0.98f, 0.98f), 0.15f, WithAlpha(color, 0.92f));
        Draw.Rectangle(position + new Vector2(0f, -0.04f), new Vector2(0.78f, 0.78f), 0.16f, WithAlpha(_ink, 0.28f));

        Draw.FontSize = 0.14f;
        Draw.Text(position + new Vector2(-0.42f, 0.7f), label, TextAlign.Left, WithAlpha(_text, 0.82f));
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }
}

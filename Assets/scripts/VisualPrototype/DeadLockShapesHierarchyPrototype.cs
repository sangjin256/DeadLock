using Shapes;
using TMPro;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class DeadLockShapesHierarchyPrototype : MonoBehaviour
{
    private enum RelayVisualType
    {
        Link,
        Transfer
    }

    [Header("Scene")]
    [SerializeField]
    private Camera _targetCamera;

    [SerializeField]
    private bool _rebuild;

    private readonly Color _blue = new Color(0.42f, 0.61f, 0.95f, 1f);
    private readonly Color _green = new Color(0.72f, 0.82f, 0.2f, 1f);
    private readonly Color _red = new Color(0.93f, 0.36f, 0.36f, 1f);
    private readonly Color _mint = new Color(0.18f, 0.78f, 0.68f, 1f);
    private readonly Color _amber = new Color(0.98f, 0.68f, 0.2f, 1f);
    private readonly Color _background = new Color(0.07f, 0.085f, 0.1f, 1f);
    private readonly Color _ink = new Color(0.055f, 0.065f, 0.075f, 1f);
    private readonly Color _stationFill = new Color(0.91f, 0.94f, 0.95f, 1f);
    private readonly Color _stationMuted = new Color(0.72f, 0.78f, 0.8f, 1f);
    private readonly Color _text = new Color(0.86f, 0.9f, 0.92f, 0.86f);

    private const string GeneratedRootName = "Generated Metro Shapes Hierarchy";
    private const string LegacyGeneratedRootName = "Generated Shapes Hierarchy";
    private const float MetroLineThickness = 0.15f;
    private const float RelayStubLength = 0.6f;

    [System.NonSerialized]
    private bool _isBuildQueued;

    [System.NonSerialized]
    private bool _forceQueuedBuild;

    private void Reset()
    {
        _targetCamera = Camera.main;
    }

    private void OnEnable()
    {
        QueueBuild(force: false);
    }

    private void OnValidate()
    {
        if (_targetCamera == null)
        {
            _targetCamera = Camera.main;
        }

        if (_rebuild)
        {
            _rebuild = false;
            QueueBuild(force: true);
        }
    }

    private void QueueBuild(bool force)
    {
        _forceQueuedBuild |= force;

#if UNITY_EDITOR
        if (Application.isPlaying)
        {
            ExecuteQueuedBuild();
            return;
        }

        if (_isBuildQueued)
        {
            return;
        }

        _isBuildQueued = true;
        EditorApplication.delayCall += ExecuteQueuedBuild;
#else
        ExecuteQueuedBuild();
#endif
    }

    private void ExecuteQueuedBuild()
    {
        _isBuildQueued = false;

        if (this == null)
        {
            return;
        }

        if (_forceQueuedBuild)
        {
            _forceQueuedBuild = false;
            Build();
            return;
        }

        BuildIfNeeded();
    }

    private void BuildIfNeeded()
    {
        bool hasGeneratedRoot = CountChildren(GeneratedRootName) > 0;
        bool hasLegacyRoot = CountChildren(LegacyGeneratedRootName) > 0;

        if (hasGeneratedRoot && hasLegacyRoot == false && CountChildren(GeneratedRootName) == 1)
        {
            return;
        }

        Build();
    }

    private void Build()
    {
        if (_targetCamera != null)
        {
            _targetCamera.backgroundColor = _background;
        }

        RemoveGeneratedRoots();

        Transform generatedRoot = CreateGroup(GeneratedRootName, transform);
        Transform labels = CreateGroup("Labels", generatedRoot);
        Transform connections = CreateGroup("Metro Connections", generatedRoot);
        Transform relayLinks = CreateGroup("Relay Links", generatedRoot);
        Transform processes = CreateGroup("Process Stations", generatedRoot);
        Transform resources = CreateGroup("Resource Stations", generatedRoot);

        Vector2 processA = new Vector2(-4.8f, 1.65f);
        Vector2 processB = new Vector2(-3.6f, -2.1f);
        Vector2 basic = new Vector2(-1.15f, 1.8f);
        Vector2 capacity = new Vector2(2.05f, 1.4f);
        Vector2 simultaneous = new Vector2(4.45f, -0.55f);
        Vector2 colorSwitch = new Vector2(1.3f, -2.1f);
        Vector2 emptyColor = new Vector2(-0.9f, -0.8f);
        Vector2 clock = new Vector2(4.75f, 2.0f);

        CreateLabel("Title", "DeadLock Metro Visual Prototype", new Vector2(-5.85f, 3.08f), 0.24f, labels, TextAlignmentOptions.Left);
        CreateLabel("Subtitle", "uniform lines / compact resources / hierarchy-editable Shapes", new Vector2(-5.85f, 2.75f), 0.14f, labels, TextAlignmentOptions.Left);

        CreateMetroConnection("Green_ProcessA_Basic", _green, connections, processA, basic);
        CreateMetroConnection("Blue_ProcessA_Empty", _blue, connections, processA, emptyColor);
        CreateMetroConnection("Red_ProcessB_Switch", _red, connections, processB, colorSwitch);
        CreateMetroConnection("Red_Switch_Sync", _red, connections, colorSwitch, simultaneous);
        CreateMetroConnection("Amber_Capacity_Clock", _amber, connections, capacity, clock);

        CreateRelayLink("RelayLink_Basic_ColorSwitch", basic, colorSwitch, RelayVisualType.Link, relayLinks);
        CreateRelayLink("RelayTransfer_Capacity_Simultaneous", capacity, simultaneous, RelayVisualType.Transfer, relayLinks);

        CreateProcess("Process_Selected_P1", processA, "P1", new[] { _green, _blue, _red }, true, processes);
        CreateProcess("Process_P2", processB, "P2", new[] { _red, _green }, false, processes);

        CreateBasicResource("Resource_Basic", basic, _green, resources);
        CreateCapacityResource("Resource_Capacity_2_of_3", capacity, _blue, 3, 2, resources);
        CreateSimultaneousResource("Resource_Simultaneous_2_of_3", simultaneous, _mint, 3, 2, resources);
        CreateColorSwitchResource("Resource_ColorSwitch", colorSwitch, new[] { _red, _green, _blue }, resources);
        CreateEmptyColorResource("Resource_EmptyColor", emptyColor, resources);
        CreateClockResource("Resource_Clock_3", clock, _amber, 3, resources);
    }

    private void RemoveGeneratedRoots()
    {
        RemoveChild(GeneratedRootName);
        RemoveChild(LegacyGeneratedRootName);
    }

    private void RemoveChild(string childName)
    {
        while (true)
        {
            Transform existing = transform.Find(childName);
            if (existing == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(existing.gameObject);
                return;
            }

            DestroyImmediate(existing.gameObject);
        }
    }

    private int CountChildren(string childName)
    {
        int count = 0;
        for (int i = 0; i < transform.childCount; i++)
        {
            if (transform.GetChild(i).name == childName)
            {
                count++;
            }
        }

        return count;
    }

    private void CreateMetroConnection(string name, Color color, Transform parent, params Vector2[] points)
    {
        Transform root = CreateGroup("Line_" + name, parent);

        for (int i = 0; i < points.Length - 1; i++)
        {
            Vector2 start = points[i];
            Vector2 end = points[i + 1];
            CreateLine("Shadow_" + i, start, end, MetroLineThickness + 0.08f, WithAlpha(Color.black, 0.34f), root, 0);
            CreateLine("Track_" + i, start, end, MetroLineThickness, color, root, 2);
        }
    }

    private void CreateRelayLink(string name, Vector2 start, Vector2 end, RelayVisualType type, Transform parent)
    {
        Transform root = CreateGroup(name, parent);
        Vector2 direction = (end - start).normalized;
        float length = Vector2.Distance(start, end);
        float nodePadding = 0.52f;
        float stubLength = Mathf.Min(RelayStubLength, Mathf.Max(0f, (length - nodePadding * 2f) * 0.5f));
        Vector2 visibleStart = start + direction * nodePadding;
        Vector2 visibleEnd = end - direction * nodePadding;
        Vector2 startStubEnd = visibleStart + direction * stubLength;
        Vector2 endStubEnd = visibleEnd - direction * stubLength;

        CreateRelayStubLine("StartStub", visibleStart, startStubEnd, root);
        CreateRelayStubLine("EndStub", visibleEnd, endStubEnd, root);
        CreateRelayEndpointPlaceholders(type, visibleStart, startStubEnd, endStubEnd, direction, root);
    }

    private void CreateRelayStubLine(string name, Vector2 start, Vector2 end, Transform parent)
    {
        Color relayColor = new Color(0.08f, 0.09f, 0.1f, 1f);
        CreateLine(name, start, end, 0.14f, WithAlpha(relayColor, 0.45f), parent, 1);
    }

    private void CreateRelayEndpointPlaceholders(RelayVisualType type, Vector2 startStubStart, Vector2 startStubEnd, Vector2 endStubEnd, Vector2 direction, Transform parent)
    {
        Color relayColor = new Color(0.08f, 0.09f, 0.1f, 1f);

        if (type == RelayVisualType.Link)
        {
            CreateRing("LinkStartPlaceholder", startStubEnd, 0.12f, 0.035f, WithAlpha(relayColor, 0.68f), parent, 4);
            CreateRing("LinkEndPlaceholder", endStubEnd, 0.12f, 0.035f, WithAlpha(relayColor, 0.68f), parent, 4);
            return;
        }

        CreateDisc("TransferSenderPlaceholder", startStubEnd, 0.095f, WithAlpha(relayColor, 0.62f), parent, 4);
        CreateRing("TransferReceiverPlaceholder", endStubEnd, 0.12f, 0.035f, WithAlpha(relayColor, 0.68f), parent, 4);
        CreateDisc("TransferDirectionPlaceholder", Vector2.Lerp(startStubStart, startStubEnd, 0.62f), 0.055f, WithAlpha(relayColor, 0.5f), parent, 4);
    }

    private void CreateProcess(string name, Vector2 position, string label, Color[] requestedColors, bool selected, Transform parent)
    {
        Transform root = CreateGroup(name, parent);
        root.localPosition = ToVector3(position);

        CreateDisc("ProcessShadow", new Vector2(0.07f, -0.07f), 0.7f, WithAlpha(Color.black, 0.26f), root, 5);
        CreateDisc("StationStroke", Vector2.zero, 0.68f, selected ? _mint : WithAlpha(_stationMuted, 0.72f), root, 20);
        CreateDisc("StationFill", Vector2.zero, 0.6f, _stationFill, root, 22);
        CreateDisc("ProcessPort", Vector2.zero, 0.16f, _ink, root, 24);
        CreateLabel("Label", label, new Vector2(0f, -0.04f), 0.16f, root, TextAlignmentOptions.Center);

        Transform tray = CreateGroup("RequiredResourceTray", root);
        tray.localPosition = ToVector3(new Vector2(0f, 0.88f));

        float trayWidth = Mathf.Max(0.46f, 0.28f + requestedColors.Length * 0.28f);
        CreateRectangle("TrayShadow", new Vector2(0.035f, -0.035f), new Vector2(trayWidth, 0.32f), 0.16f, WithAlpha(Color.black, 0.28f), tray, 25);
        CreateRectangle("TrayBackground", Vector2.zero, new Vector2(trayWidth, 0.32f), 0.16f, WithAlpha(_ink, 0.78f), tray, 26);

        float start = -(requestedColors.Length - 1) * 0.16f;
        for (int i = 0; i < requestedColors.Length; i++)
        {
            Vector2 chipPosition = new Vector2(start + 0.32f * i, 0f);
            CreateDisc("RequestRim_" + i, chipPosition, 0.125f, WithAlpha(Color.white, 0.82f), tray, 27);
            CreateDisc("Request_" + i, chipPosition, 0.1f, requestedColors[i], tray, 28);
        }
    }

    private void CreateBasicResource(string name, Vector2 position, Color color, Transform parent)
    {
        Transform root = CreateResourceShell(name, position, color, parent);
        CreateDisc("Port", Vector2.zero, 0.16f, _ink, root, 24);
        CreateLabel("TypeLabel", "Basic", new Vector2(0f, -0.76f), 0.12f, root, TextAlignmentOptions.Center);
    }

    private void CreateCapacityResource(string name, Vector2 position, Color color, int capacity, int available, Transform parent)
    {
        Transform root = CreateResourceShell(name, position, color, parent);

        float start = -(capacity - 1) * 0.18f;
        for (int i = 0; i < capacity; i++)
        {
            Color slotColor = i < available ? color : WithAlpha(_stationMuted, 0.42f);
            CreateDisc("CapacitySlot_" + i, new Vector2(start + i * 0.36f, 0f), 0.11f, slotColor, root, 24);
        }

        CreateLabel("Count", available + "/" + capacity, new Vector2(0f, -0.72f), 0.12f, root, TextAlignmentOptions.Center);
    }

    private void CreateSimultaneousResource(string name, Vector2 position, Color color, int requiredCount, int activeCount, Transform parent)
    {
        Transform root = CreateResourceShell(name, position, color, parent);
        Vector2 hub = Vector2.zero;
        Vector2[] slots =
        {
            new Vector2(-0.26f, 0.2f),
            new Vector2(-0.28f, -0.16f),
            new Vector2(0.27f, -0.03f)
        };

        for (int i = 0; i < requiredCount && i < slots.Length; i++)
        {
            CreateLine("SimLink_" + i, slots[i], hub, 0.04f, WithAlpha(color, 0.62f), root, 23);
        }

        for (int i = 0; i < requiredCount && i < slots.Length; i++)
        {
            Color slotColor = i < activeCount ? color : WithAlpha(_stationMuted, 0.42f);
            CreateDisc("SimSlot_" + i, slots[i], 0.105f, slotColor, root, 24);
        }

        bool isActive = activeCount >= requiredCount;
        CreateDisc("ActivationLight", hub, 0.12f, isActive ? _green : _red, root, 26);
        CreateLabel("Count", activeCount + "/" + requiredCount, new Vector2(0f, -0.72f), 0.12f, root, TextAlignmentOptions.Center);
    }

    private void CreateColorSwitchResource(string name, Vector2 position, Color[] colors, Transform parent)
    {
        Transform root = CreateResourceShell(name, position, colors[0], parent);

        for (int i = 0; i < colors.Length; i++)
        {
            float angle = ShapesMath.TAU * (i / (float)colors.Length) + ShapesMath.TAU / 4f;
            Vector2 direction = ShapesMath.AngToDir(angle);
            CreateDisc("SwitchColor_" + i, direction * 0.23f, 0.095f, colors[i], root, 24);
        }

        CreateLabel("TypeLabel", "Switch", new Vector2(0f, -0.76f), 0.12f, root, TextAlignmentOptions.Center);
    }

    private void CreateEmptyColorResource(string name, Vector2 position, Transform parent)
    {
        Transform root = CreateResourceShell(name, position, _stationMuted, parent);
        CreateLine("EmptySlashA", new Vector2(-0.19f, -0.19f), new Vector2(0.19f, 0.19f), 0.06f, _stationMuted, root, 24);
        CreateLine("EmptySlashB", new Vector2(-0.19f, 0.19f), new Vector2(0.19f, -0.19f), 0.06f, _stationMuted, root, 24);
        CreateLabel("TypeLabel", "Empty", new Vector2(0f, -0.76f), 0.12f, root, TextAlignmentOptions.Center);
    }

    private void CreateClockResource(string name, Vector2 position, Color color, int count, Transform parent)
    {
        Transform root = CreateResourceShell(name, position, color, parent);
        CreateRing("ClockFace", Vector2.zero, 0.31f, 0.035f, WithAlpha(color, 0.48f), root, 24);
        CreateLine("ClockMinuteHand", Vector2.zero, new Vector2(0f, 0.22f), 0.035f, WithAlpha(color, 0.32f), root, 25);
        CreateLine("ClockHourHand", Vector2.zero, new Vector2(0.16f, -0.1f), 0.035f, WithAlpha(color, 0.32f), root, 25);
        CreateLabel("TurnCount", count.ToString(), new Vector2(0f, -0.07f), 0.38f, root, TextAlignmentOptions.Center, _ink);
    }

    private Transform CreateResourceShell(string name, Vector2 position, Color color, Transform parent)
    {
        Transform root = CreateGroup(name, parent);
        root.localPosition = ToVector3(position);
        CreateRectangle("ResourceShadow", new Vector2(0.07f, -0.07f), new Vector2(1.02f, 1.02f), 0.21f, WithAlpha(Color.black, 0.25f), root, 5);
        CreateRectangle("ResourceStroke", Vector2.zero, new Vector2(0.98f, 0.98f), 0.2f, color, root, 20);
        CreateRectangle("ResourceFill", Vector2.zero, new Vector2(0.83f, 0.83f), 0.17f, _stationFill, root, 22);
        return root;
    }

    private Disc CreateDisc(string name, Vector2 localPosition, float radius, Color color, Transform parent, int sortingOrder)
    {
        GameObject go = CreateObject(name, parent);
        go.transform.localPosition = ToVector3(localPosition);

        Disc disc = go.AddComponent<Disc>();
        disc.Type = DiscType.Disc;
        disc.Geometry = DiscGeometry.Flat2D;
        disc.Radius = radius;
        disc.Color = color;
        disc.SortingOrder = sortingOrder;
        return disc;
    }

    private Disc CreateRing(string name, Vector2 localPosition, float radius, float thickness, Color color, Transform parent, int sortingOrder)
    {
        GameObject go = CreateObject(name, parent);
        go.transform.localPosition = ToVector3(localPosition);

        Disc disc = go.AddComponent<Disc>();
        disc.Type = DiscType.Ring;
        disc.Geometry = DiscGeometry.Flat2D;
        disc.Radius = radius;
        disc.Thickness = thickness;
        disc.Color = color;
        disc.SortingOrder = sortingOrder;
        return disc;
    }

    private Shapes.Rectangle CreateRectangle(string name, Vector2 localPosition, Vector2 size, float cornerRadius, Color color, Transform parent, int sortingOrder)
    {
        GameObject go = CreateObject(name, parent);
        go.transform.localPosition = ToVector3(localPosition);

        Shapes.Rectangle rectangle = go.AddComponent<Shapes.Rectangle>();
        rectangle.Type = Shapes.Rectangle.RectangleType.RoundedSolid;
        rectangle.Width = size.x;
        rectangle.Height = size.y;
        rectangle.CornerRadius = cornerRadius;
        rectangle.Color = color;
        rectangle.SortingOrder = sortingOrder;
        return rectangle;
    }

    private Line CreateLine(string name, Vector2 start, Vector2 end, float thickness, Color color, Transform parent, int sortingOrder)
    {
        GameObject go = CreateObject(name, parent);

        Line line = go.AddComponent<Line>();
        line.Geometry = LineGeometry.Flat2D;
        line.EndCaps = LineEndCap.Round;
        line.Start = ToVector3(start);
        line.End = ToVector3(end);
        line.Thickness = thickness;
        line.Color = color;
        line.SortingOrder = sortingOrder;
        return line;
    }

    private TextMeshPro CreateLabel(string name, string text, Vector2 localPosition, float fontSize, Transform parent, TextAlignmentOptions alignment)
    {
        return CreateLabel(name, text, localPosition, fontSize, parent, alignment, _text);
    }

    private TextMeshPro CreateLabel(string name, string text, Vector2 localPosition, float fontSize, Transform parent, TextAlignmentOptions alignment, Color color)
    {
        GameObject go = CreateObject(name, parent);
        go.transform.localPosition = ToVector3(localPosition, -0.01f);

        TextMeshPro label = go.AddComponent<TextMeshPro>();
        label.text = text;
        label.fontSize = fontSize;
        label.color = color;
        label.alignment = alignment;
        label.enableWordWrapping = false;
        label.sortingOrder = 40;
        label.rectTransform.sizeDelta = new Vector2(3f, 0.5f);
        return label;
    }

    private Transform CreateGroup(string name, Transform parent)
    {
        return CreateObject(name, parent).transform;
    }

    private GameObject CreateObject(string name, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;
        return go;
    }

    private static Vector3 ToVector3(Vector2 value, float z = 0f)
    {
        return new Vector3(value.x, value.y, z);
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }
}

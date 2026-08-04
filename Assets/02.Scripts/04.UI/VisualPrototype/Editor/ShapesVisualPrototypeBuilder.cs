using Shapes;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ShapesVisualPrototypeBuilder
{
    private const string MenuPath = "Tools/DeadLock/Visuals/Rebuild Shapes Visual Prototype";
    private const string PrototypeScenePath = "Assets/Outdated/Scenes/VisualPrototype_Shapes.unity";
    private const string VisualPrototypeRootName = "Visual Prototype Root";
    private const string GeneratedRootName = "Generated Shapes State Reference";
    private const string LegacyGeneratedRootName = "Generated Metro Shapes Hierarchy V3";
    private const float ResourceSize = 0.98f;
    private const float ResourceFillSize = 0.83f;
    private const float ConnectionThickness = 0.15f;

    private static readonly Color Blue = new Color(0.42f, 0.61f, 0.95f, 1f);
    private static readonly Color Green = new Color(0.72f, 0.82f, 0.20f, 1f);
    private static readonly Color Red = new Color(0.93f, 0.36f, 0.36f, 1f);
    private static readonly Color Mint = new Color(0.18f, 0.78f, 0.68f, 1f);
    private static readonly Color Amber = new Color(0.98f, 0.68f, 0.20f, 1f);
    private static readonly Color Ink = new Color(0.055f, 0.065f, 0.075f, 1f);
    private static readonly Color StationFill = new Color(0.91f, 0.94f, 0.95f, 1f);
    private static readonly Color Muted = new Color(0.72f, 0.78f, 0.80f, 1f);

    [MenuItem(MenuPath)]
    public static void RebuildShapesVisualPrototype()
    {
        Scene activeScene = SceneManager.GetActiveScene();

        if (activeScene.path != PrototypeScenePath)
        {
            Debug.LogError("[ShapesVisualPrototypeBuilder] Open VisualPrototype_Shapes before rebuilding the state reference.");
            return;
        }

        if (Application.isPlaying)
        {
            Debug.LogError("[ShapesVisualPrototypeBuilder] Rebuild the state reference while the Editor is not playing.");
            return;
        }

        GameObject visualPrototypeRoot = GameObject.Find(VisualPrototypeRootName);

        if (visualPrototypeRoot == null)
        {
            Debug.LogError("[ShapesVisualPrototypeBuilder] Visual Prototype Root was not found in the active scene.");
            return;
        }

        RemoveGeneratedRoot(visualPrototypeRoot.transform, GeneratedRootName);
        RemoveGeneratedRoot(visualPrototypeRoot.transform, LegacyGeneratedRootName);

        Transform generatedRoot = CreateGroup(GeneratedRootName, visualPrototypeRoot.transform);
        BuildProcessStateSamples(generatedRoot);
        BuildCapacityStateMatrix(generatedRoot);
        BuildRuleCombinationSamples(generatedRoot);
        BuildConnectionAndRelaySamples(generatedRoot);
        ConfigurePrototypeCamera();

        Selection.activeGameObject = generatedRoot.gameObject;
        EditorSceneManager.MarkSceneDirty(activeScene);
    }

    private static void BuildProcessStateSamples(Transform parent)
    {
        Transform group = CreateGroup("01_Process_State_Samples", parent);
        CreateSectionLabel("Process States", new Vector2(-8f, 7.2f), group);

        CreateProcessSample("Process_Default", new Vector2(-11f, 5.7f), new[] { Green, Blue, Red }, EProcessSampleState.Default, group);
        CreateProcessSample("Process_Selected", new Vector2(-7.5f, 5.7f), new[] { Green, Blue, Red }, EProcessSampleState.Selected, group);
        CreateProcessSample("Process_Waiting", new Vector2(-4f, 5.7f), new[] { Red, Green }, EProcessSampleState.Waiting, group);
        CreateProcessSample("Process_Completed", new Vector2(-0.5f, 5.7f), new[] { Amber, Mint }, EProcessSampleState.Completed, group);
    }

    private static void BuildCapacityStateMatrix(Transform parent)
    {
        Transform group = CreateGroup("02_Capacity_State_Matrix", parent);
        CreateSectionLabel("Capacity Occupancy", new Vector2(-5.75f, 4.55f), group);

        CreateCapacityColumn(group, 1, -11f);
        CreateCapacityColumn(group, 2, -7.5f);
        CreateCapacityColumn(group, 3, -3.5f);
        CreateCapacityColumn(group, 4, 1f);
    }

    private static void BuildRuleCombinationSamples(Transform parent)
    {
        Transform group = CreateGroup("03_Rule_Combination_Samples", parent);
        CreateSectionLabel("Rule Combinations", new Vector2(7.5f, 7.2f), group);

        CreateClockResource("Clock_Capacity_2", new Vector2(5f, 5.7f), Amber, 2, 1, 4, true, group);
        CreateClockResource("Clock_Capacity_3", new Vector2(8.5f, 5.7f), Amber, 3, 2, 3, true, group);
        CreateClockResource("Clock_Capacity_4_Closed", new Vector2(12f, 5.7f), Amber, 4, 4, 0, false, group);

        CreateColorSwitchResource("ColorSwitch_Capacity_2", new Vector2(5f, 2.7f), new[] { Red, Green, Blue }, 1, 2, 1, group);
        CreateEmptyColorResource("EmptyColor_Capacity_2_Unfixed", new Vector2(8.5f, 2.7f), Muted, 2, 0, false, group);
        CreateEmptyColorResource("EmptyColor_Capacity_2_Fixed", new Vector2(12f, 2.7f), Mint, 2, 1, true, group);

        CreateSimultaneousResource("Simultaneous_Capacity_3_Partial", new Vector2(5f, -0.7f), Mint, 3, 2, group);
        CreateSimultaneousResource("Simultaneous_Capacity_3_Full", new Vector2(8.5f, -0.7f), Mint, 3, 3, group);
        CreateSimultaneousClockResource("SimultaneousClock_Capacity_3", new Vector2(12f, -0.7f), Mint, 3, 2, 2, group);
        CreateColorSwitchClockResource("ColorSwitchClock_Capacity_2", new Vector2(12f, -3.5f), new[] { Red, Green, Blue }, 2, 2, 1, 3, true, group);
    }

    private static void BuildConnectionAndRelaySamples(Transform parent)
    {
        Transform group = CreateGroup("04_Connection_And_Relay_Samples", parent);
        CreateSectionLabel("Connection And Relay", new Vector2(0f, -3.2f), group);

        Vector2 processPosition = new Vector2(-6.5f, -5.25f);
        Vector2 resourcePosition = new Vector2(-3.5f, -5.25f);
        CreateMetroConnection("Occupied_Connection", Mint, group, processPosition, resourcePosition);
        CreateProcessSample("Process_Connection", processPosition, new[] { Mint }, EProcessSampleState.Default, group);
        CreateBasicResource("Resource_Connection", resourcePosition, Mint, group);

        Vector2 linkStart = new Vector2(0.5f, -5.25f);
        Vector2 linkEnd = new Vector2(3.75f, -5.25f);
        CreateBasicResource("Resource_RelayLink_A", linkStart, Blue, group);
        CreateColorSwitchResource("Resource_RelayLink_B", linkEnd, new[] { Blue, Green, Amber }, 0, 1, 0, group);
        CreateRelay("RelayLink", linkStart, linkEnd, ERelayVisualType.Link, group);

        Vector2 transferStart = new Vector2(7f, -5.25f);
        Vector2 transferEnd = new Vector2(10.25f, -5.25f);
        CreateCapacityResource("Resource_RelayTransfer_Sender", transferStart, Red, 1, 1, group);
        CreateSimultaneousResource("Resource_RelayTransfer_Receiver", transferEnd, Red, 2, 1, group);
        CreateRelay("RelayTransfer", transferStart, transferEnd, ERelayVisualType.Transfer, group);
    }

    private static void CreateCapacityColumn(Transform parent, int capacity, float x)
    {
        Transform group = CreateGroup("Capacity_" + capacity, parent);
        CreateLabel("CapacityLabel", "C" + capacity, new Vector2(x, 3.85f), 3f, Muted, group, 60);

        for (int occupiedCount = 0; occupiedCount <= capacity; occupiedCount++)
        {
            float y = 2.9f - occupiedCount * 1.55f;
            string name = "Capacity_" + capacity + "_Occupied_" + occupiedCount;
            CreateCapacityResource(name, new Vector2(x, y), Blue, capacity, occupiedCount, group);
            CreateLabel("StateLabel", occupiedCount + "/" + capacity, new Vector2(x, y - 0.73f), 2f, WithAlpha(Muted, 0.88f), group, 60);
        }
    }

    private static void CreateProcessSample(string name,
                                            Vector2 position,
                                            Color[] requestedColorArray,
                                            EProcessSampleState state,
                                            Transform parent)
    {
        Transform root = CreateGroup(name, parent);
        root.localPosition = ToVector3(position);
        Transform background = CreateGroup("Background", root);
        Transform port = CreateGroup("Port", root);
        Transform tray = CreateGroup("RequiredColorTray", root);
        Transform stateGroup = CreateGroup("State", root);

        Color strokeColor = state == EProcessSampleState.Selected ? Mint : WithAlpha(Muted, 0.72f);
        Color fillColor = state == EProcessSampleState.Completed ? WithAlpha(StationFill, 0.44f) : StationFill;
        CreateDisc("Shadow", new Vector2(0.07f, -0.07f), 0.70f, WithAlpha(Color.black, 0.26f), background, 5);
        CreateDisc("Stroke", Vector2.zero, 0.68f, strokeColor, background, 20);
        CreateDisc("Fill", Vector2.zero, 0.60f, fillColor, background, 22);

        float portRadius = state == EProcessSampleState.Waiting ? 0.19f : 0.16f;
        CreateDisc("Disc", Vector2.zero, portRadius, state == EProcessSampleState.Completed ? WithAlpha(Ink, 0.42f) : Ink, port, 24);

        if (state == EProcessSampleState.Waiting)
        {
            CreateRing("WaitingPulse", Vector2.zero, 0.27f, 0.025f, WithAlpha(Amber, 0.72f), stateGroup, 25);
        }

        if (state == EProcessSampleState.Completed)
        {
            CreateRing("CompletedRing", Vector2.zero, 0.48f, 0.035f, WithAlpha(Mint, 0.72f), stateGroup, 25);
        }

        CreateRequiredColorTray(requestedColorArray, tray);
        CreateLabel("StateLabel", GetProcessStateLabel(state), new Vector2(0f, -0.93f), 2f, WithAlpha(Muted, 0.9f), root, 60);
    }

    private static void CreateRequiredColorTray(Color[] requestedColorArray, Transform parent)
    {
        parent.localPosition = ToVector3(new Vector2(0f, 0.88f));
        Transform background = CreateGroup("Background", parent);
        Transform slots = CreateGroup("Slots", parent);
        int colorCount = requestedColorArray != null ? requestedColorArray.Length : 0;
        float trayWidth = Mathf.Max(0.46f, 0.28f + colorCount * 0.28f);

        CreateRectangle("Shadow", new Vector2(0.035f, -0.035f), new Vector2(trayWidth, 0.32f), 0.16f, WithAlpha(Color.black, 0.28f), background, 25);
        CreateRectangle("Fill", Vector2.zero, new Vector2(trayWidth, 0.32f), 0.16f, WithAlpha(Ink, 0.78f), background, 26);

        float start = -(colorCount - 1) * 0.16f;

        for (int index = 0; index < colorCount; index++)
        {
            Transform slot = CreateGroup("Slot_" + index, slots);
            slot.localPosition = ToVector3(new Vector2(start + 0.32f * index, 0f));
            CreateDisc("Rim", Vector2.zero, 0.125f, WithAlpha(Color.white, 0.82f), slot, 27);
            CreateDisc("Fill", Vector2.zero, 0.10f, requestedColorArray[index], slot, 28);
        }
    }

    private static void CreateBasicResource(string name, Vector2 position, Color color, Transform parent)
    {
        Transform root = CreateResourceShell(name, position, color, parent);
        Transform port = CreateGroup("Port", root);
        CreateDisc("Disc", Vector2.zero, 0.16f, Ink, port, 24);
    }

    private static void CreateCapacityResource(string name,
                                               Vector2 position,
                                               Color color,
                                               int capacity,
                                               int occupiedCount,
                                               Transform parent)
    {
        Transform root = CreateResourceShell(name, position, color, parent);
        CreateOccupancySlots(root, color, capacity, occupiedCount, false);
    }

    private static void CreateClockResource(string name,
                                            Vector2 position,
                                            Color color,
                                            int capacity,
                                            int occupiedCount,
                                            int remainingRoundCount,
                                            bool isOpen,
                                            Transform parent)
    {
        Transform root = CreateResourceShell(name, position, isOpen ? color : WithAlpha(color, 0.42f), parent);
        CreateOccupancySlots(root, color, capacity, occupiedCount, false);
        CreateClockBadge(root, color, remainingRoundCount, isOpen);
    }

    private static void CreateColorSwitchResource(string name,
                                                  Vector2 position,
                                                  Color[] colorArray,
                                                  int currentColorIndex,
                                                  int capacity,
                                                  int occupiedCount,
                                                  Transform parent)
    {
        if (colorArray == null || colorArray.Length == 0)
        {
            return;
        }

        Color currentColor = colorArray[Mathf.Clamp(currentColorIndex, 0, colorArray.Length - 1)];
        Transform root = CreateResourceShell(name, position, currentColor, parent);
        CreateOccupancySlots(root, currentColor, capacity, occupiedCount, false);
        CreateColorSwitchTrack(root, colorArray, currentColorIndex);
    }

    private static void CreateColorSwitchClockResource(string name,
                                                       Vector2 position,
                                                       Color[] colorArray,
                                                       int currentColorIndex,
                                                       int capacity,
                                                       int occupiedCount,
                                                       int remainingRoundCount,
                                                       bool isOpen,
                                                       Transform parent)
    {
        if (colorArray == null || colorArray.Length == 0)
        {
            return;
        }

        Color currentColor = colorArray[Mathf.Clamp(currentColorIndex, 0, colorArray.Length - 1)];
        Transform root = CreateResourceShell(name, position, currentColor, parent);
        CreateOccupancySlots(root, currentColor, capacity, occupiedCount, false);
        CreateColorSwitchTrack(root, colorArray, currentColorIndex);
        CreateClockBadge(root, Amber, remainingRoundCount, isOpen, true);
    }

    private static void CreateEmptyColorResource(string name,
                                                 Vector2 position,
                                                 Color color,
                                                 int capacity,
                                                 int occupiedCount,
                                                 bool isFixed,
                                                 Transform parent)
    {
        Color shellColor = isFixed ? color : Muted;
        Transform root = CreateResourceShell(name, position, shellColor, parent);
        CreateOccupancySlots(root, color, capacity, occupiedCount, false);

        if (isFixed)
        {
            return;
        }

        Transform ruleVisuals = CreateGroup("RuleVisuals", root);
        Transform emptyColor = CreateGroup("EmptyColor", ruleVisuals);
        CreateLine("SlashA", new Vector2(-0.29f, -0.29f), new Vector2(0.29f, 0.29f), 0.035f, WithAlpha(Muted, 0.52f), emptyColor, 23);
        CreateLine("SlashB", new Vector2(-0.29f, 0.29f), new Vector2(0.29f, -0.29f), 0.035f, WithAlpha(Muted, 0.52f), emptyColor, 23);
    }

    private static void CreateSimultaneousResource(string name,
                                                   Vector2 position,
                                                   Color color,
                                                   int capacity,
                                                   int occupiedCount,
                                                   Transform parent)
    {
        Transform root = CreateResourceShell(name, position, color, parent);
        CreateSimultaneousVisual(root, color, capacity, occupiedCount);
    }

    private static void CreateSimultaneousClockResource(string name,
                                                        Vector2 position,
                                                        Color color,
                                                        int capacity,
                                                        int occupiedCount,
                                                        int remainingRoundCount,
                                                        Transform parent)
    {
        Transform root = CreateResourceShell(name, position, color, parent);
        CreateSimultaneousVisual(root, color, capacity, occupiedCount);
        CreateClockBadge(root, Amber, remainingRoundCount, true);
    }

    private static Transform CreateResourceShell(string name, Vector2 position, Color color, Transform parent)
    {
        Transform root = CreateGroup(name, parent);
        root.localPosition = ToVector3(position);
        Transform background = CreateGroup("Background", root);
        CreateGroup("StateOverlay", root);
        CreateGroup("RelayAnchors", root);

        CreateRectangle("Shadow", new Vector2(0.07f, -0.07f), new Vector2(1.02f, 1.02f), 0.21f, WithAlpha(Color.black, 0.25f), background, 5);
        CreateRectangle("Stroke", Vector2.zero, new Vector2(ResourceSize, ResourceSize), 0.20f, color, background, 20);
        CreateRectangle("Fill", Vector2.zero, new Vector2(ResourceFillSize, ResourceFillSize), 0.17f, StationFill, background, 22);
        return root;
    }

    private static void CreateOccupancySlots(Transform resourceRoot,
                                             Color occupiedColor,
                                             int capacity,
                                             int occupiedCount,
                                             bool radialLayout)
    {
        Transform slots = CreateGroup("OccupancySlots", resourceRoot);
        int clampedOccupiedCount = Mathf.Clamp(occupiedCount, 0, capacity);

        for (int index = 0; index < capacity; index++)
        {
            Transform slot = CreateGroup("Slot_" + index, slots);
            slot.localPosition = ToVector3(GetOccupancySlotPosition(index, capacity, radialLayout));
            bool isOccupied = index < clampedOccupiedCount;
            Color rimColor = isOccupied ? WithAlpha(Color.white, 0.86f) : WithAlpha(Muted, 0.52f);
            Color fillColor = isOccupied ? occupiedColor : WithAlpha(Muted, 0.28f);
            CreateDisc("Rim", Vector2.zero, 0.12f, rimColor, slot, 24);
            CreateDisc("Fill", Vector2.zero, 0.092f, fillColor, slot, 25);
        }
    }

    private static void CreateSimultaneousVisual(Transform resourceRoot,
                                                 Color color,
                                                 int capacity,
                                                 int occupiedCount)
    {
        Transform links = CreateGroup("Links", resourceRoot);
        Transform slots = CreateGroup("OccupancySlots", resourceRoot);
        Transform ruleVisuals = CreateGroup("RuleVisuals", resourceRoot);
        Transform simultaneous = CreateGroup("Simultaneous", ruleVisuals);
        Vector2 hubPosition = capacity == 3 ? new Vector2(0f, -0.06f) : Vector2.zero;
        int clampedOccupiedCount = Mathf.Clamp(occupiedCount, 0, capacity);

        for (int index = 0; index < capacity; index++)
        {
            Vector2 slotPosition = GetSimultaneousSlotPosition(index, capacity) + hubPosition;
            CreateLine("Link_" + index, slotPosition, hubPosition, 0.04f, WithAlpha(color, 0.62f), links, 23);

            Transform slot = CreateGroup("Slot_" + index, slots);
            slot.localPosition = ToVector3(slotPosition);
            bool isOccupied = index < clampedOccupiedCount;
            CreateDisc("Rim", Vector2.zero, 0.115f, isOccupied ? WithAlpha(Color.white, 0.86f) : WithAlpha(Muted, 0.52f), slot, 24);
            CreateDisc("Fill", Vector2.zero, 0.087f, isOccupied ? color : WithAlpha(Muted, 0.28f), slot, 25);
        }

        bool isActivated = clampedOccupiedCount == capacity;
        Color hubColor = isActivated ? Green : Red;
        CreateDisc("ActivationLight", hubPosition, 0.12f, hubColor, simultaneous, 26);
    }

    private static void CreateClockBadge(Transform resourceRoot,
                                         Color color,
                                         int remainingRoundCount,
                                         bool isOpen,
                                         bool useCompactPosition = false)
    {
        Transform ruleVisuals = FindOrCreateChild(resourceRoot, "RuleVisuals");
        Transform clock = CreateGroup("Clock", ruleVisuals);
        Vector2 badgePosition = useCompactPosition ? new Vector2(-0.48f, 0.40f) : new Vector2(0f, 0.64f);
        float badgeStrokeRadius = useCompactPosition ? 0.16f : 0.21f;
        float badgeFillRadius = useCompactPosition ? 0.13f : 0.17f;
        float shadowOffset = useCompactPosition ? 0.018f : 0.025f;
        float fontSize = useCompactPosition ? 3.5f : 5f;
        Color strokeColor = isOpen ? color : WithAlpha(Muted, 0.58f);
        Color fillColor = isOpen ? WithAlpha(StationFill, 0.98f) : WithAlpha(StationFill, 0.46f);

        CreateDisc("BadgeShadow", badgePosition + new Vector2(shadowOffset, -shadowOffset), badgeStrokeRadius + 0.01f, WithAlpha(Color.black, 0.22f), clock, 27);
        CreateDisc("BadgeStroke", badgePosition, badgeStrokeRadius, strokeColor, clock, 28);
        CreateDisc("BadgeFill", badgePosition, badgeFillRadius, fillColor, clock, 29);
        CreateLabel("TurnCount", remainingRoundCount.ToString(), badgePosition + new Vector2(0f, -0.015f), fontSize, isOpen ? Ink : WithAlpha(Ink, 0.48f), clock, 60);
    }

    private static void CreateColorSwitchTrack(Transform resourceRoot, Color[] colorArray, int currentColorIndex)
    {
        Transform ruleVisuals = FindOrCreateChild(resourceRoot, "RuleVisuals");
        Transform colorSwitchTrack = CreateGroup("ColorSwitchTrack", ruleVisuals);
        Transform links = CreateGroup("SequenceLinks", colorSwitchTrack);
        Transform chips = CreateGroup("ColorChips", colorSwitchTrack);
        int clampedCurrentColorIndex = Mathf.Clamp(currentColorIndex, 0, colorArray.Length - 1);
        int nextColorIndex = (clampedCurrentColorIndex + 1) % colorArray.Length;
        float chipSpacing = GetColorSwitchChipSpacing(colorArray.Length);
        float lineInset = colorArray.Length <= 3 ? 0.14f : 0.11f;
        float transitionArrowSize = colorArray.Length <= 3 ? 0.04f : 0.03f;
        float currentRimRadius = colorArray.Length <= 3 ? 0.13f : 0.105f;
        float otherRimRadius = colorArray.Length <= 3 ? 0.10f : 0.082f;

        colorSwitchTrack.localPosition = ToVector3(new Vector2(0f, 0.76f));

        for (int index = 0; index < colorArray.Length - 1; index++)
        {
            Vector2 start = GetColorSwitchChipPosition(index, colorArray.Length, chipSpacing);
            Vector2 end = GetColorSwitchChipPosition(index + 1, colorArray.Length, chipSpacing);
            bool isActiveTransition = index == clampedCurrentColorIndex;
            float thickness = isActiveTransition ? 0.055f : 0.026f;
            Color lineColor = isActiveTransition ? WithAlpha(StationFill, 0.94f) : WithAlpha(Muted, 0.38f);
            Vector2 lineStart = start + Vector2.right * lineInset;
            Vector2 lineEnd = end - Vector2.right * lineInset;
            CreateLine("SequenceLink_" + index, lineStart, lineEnd, thickness, lineColor, links, 27);

            if (isActiveTransition)
            {
                CreateChevron("ActiveTransitionArrow", Vector2.Lerp(lineStart, lineEnd, 0.60f), transitionArrowSize, StationFill, links, 30);
            }
        }

        for (int index = 0; index < colorArray.Length; index++)
        {
            Transform chip = CreateGroup("Color_" + index, chips);
            chip.localPosition = ToVector3(GetColorSwitchChipPosition(index, colorArray.Length, chipSpacing));
            bool isCurrent = index == clampedCurrentColorIndex;
            bool isNext = index == nextColorIndex;
            float rimRadius = isCurrent ? currentRimRadius : otherRimRadius;
            float fillRadius = rimRadius * 0.74f;
            Color fillColor = isCurrent ? colorArray[index] : WithAlpha(colorArray[index], isNext ? 0.92f : 0.62f);
            Color rimColor = isCurrent ? WithAlpha(Color.white, 0.96f) : WithAlpha(Color.white, isNext ? 0.78f : 0.36f);
            CreateDisc("Rim", Vector2.zero, rimRadius, rimColor, chip, 28);
            CreateDisc("Fill", Vector2.zero, fillRadius, fillColor, chip, 29);

            if (isNext && colorArray.Length > 1)
            {
                CreateRing("NextMarker", Vector2.zero, rimRadius + 0.022f, 0.024f, WithAlpha(StationFill, 0.92f), chip, 30);
                CreateChevron("NextPointer", new Vector2(0f, rimRadius + 0.075f), transitionArrowSize, StationFill, chip, 31, -90f);
            }
        }
    }

    private static void CreateMetroConnection(string name, Color color, Transform parent, Vector2 start, Vector2 end)
    {
        Transform root = CreateGroup(name, parent);
        Transform background = CreateGroup("Background", root);
        Transform track = CreateGroup("Track", root);
        CreateLine("Shadow", start, end, ConnectionThickness + 0.08f, WithAlpha(Color.black, 0.34f), background, 10);
        CreateLine("Line", start, end, ConnectionThickness, color, track, 12);
    }

    private static void CreateRelay(string name, Vector2 start, Vector2 end, ERelayVisualType type, Transform parent)
    {
        Transform root = CreateGroup(name, parent);
        Transform links = CreateGroup("Links", root);
        Transform endpoints = CreateGroup("Endpoints", root);
        Vector2 direction = (end - start).normalized;
        float length = Vector2.Distance(start, end);
        float nodePadding = 0.52f;
        float stubLength = Mathf.Min(0.60f, Mathf.Max(0f, (length - nodePadding * 2f) * 0.5f));
        Vector2 visibleStart = start + direction * nodePadding;
        Vector2 visibleEnd = end - direction * nodePadding;
        Vector2 startStubEnd = visibleStart + direction * stubLength;
        Vector2 endStubEnd = visibleEnd - direction * stubLength;
        Color relayColor = new Color(0.08f, 0.09f, 0.10f, 1f);

        CreateLine("StartStub", visibleStart, startStubEnd, 0.14f, WithAlpha(relayColor, 0.45f), links, 14);
        CreateLine("EndStub", visibleEnd, endStubEnd, 0.14f, WithAlpha(relayColor, 0.45f), links, 14);

        if (type == ERelayVisualType.Link)
        {
            CreateRing("LinkStart", startStubEnd, 0.12f, 0.035f, WithAlpha(relayColor, 0.68f), endpoints, 30);
            CreateRing("LinkEnd", endStubEnd, 0.12f, 0.035f, WithAlpha(relayColor, 0.68f), endpoints, 30);
            return;
        }

        CreateDisc("TransferSender", startStubEnd, 0.095f, WithAlpha(relayColor, 0.62f), endpoints, 30);
        CreateRing("TransferReceiver", endStubEnd, 0.12f, 0.035f, WithAlpha(relayColor, 0.68f), endpoints, 30);
        CreateDisc("TransferDirection", Vector2.Lerp(visibleStart, startStubEnd, 0.62f), 0.055f, WithAlpha(relayColor, 0.50f), endpoints, 30);
    }

    private static Vector2 GetOccupancySlotPosition(int index, int capacity, bool radialLayout)
    {
        if (radialLayout)
        {
            return GetSimultaneousSlotPosition(index, capacity);
        }

        if (capacity == 1)
        {
            return Vector2.zero;
        }

        if (capacity == 2)
        {
            return new Vector2(index == 0 ? -0.18f : 0.18f, 0f);
        }

        if (capacity == 3)
        {
            return GetSimultaneousSlotPosition(index, capacity) + new Vector2(0f, -0.06f);
        }

        if (capacity == 4)
        {
            Vector2[] positionArray =
            {
                new Vector2(-0.16f, 0.16f),
                new Vector2(0.16f, 0.16f),
                new Vector2(-0.16f, -0.16f),
                new Vector2(0.16f, -0.16f)
            };

            return positionArray[index];
        }

        return GetRadialPosition(index, capacity, 0.22f);
    }

    private static Vector2 GetSimultaneousSlotPosition(int index, int capacity)
    {
        return GetRadialPosition(index, capacity, 0.29f);
    }

    private static float GetColorSwitchChipSpacing(int colorCount)
    {
        if (colorCount <= 3)
        {
            return 0.40f;
        }

        if (colorCount == 4)
        {
            return 0.30f;
        }

        return 0.25f;
    }

    private static Vector2 GetColorSwitchChipPosition(int index, int colorCount, float chipSpacing)
    {
        float startX = -(colorCount - 1) * chipSpacing * 0.5f;
        return new Vector2(startX + index * chipSpacing, 0f);
    }

    private static Vector2 GetRadialPosition(int index, int count, float radius)
    {
        float angle = Mathf.PI * 0.5f + Mathf.PI * 2f * index / count;
        return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
    }

    private static string GetProcessStateLabel(EProcessSampleState state)
    {
        switch (state)
        {
            case EProcessSampleState.Selected:
                return "Selected";
            case EProcessSampleState.Waiting:
                return "Waiting";
            case EProcessSampleState.Completed:
                return "Completed";
            default:
                return "Default";
        }
    }

    private static void ConfigurePrototypeCamera()
    {
        Camera targetCamera = Camera.main;

        if (targetCamera == null)
        {
            return;
        }

        targetCamera.orthographic = true;
        targetCamera.orthographicSize = 8.5f;
        targetCamera.backgroundColor = new Color(0.07f, 0.085f, 0.10f, 1f);
        targetCamera.transform.position = new Vector3(0f, 0.5f, -10f);
    }

    private static void CreateSectionLabel(string text, Vector2 position, Transform parent)
    {
        CreateLabel("SectionLabel", text, position, 4f, WithAlpha(StationFill, 0.92f), parent, 60);
    }

    private static Transform FindOrCreateChild(Transform parent, string name)
    {
        Transform child = parent.Find(name);

        if (child != null)
        {
            return child;
        }

        return CreateGroup(name, parent);
    }

    private static Transform CreateGroup(string name, Transform parent)
    {
        return CreateObject(name, parent).transform;
    }

    private static GameObject CreateObject(string name, Transform parent)
    {
        GameObject gameObject = new GameObject(name);
        gameObject.transform.SetParent(parent, false);
        gameObject.transform.localPosition = Vector3.zero;
        gameObject.transform.localRotation = Quaternion.identity;
        gameObject.transform.localScale = Vector3.one;
        Undo.RegisterCreatedObjectUndo(gameObject, "Rebuild Shapes Visual Prototype");
        return gameObject;
    }

    private static Disc CreateDisc(string name, Vector2 localPosition, float radius, Color color, Transform parent, int sortingOrder)
    {
        GameObject gameObject = CreateObject(name, parent);
        gameObject.transform.localPosition = ToVector3(localPosition);

        Disc disc = gameObject.AddComponent<Disc>();
        disc.Type = DiscType.Disc;
        disc.Geometry = DiscGeometry.Flat2D;
        disc.Radius = radius;
        disc.Color = color;
        disc.SortingOrder = sortingOrder;
        return disc;
    }

    private static Disc CreateRing(string name, Vector2 localPosition, float radius, float thickness, Color color, Transform parent, int sortingOrder)
    {
        GameObject gameObject = CreateObject(name, parent);
        gameObject.transform.localPosition = ToVector3(localPosition);

        Disc disc = gameObject.AddComponent<Disc>();
        disc.Type = DiscType.Ring;
        disc.Geometry = DiscGeometry.Flat2D;
        disc.Radius = radius;
        disc.Thickness = thickness;
        disc.Color = color;
        disc.SortingOrder = sortingOrder;
        return disc;
    }

    private static Shapes.Rectangle CreateRectangle(string name,
                                                    Vector2 localPosition,
                                                    Vector2 size,
                                                    float cornerRadius,
                                                    Color color,
                                                    Transform parent,
                                                    int sortingOrder)
    {
        GameObject gameObject = CreateObject(name, parent);
        gameObject.transform.localPosition = ToVector3(localPosition);

        Shapes.Rectangle rectangle = gameObject.AddComponent<Shapes.Rectangle>();
        rectangle.Type = Shapes.Rectangle.RectangleType.RoundedSolid;
        rectangle.Width = size.x;
        rectangle.Height = size.y;
        rectangle.CornerRadius = cornerRadius;
        rectangle.Color = color;
        rectangle.SortingOrder = sortingOrder;
        return rectangle;
    }

    private static Line CreateLine(string name, Vector2 start, Vector2 end, float thickness, Color color, Transform parent, int sortingOrder)
    {
        GameObject gameObject = CreateObject(name, parent);

        Line line = gameObject.AddComponent<Line>();
        line.Geometry = LineGeometry.Flat2D;
        line.EndCaps = LineEndCap.Round;
        line.Start = ToVector3(start);
        line.End = ToVector3(end);
        line.Thickness = thickness;
        line.Color = color;
        line.SortingOrder = sortingOrder;
        return line;
    }

    private static void CreateChevron(string name,
                                      Vector2 localPosition,
                                      float size,
                                      Color color,
                                      Transform parent,
                                      int sortingOrder,
                                      float rotationZ = 0f)
    {
        Transform chevron = CreateGroup(name, parent);
        chevron.localPosition = ToVector3(localPosition);
        chevron.localRotation = Quaternion.Euler(0f, 0f, rotationZ);

        CreateLine("Upper", new Vector2(-size, size), Vector2.zero, 0.022f, color, chevron, sortingOrder);
        CreateLine("Lower", new Vector2(-size, -size), Vector2.zero, 0.022f, color, chevron, sortingOrder);
    }

    private static TextMeshPro CreateLabel(string name,
                                            string text,
                                            Vector2 localPosition,
                                            float fontSize,
                                            Color color,
                                            Transform parent,
                                            int sortingOrder)
    {
        GameObject gameObject = CreateObject(name, parent);
        gameObject.transform.localPosition = ToVector3(localPosition, -0.01f);

        TextMeshPro label = gameObject.AddComponent<TextMeshPro>();
        label.text = text;
        label.fontSize = fontSize;
        label.color = color;
        label.alignment = TextAlignmentOptions.Center;
        label.enableWordWrapping = false;
        label.sortingOrder = sortingOrder;
        label.rectTransform.sizeDelta = new Vector2(4f, 1f);
        return label;
    }

    private static void RemoveGeneratedRoot(Transform parent, string generatedRootName)
    {
        Transform generatedRoot = parent.Find(generatedRootName);

        if (generatedRoot == null)
        {
            return;
        }

        Undo.DestroyObjectImmediate(generatedRoot.gameObject);
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

    private enum EProcessSampleState
    {
        Default,
        Selected,
        Waiting,
        Completed,
    }

    private enum ERelayVisualType
    {
        Link,
        Transfer,
    }
}

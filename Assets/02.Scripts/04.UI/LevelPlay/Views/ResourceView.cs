using System;
using System.Collections.Generic;
using Shapes;
using TMPro;
using UnityEngine;

public sealed class ResourceView : MonoBehaviour
{
    private const int MaxCapacity = 4;
    private const int MaxColorSwitchColorCount = 5;

    public event Action<int> OnPressed;

    [SerializeField]
    private VisualSettingsSO _visualSettings;

    [SerializeField]
    private Shapes.Rectangle _shadowRectangle;

    [SerializeField]
    private Shapes.Rectangle _strokeRectangle;

    [SerializeField]
    private Shapes.Rectangle _fillRectangle;

    [SerializeField]
    private BoxCollider2D _clickCollider;

    private readonly ResourceSlotVariant[] _occupancyVariantArray = new ResourceSlotVariant[MaxCapacity];
    private readonly ResourceSlotVariant[] _simultaneousOccupancyVariantArray = new ResourceSlotVariant[MaxCapacity];
    private readonly ColorSwitchVariant[] _colorSwitchVariantArray = new ColorSwitchVariant[MaxColorSwitchColorCount];
    private readonly SimultaneousVariant[] _simultaneousVariantArray = new SimultaneousVariant[MaxCapacity];
    private ClockVariant _normalClockVariant;
    private ClockVariant _compactClockVariant;
    private Transform _emptyColorRoot;
    private Disc _lockOverlay;
    private int _resourceId;

    public int ResourceId => _resourceId;

    private void Awake()
    {
        CacheReferences();
    }

    public void SetVisualSettings(VisualSettingsSO visualSettings)
    {
        _visualSettings = visualSettings;
    }

    public void ConfigurePrefabReferences(VisualSettingsSO visualSettings)
    {
        _visualSettings = visualSettings;
        CacheReferences();
    }

    public void Refresh(int resourceId,
                        Vector2 localPosition,
                        int baseColorId,
                        int capacity,
                        IReadOnlyList<int> occupiedColorIdList,
                        bool isLocked,
                        bool isHighlighted,
                        ResourceRuleVisualData ruleVisualData)
    {
        CacheReferences();

        if (!HasRequiredReferences())
        {
            return;
        }

        _resourceId = resourceId;
        int clampedCapacity = Mathf.Clamp(capacity, 1, MaxCapacity);
        if (ruleVisualData == null)
        {
            return;
        }

        ResourceRuleVisualData visualData = ruleVisualData;
        transform.localPosition = LevelPlayViewShapeUtility.ToVector3(localPosition);
        RefreshShell(baseColorId, isLocked, isHighlighted, visualData);
        RefreshOccupancySlots(clampedCapacity, occupiedColorIdList, visualData.IsSimultaneous);
        RefreshRuleVisuals(baseColorId, clampedCapacity, occupiedColorIdList, visualData);
        RefreshStateOverlay(isLocked);
    }

    private void RefreshShell(int baseColorId,
                              bool isLocked,
                              bool isHighlighted,
                              ResourceRuleVisualData visualData)
    {
        Color baseColor = _visualSettings.GetColor(baseColorId);

        if (visualData.HasEmptyColor && !visualData.IsEmptyColorFixed)
        {
            baseColor = _visualSettings.MutedColor;
        }

        if (visualData.HasClock && !visualData.IsClockOpen)
        {
            baseColor = LevelPlayViewShapeUtility.WithAlpha(baseColor, 0.42f);
        }

        _shadowRectangle.Color = LevelPlayViewShapeUtility.WithAlpha(Color.black, isHighlighted ? 0.36f : 0.25f);
        _strokeRectangle.Color = isHighlighted ? _visualSettings.SelectedColor : baseColor;
        _fillRectangle.Color = _visualSettings.StationFillColor;
    }

    private void RefreshOccupancySlots(int capacity,
                                       IReadOnlyList<int> occupiedColorIdList,
                                       bool isSimultaneous)
    {
        int occupiedCount = occupiedColorIdList != null ? Mathf.Min(occupiedColorIdList.Count, capacity) : 0;

        for (int variantIndex = 0; variantIndex < _occupancyVariantArray.Length; variantIndex++)
        {
            ResourceSlotVariant variant = _occupancyVariantArray[variantIndex];

            if (variant == null)
            {
                continue;
            }

            bool isActiveVariant = !isSimultaneous && variantIndex + 1 == capacity;
            variant.Root.gameObject.SetActive(isActiveVariant);

            if (!isActiveVariant)
            {
                continue;
            }

            for (int slotIndex = 0; slotIndex < variant.SlotArray.Length; slotIndex++)
            {
                ResourceSlot slot = variant.SlotArray[slotIndex];
                bool isOccupied = slotIndex < occupiedCount;
                slot.Rim.Color = isOccupied
                    ? LevelPlayViewShapeUtility.WithAlpha(Color.white, 0.86f)
                    : LevelPlayViewShapeUtility.WithAlpha(_visualSettings.MutedColor, 0.52f);
                slot.Fill.Color = isOccupied
                    ? _visualSettings.GetColor(occupiedColorIdList[slotIndex])
                    : LevelPlayViewShapeUtility.WithAlpha(_visualSettings.MutedColor, 0.28f);
            }
        }

        for (int variantIndex = 0; variantIndex < _simultaneousOccupancyVariantArray.Length; variantIndex++)
        {
            ResourceSlotVariant variant = _simultaneousOccupancyVariantArray[variantIndex];

            if (variant == null)
            {
                continue;
            }

            bool isActiveVariant = isSimultaneous && variantIndex + 1 == capacity;
            variant.Root.gameObject.SetActive(isActiveVariant);

            if (!isActiveVariant)
            {
                continue;
            }

            for (int slotIndex = 0; slotIndex < variant.SlotArray.Length; slotIndex++)
            {
                ResourceSlot slot = variant.SlotArray[slotIndex];
                bool isOccupied = slotIndex < occupiedCount;
                slot.Rim.Color = isOccupied
                    ? LevelPlayViewShapeUtility.WithAlpha(Color.white, 0.86f)
                    : LevelPlayViewShapeUtility.WithAlpha(_visualSettings.MutedColor, 0.52f);
                slot.Fill.Color = isOccupied
                    ? _visualSettings.GetColor(occupiedColorIdList[slotIndex])
                    : LevelPlayViewShapeUtility.WithAlpha(_visualSettings.MutedColor, 0.28f);
            }
        }
    }

    private void RefreshRuleVisuals(int baseColorId,
                                    int capacity,
                                    IReadOnlyList<int> occupiedColorIdList,
                                    ResourceRuleVisualData visualData)
    {
        RefreshColorSwitch(visualData);
        RefreshClock(visualData, visualData.ColorSwitchColorIdArray.Length > 0);

        if (_emptyColorRoot != null)
        {
            _emptyColorRoot.gameObject.SetActive(visualData.HasEmptyColor && !visualData.IsEmptyColorFixed);
        }

        RefreshSimultaneous(baseColorId, capacity, occupiedColorIdList, visualData.IsSimultaneous);
    }

    private void RefreshColorSwitch(ResourceRuleVisualData visualData)
    {
        int colorCount = Mathf.Min(visualData.ColorSwitchColorIdArray.Length, MaxColorSwitchColorCount);

        for (int variantIndex = 0; variantIndex < _colorSwitchVariantArray.Length; variantIndex++)
        {
            ColorSwitchVariant variant = _colorSwitchVariantArray[variantIndex];

            if (variant == null)
            {
                continue;
            }

            bool isActiveVariant = variantIndex + 1 == colorCount;
            variant.Root.gameObject.SetActive(isActiveVariant);

            if (!isActiveVariant)
            {
                continue;
            }

            int currentIndex = Mathf.Clamp(visualData.CurrentColorSwitchIndex, 0, colorCount - 1);
            int nextIndex = (currentIndex + 1) % colorCount;

            for (int chipIndex = 0; chipIndex < variant.ChipArray.Length; chipIndex++)
            {
                ColorSwitchChip chip = variant.ChipArray[chipIndex];
                bool isCurrent = chipIndex == currentIndex;
                bool isNext = colorCount > 1 && chipIndex == nextIndex;
                Color chipColor = _visualSettings.GetColor(visualData.ColorSwitchColorIdArray[chipIndex]);
                chip.Rim.Color = LevelPlayViewShapeUtility.WithAlpha(Color.white, isCurrent ? 0.96f : isNext ? 0.78f : 0.36f);
                chip.Fill.Color = LevelPlayViewShapeUtility.WithAlpha(chipColor, isCurrent ? 1f : isNext ? 0.92f : 0.62f);
                chip.NextMarker.gameObject.SetActive(isNext);
            }

            for (int linkIndex = 0; linkIndex < variant.LinkArray.Length; linkIndex++)
            {
                variant.LinkArray[linkIndex].Color = linkIndex == currentIndex
                    ? LevelPlayViewShapeUtility.WithAlpha(_visualSettings.StationFillColor, 0.94f)
                    : LevelPlayViewShapeUtility.WithAlpha(_visualSettings.MutedColor, 0.38f);
            }

            if (variant.Pointer != null)
            {
                variant.Pointer.gameObject.SetActive(colorCount > 1);
            }
        }
    }

    private void RefreshClock(ResourceRuleVisualData visualData, bool useCompactVariant)
    {
        ClockVariant activeVariant = useCompactVariant ? _compactClockVariant : _normalClockVariant;
        ClockVariant inactiveVariant = useCompactVariant ? _normalClockVariant : _compactClockVariant;

        if (inactiveVariant != null)
        {
            inactiveVariant.Root.gameObject.SetActive(false);
        }

        if (activeVariant == null)
        {
            return;
        }

        activeVariant.Root.gameObject.SetActive(visualData.HasClock);

        if (!visualData.HasClock)
        {
            return;
        }

        Color strokeColor = visualData.IsClockOpen
            ? _visualSettings.WaitingColor
            : LevelPlayViewShapeUtility.WithAlpha(_visualSettings.MutedColor, 0.58f);
        Color fillColor = visualData.IsClockOpen
            ? LevelPlayViewShapeUtility.WithAlpha(_visualSettings.StationFillColor, 0.98f)
            : LevelPlayViewShapeUtility.WithAlpha(_visualSettings.StationFillColor, 0.46f);
        activeVariant.Shadow.Color = LevelPlayViewShapeUtility.WithAlpha(Color.black, 0.22f);
        activeVariant.Stroke.Color = strokeColor;
        activeVariant.Fill.Color = fillColor;
        activeVariant.Label.text = Mathf.Max(0, visualData.ClockRemainingRoundCount).ToString();
        activeVariant.Label.color = visualData.IsClockOpen
            ? _visualSettings.InkColor
            : LevelPlayViewShapeUtility.WithAlpha(_visualSettings.InkColor, 0.48f);
    }

    private void RefreshSimultaneous(int baseColorId,
                                     int capacity,
                                     IReadOnlyList<int> occupiedColorIdList,
                                     bool isSimultaneous)
    {
        int occupiedCount = occupiedColorIdList != null ? Mathf.Min(occupiedColorIdList.Count, capacity) : 0;

        for (int variantIndex = 0; variantIndex < _simultaneousVariantArray.Length; variantIndex++)
        {
            SimultaneousVariant variant = _simultaneousVariantArray[variantIndex];

            if (variant == null)
            {
                continue;
            }

            bool isActiveVariant = isSimultaneous && variantIndex + 1 == capacity;
            variant.Root.gameObject.SetActive(isActiveVariant);

            if (!isActiveVariant)
            {
                continue;
            }

            Color linkColor = LevelPlayViewShapeUtility.WithAlpha(_visualSettings.GetColor(baseColorId), 0.62f);

            for (int linkIndex = 0; linkIndex < variant.LinkArray.Length; linkIndex++)
            {
                variant.LinkArray[linkIndex].Color = linkColor;
            }

            variant.ActivationLight.Color = occupiedCount == capacity
                ? _visualSettings.CompletedColor
                : _visualSettings.BlockedColor;
        }
    }

    private void RefreshStateOverlay(bool isLocked)
    {
        if (_lockOverlay == null)
        {
            return;
        }

        _lockOverlay.gameObject.SetActive(isLocked);

        if (isLocked)
        {
            _lockOverlay.Color = LevelPlayViewShapeUtility.WithAlpha(_visualSettings.BlockedColor, 0.78f);
        }
    }

    private void CacheReferences()
    {
        _shadowRectangle ??= FindComponent<Shapes.Rectangle>("Background/Shadow");
        _strokeRectangle ??= FindComponent<Shapes.Rectangle>("Background/Stroke");
        _fillRectangle ??= FindComponent<Shapes.Rectangle>("Background/Fill");
        _clickCollider ??= GetComponent<BoxCollider2D>();
        _emptyColorRoot ??= transform.Find("RuleVisuals/EmptyColor");
        _lockOverlay ??= FindComponent<Disc>("StateOverlay/Lock");
        _normalClockVariant ??= CacheClockVariant("Normal");
        _compactClockVariant ??= CacheClockVariant("Compact");

        for (int capacity = 1; capacity <= MaxCapacity; capacity++)
        {
            int index = capacity - 1;
            _occupancyVariantArray[index] ??= CacheOccupancyVariant(capacity);
            _simultaneousOccupancyVariantArray[index] ??= CacheOccupancyVariant(capacity, true);
            _simultaneousVariantArray[index] ??= CacheSimultaneousVariant(capacity);
        }

        for (int colorCount = 1; colorCount <= MaxColorSwitchColorCount; colorCount++)
        {
            int index = colorCount - 1;
            _colorSwitchVariantArray[index] ??= CacheColorSwitchVariant(colorCount);
        }
    }

    private ResourceSlotVariant CacheOccupancyVariant(int capacity, bool isSimultaneous = false)
    {
        string variantName = isSimultaneous ? "SimultaneousCapacity_" : "Capacity_";
        Transform root = transform.Find("OccupancySlots/" + variantName + capacity);

        if (root == null)
        {
            return null;
        }

        ResourceSlot[] slotArray = new ResourceSlot[capacity];

        for (int slotNumber = 1; slotNumber <= capacity; slotNumber++)
        {
            Transform slotRoot = root.Find("Slot_" + slotNumber);
            slotArray[slotNumber - 1] = new ResourceSlot(slotRoot.Find("Rim").GetComponent<Disc>(),
                                                          slotRoot.Find("Fill").GetComponent<Disc>());
        }

        return new ResourceSlotVariant(root, slotArray);
    }

    private ColorSwitchVariant CacheColorSwitchVariant(int colorCount)
    {
        Transform root = transform.Find("RuleVisuals/ColorSwitch/Variant_" + colorCount);

        if (root == null)
        {
            return null;
        }

        ColorSwitchChip[] chipArray = new ColorSwitchChip[colorCount];

        for (int chipNumber = 1; chipNumber <= colorCount; chipNumber++)
        {
            Transform chipRoot = root.Find("Chip_" + chipNumber);
            chipArray[chipNumber - 1] = new ColorSwitchChip(chipRoot.Find("Rim").GetComponent<Disc>(),
                                                             chipRoot.Find("Fill").GetComponent<Disc>(),
                                                             chipRoot.Find("NextMarker").GetComponent<Disc>());
        }

        Line[] linkArray = new Line[Mathf.Max(0, colorCount - 1)];

        for (int linkNumber = 1; linkNumber < colorCount; linkNumber++)
        {
            linkArray[linkNumber - 1] = FindComponent<Line>("RuleVisuals/ColorSwitch/Variant_" + colorCount + "/Links/Link_" + linkNumber);
        }

        return new ColorSwitchVariant(root,
                                      chipArray,
                                      linkArray,
                                      root.Find("Pointer"));
    }

    private ClockVariant CacheClockVariant(string variantName)
    {
        Transform root = transform.Find("RuleVisuals/Clock/" + variantName);

        if (root == null)
        {
            return null;
        }

        return new ClockVariant(root,
                                root.Find("Badge/Shadow").GetComponent<Disc>(),
                                root.Find("Badge/Stroke").GetComponent<Disc>(),
                                root.Find("Badge/Fill").GetComponent<Disc>(),
                                root.Find("TurnCount").GetComponent<TextMeshPro>());
    }

    private SimultaneousVariant CacheSimultaneousVariant(int capacity)
    {
        Transform root = transform.Find("RuleVisuals/Simultaneous/Capacity_" + capacity);

        if (root == null)
        {
            return null;
        }

        Line[] linkArray = new Line[capacity];

        for (int linkNumber = 1; linkNumber <= capacity; linkNumber++)
        {
            linkArray[linkNumber - 1] = root.Find("Links/Link_" + linkNumber).GetComponent<Line>();
        }

        return new SimultaneousVariant(root,
                                       linkArray,
                                       root.Find("ActivationLight").GetComponent<Disc>());
    }

    private T FindComponent<T>(string path) where T : Component
    {
        Transform target = transform.Find(path);
        return target != null ? target.GetComponent<T>() : null;
    }

    private bool HasRequiredReferences()
    {
        return _visualSettings != null &&
               _shadowRectangle != null &&
               _strokeRectangle != null &&
               _fillRectangle != null &&
               _clickCollider != null;
    }

    public void NotifyPressed()
    {
        OnPressed?.Invoke(_resourceId);
    }

    private sealed class ResourceSlotVariant
    {
        public readonly Transform Root;
        public readonly ResourceSlot[] SlotArray;

        public ResourceSlotVariant(Transform root, ResourceSlot[] slotArray)
        {
            Root = root;
            SlotArray = slotArray;
        }
    }

    private sealed class ResourceSlot
    {
        public readonly Disc Rim;
        public readonly Disc Fill;

        public ResourceSlot(Disc rim, Disc fill)
        {
            Rim = rim;
            Fill = fill;
        }
    }

    private sealed class ColorSwitchVariant
    {
        public readonly Transform Root;
        public readonly ColorSwitchChip[] ChipArray;
        public readonly Line[] LinkArray;
        public readonly Transform Pointer;

        public ColorSwitchVariant(Transform root, ColorSwitchChip[] chipArray, Line[] linkArray, Transform pointer)
        {
            Root = root;
            ChipArray = chipArray;
            LinkArray = linkArray;
            Pointer = pointer;
        }
    }

    private sealed class ColorSwitchChip
    {
        public readonly Disc Rim;
        public readonly Disc Fill;
        public readonly Disc NextMarker;

        public ColorSwitchChip(Disc rim, Disc fill, Disc nextMarker)
        {
            Rim = rim;
            Fill = fill;
            NextMarker = nextMarker;
        }
    }

    private sealed class ClockVariant
    {
        public readonly Transform Root;
        public readonly Disc Shadow;
        public readonly Disc Stroke;
        public readonly Disc Fill;
        public readonly TextMeshPro Label;

        public ClockVariant(Transform root, Disc shadow, Disc stroke, Disc fill, TextMeshPro label)
        {
            Root = root;
            Shadow = shadow;
            Stroke = stroke;
            Fill = fill;
            Label = label;
        }
    }

    private sealed class SimultaneousVariant
    {
        public readonly Transform Root;
        public readonly Line[] LinkArray;
        public readonly Disc ActivationLight;

        public SimultaneousVariant(Transform root, Line[] linkArray, Disc activationLight)
        {
            Root = root;
            LinkArray = linkArray;
            ActivationLight = activationLight;
        }
    }
}

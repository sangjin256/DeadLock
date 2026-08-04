using System;
using System.Collections.Generic;
using DG.Tweening;
using Shapes;
using UnityEngine;

public sealed class ProcessView : MonoBehaviour
{
    private const int MaxSlotCount = 6;

    public event Action<int> OnPressed;
    public event Action<int, int> OnSlotPressed;
    public event Action<int, int> OnSlotLongPressed;

    [SerializeField]
    private VisualSettingsSO _visualSettings;

    [SerializeField]
    private Disc _shadowDisc;

    [SerializeField]
    private Disc _strokeDisc;

    [SerializeField]
    private Disc _fillDisc;

    [SerializeField]
    private Disc _portDisc;

    [SerializeField]
    private Transform _requiredColorTrayRoot;

    [SerializeField]
    private Transform _stateOverlayRoot;

    private readonly ProcessTrayVariant[] _trayVariantArray = new ProcessTrayVariant[MaxSlotCount];
    private readonly Disc[] _stateOverlayDiscArray = new Disc[5];
    private int _processId;
    private Vector3 _portInitialLocalScale;
    private bool _isWaitingPulseActive;

    private void Awake()
    {
        CacheReferences();
        CacheInitialFeedbackState();
    }

    private void OnDisable()
    {
        StopFeedback();
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

    public void NotifyPressed()
    {
        OnPressed?.Invoke(_processId);
    }

    public void Refresh(int processId,
                        Vector2 localPosition,
                        IReadOnlyList<int> slotIdList,
                        IReadOnlyList<int> requiredColorIdList,
                        EProcessVisualState visualState)
    {
        CacheReferences();

        if (!HasRequiredReferences())
        {
            return;
        }

        _processId = processId;
        transform.localPosition = LevelPlayViewShapeUtility.ToVector3(localPosition);
        RefreshShell(visualState);
        RefreshWaitingPulse(visualState == EProcessVisualState.Waiting);
        RefreshRequiredColorTray(slotIdList, requiredColorIdList);
        RefreshStateOverlay(visualState);
    }

    public void PlayRejectedFeedback()
    {
        CacheReferences();
        CacheInitialFeedbackState();

        if (_visualSettings == null || _strokeDisc == null)
        {
            return;
        }

        Transform strokeTransform = _strokeDisc.transform;
        strokeTransform.DOKill();
        strokeTransform.DOPunchPosition(Vector3.right * _visualSettings.InvalidInputShakeDistance,
                                        _visualSettings.InvalidInputShakeDuration,
                                        8,
                                        0.72f)
                       .SetUpdate(true);
    }

    private void RefreshShell(EProcessVisualState visualState)
    {
        Color strokeColor = _visualSettings.MutedColor;
        Color fillColor = _visualSettings.StationFillColor;
        Color portColor = _visualSettings.InkColor;

        switch (visualState)
        {
            case EProcessVisualState.Selected:
                strokeColor = _visualSettings.SelectedColor;
                break;

            case EProcessVisualState.Waiting:
                strokeColor = _visualSettings.WaitingColor;
                break;

            case EProcessVisualState.Failed:
                strokeColor = _visualSettings.BlockedColor;
                break;

            case EProcessVisualState.Completed:
                strokeColor = _visualSettings.CompletedColor;
                fillColor = LevelPlayViewShapeUtility.WithAlpha(_visualSettings.StationFillColor, 0.44f);
                portColor = LevelPlayViewShapeUtility.WithAlpha(_visualSettings.InkColor, 0.42f);
                break;
        }

        _shadowDisc.Color = LevelPlayViewShapeUtility.WithAlpha(Color.black, 0.26f);
        _strokeDisc.Color = strokeColor;
        _fillDisc.Color = fillColor;
        _portDisc.Color = portColor;
    }

    private void RefreshRequiredColorTray(IReadOnlyList<int> slotIdList,
                                          IReadOnlyList<int> requiredColorIdList)
    {
        int colorCount = requiredColorIdList != null ? Mathf.Min(requiredColorIdList.Count, MaxSlotCount) : 0;

        for (int i = 0; i < MaxSlotCount; i++)
        {
            ProcessTrayVariant variant = _trayVariantArray[i];

            if (variant == null)
            {
                continue;
            }

            bool isActiveVariant = i + 1 == colorCount;
            variant.Root.gameObject.SetActive(isActiveVariant);

            if (!isActiveVariant)
            {
                continue;
            }

            for (int chipIndex = 0; chipIndex < variant.ChipArray.Length; chipIndex++)
            {
                ProcessChip chip = variant.ChipArray[chipIndex];
                bool isChipActive = chipIndex < colorCount;
                chip.Root.gameObject.SetActive(isChipActive);

                if (!isChipActive)
                {
                    continue;
                }

                chip.Rim.Color = LevelPlayViewShapeUtility.WithAlpha(Color.white, 0.82f);
                chip.Fill.Color = _visualSettings.GetColor(requiredColorIdList[chipIndex]);
                chip.Input.Initialize(slotIdList != null && chipIndex < slotIdList.Count ? slotIdList[chipIndex] : chipIndex);
            }
        }
    }

    private void RefreshStateOverlay(EProcessVisualState visualState)
    {
        for (int i = 0; i < _stateOverlayDiscArray.Length; i++)
        {
            Disc overlay = _stateOverlayDiscArray[i];

            if (overlay != null)
            {
                overlay.gameObject.SetActive(false);
            }
        }

        int stateIndex = (int)visualState;

        if (stateIndex <= 0 || stateIndex >= _stateOverlayDiscArray.Length)
        {
            return;
        }

        Disc activeOverlay = _stateOverlayDiscArray[stateIndex];

        if (activeOverlay == null)
        {
            return;
        }

        activeOverlay.gameObject.SetActive(true);
        activeOverlay.Color = GetStateOverlayColor(visualState);
    }

    private Color GetStateOverlayColor(EProcessVisualState visualState)
    {
        switch (visualState)
        {
            case EProcessVisualState.Selected:
                return LevelPlayViewShapeUtility.WithAlpha(_visualSettings.SelectedColor, 0.72f);

            case EProcessVisualState.Waiting:
                return LevelPlayViewShapeUtility.WithAlpha(_visualSettings.WaitingColor, 0.72f);

            case EProcessVisualState.Failed:
                return LevelPlayViewShapeUtility.WithAlpha(_visualSettings.BlockedColor, 0.82f);

            case EProcessVisualState.Completed:
                return LevelPlayViewShapeUtility.WithAlpha(_visualSettings.CompletedColor, 0.72f);

            default:
                return Color.clear;
        }
    }

    private void HandleSlotPressed(int slotId)
    {
        OnSlotPressed?.Invoke(_processId, slotId);
    }

    private void HandleSlotLongPressed(int slotId)
    {
        OnSlotLongPressed?.Invoke(_processId, slotId);
    }

    private void RefreshWaitingPulse(bool shouldPulse)
    {
        CacheInitialFeedbackState();

        if (_portDisc == null || _visualSettings == null || shouldPulse == _isWaitingPulseActive)
        {
            return;
        }

        _isWaitingPulseActive = shouldPulse;
        Transform portTransform = _portDisc.transform;
        portTransform.DOKill();
        portTransform.localScale = _portInitialLocalScale;

        if (!shouldPulse)
        {
            return;
        }

        float halfDuration = Mathf.Max(0.01f, _visualSettings.WaitingPortPulseDuration * 0.5f);
        portTransform.DOScale(_portInitialLocalScale * _visualSettings.WaitingPortPulseScale, halfDuration)
                     .SetEase(Ease.InOutSine)
                     .SetLoops(-1, LoopType.Yoyo)
                     .SetUpdate(true);
    }

    private void StopFeedback()
    {
        if (_portDisc != null)
        {
            _portDisc.transform.DOKill();
            _portDisc.transform.localScale = _portInitialLocalScale;
        }

        if (_strokeDisc != null)
        {
            _strokeDisc.transform.DOKill();
        }

        _isWaitingPulseActive = false;
    }

    private void CacheInitialFeedbackState()
    {
        if (_portDisc != null && _portInitialLocalScale == Vector3.zero)
        {
            _portInitialLocalScale = _portDisc.transform.localScale;
        }
    }

    private void CacheReferences()
    {
        _shadowDisc ??= FindComponent<Disc>("Background/Shadow");
        _strokeDisc ??= FindComponent<Disc>("Background/Stroke");
        _fillDisc ??= FindComponent<Disc>("Background/Fill");
        _portDisc ??= FindComponent<Disc>("Port/Disc");
        _requiredColorTrayRoot ??= transform.Find("RequiredColorTray");
        _stateOverlayRoot ??= transform.Find("StateOverlay");

        CacheStateOverlay(EProcessVisualState.Selected, "Selected");
        CacheStateOverlay(EProcessVisualState.Waiting, "Waiting");
        CacheStateOverlay(EProcessVisualState.Failed, "Failed");
        CacheStateOverlay(EProcessVisualState.Completed, "Completed");

        for (int slotCount = 1; slotCount <= MaxSlotCount; slotCount++)
        {
            int variantIndex = slotCount - 1;

            if (_trayVariantArray[variantIndex] != null)
            {
                continue;
            }

            Transform root = transform.Find("RequiredColorTray/Variants/SlotCount_" + slotCount);

            if (root == null)
            {
                continue;
            }

            ProcessChip[] chipArray = new ProcessChip[slotCount];

            for (int chipIndex = 1; chipIndex <= slotCount; chipIndex++)
            {
                Transform chipRoot = root.Find("ColorChips/Chip_" + chipIndex);

                if (chipRoot == null)
                {
                    continue;
                }

                ProcessSlotInput input = chipRoot.GetComponent<ProcessSlotInput>();

                if (input != null)
                {
                    input.OnPressed -= HandleSlotPressed;
                    input.OnPressed += HandleSlotPressed;
                    input.OnLongPressed -= HandleSlotLongPressed;
                    input.OnLongPressed += HandleSlotLongPressed;
                }

                chipArray[chipIndex - 1] = new ProcessChip(chipRoot,
                                                            chipRoot.Find("Rim")?.GetComponent<Disc>(),
                                                            chipRoot.Find("Fill")?.GetComponent<Disc>(),
                                                            input);
            }

            _trayVariantArray[variantIndex] = new ProcessTrayVariant(root, chipArray);
        }
    }

    private void CacheStateOverlay(EProcessVisualState visualState, string path)
    {
        int stateIndex = (int)visualState;

        if (_stateOverlayDiscArray[stateIndex] == null)
        {
            _stateOverlayDiscArray[stateIndex] = FindComponent<Disc>("StateOverlay/" + path);
        }
    }

    private T FindComponent<T>(string path) where T : Component
    {
        Transform target = transform.Find(path);
        return target != null ? target.GetComponent<T>() : null;
    }

    private bool HasRequiredReferences()
    {
        return _visualSettings != null &&
               _shadowDisc != null &&
               _strokeDisc != null &&
               _fillDisc != null &&
               _portDisc != null &&
               _requiredColorTrayRoot != null &&
               _stateOverlayRoot != null;
    }

    private sealed class ProcessTrayVariant
    {
        public readonly Transform Root;
        public readonly ProcessChip[] ChipArray;

        public ProcessTrayVariant(Transform root, ProcessChip[] chipArray)
        {
            Root = root;
            ChipArray = chipArray;
        }
    }

    private sealed class ProcessChip
    {
        public readonly Transform Root;
        public readonly Disc Rim;
        public readonly Disc Fill;
        public readonly ProcessSlotInput Input;

        public ProcessChip(Transform root, Disc rim, Disc fill, ProcessSlotInput input)
        {
            Root = root;
            Rim = rim;
            Fill = fill;
            Input = input;
        }
    }
}

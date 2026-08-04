using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public sealed class LevelPlayHudView : MonoBehaviour
{
    private const int SpeedButtonCount = 3;

    public event Action OnRunPressed;
    public event Action OnRestartPressed;
    public event Action OnPausePressed;
    public event Action<float> OnPlaybackSpeedPressed;
    public event Action<int> OnRequiredColorPressed;
    public event Action<int, Vector2> OnRequiredColorDropped;

    [SerializeField]
    private VisualSettingsSO _visualSettings;

    [SerializeField]
    private GameObject _planningRoot;

    [SerializeField]
    private GameObject _playbackRoot;

    [SerializeField]
    private GameObject _colorRailRoot;

    [SerializeField]
    private GameObject _resultRoot;

    [SerializeField]
    private TMP_Text _stageLabel;

    [SerializeField]
    private TMP_Text _planningProgressLabel;

    [SerializeField]
    private TMP_Text _playbackRoundLabel;

    [SerializeField]
    private TMP_Text _pauseLabel;

    [SerializeField]
    private TMP_Text _resultTitleLabel;

    [SerializeField]
    private TMP_Text _resultRoundLabel;

    [SerializeField]
    private Button _runButton;

    [SerializeField]
    private Button _restartButton;

    [SerializeField]
    private Button _pauseButton;

    [SerializeField]
    private Button _resultRestartButton;

    [SerializeField]
    private Button[] _speedButtonArray;

    [SerializeField]
    private CanvasDiscGraphic[] _speedSelectionRingArray;

    [SerializeField]
    private HudColorChipReference[] _colorChipReferenceArray;

    [SerializeField]
    private GameObject[] _resultStarRootArray;

    [SerializeField]
    private RectTransform _dragPreviewRoot;

    [SerializeField]
    private CanvasDiscGraphic _dragPreviewFill;

    private LevelPlayHudVisualData _currentVisualData;
    private readonly UnityAction[] _speedPressedActionArray = new UnityAction[SpeedButtonCount];

    private void OnEnable()
    {
        SubscribeInput();
    }

    private void OnDisable()
    {
        HideDragPreview();
        UnsubscribeInput();
    }

    public void Refresh(LevelPlayHudVisualData data)
    {
        if (data == null || _visualSettings == null || !HasRequiredReferences())
        {
            return;
        }

        _currentVisualData = data;

        bool isResultVisible = data.ResultState != ELevelPlayResultVisualState.None;
        bool isPlanningVisible = data.IsPlanning && !isResultVisible;
        bool isPlaybackVisible = !data.IsPlanning && data.IsPlaybackRunning && !isResultVisible;

        _stageLabel.text = "LEVEL " + data.LevelId.ToString("00");
        _planningProgressLabel.text = "RESERVED " + data.ReservedSlotCount + " / " + data.TotalSlotCount;
        _playbackRoundLabel.text = data.CurrentRoundIndex > 0 ? "ROUND " + data.CurrentRoundIndex : "READY";
        _planningProgressLabel.gameObject.SetActive(data.IsPlanning && !isResultVisible);
        _playbackRoundLabel.gameObject.SetActive(!data.IsPlanning && !isResultVisible);
        _planningRoot.SetActive(isPlanningVisible);
        _playbackRoot.SetActive(isPlaybackVisible);
        _resultRoot.SetActive(isResultVisible);
        _runButton.interactable = data.CanStartSimulation;
        _pauseLabel.text = data.IsPlaybackPaused ? "RESUME" : "PAUSE";

        RefreshColorRail(data, isPlanningVisible);
        RefreshSpeedSelection(data.PlaybackSpeed);
        RefreshResult(data);

        if (!isPlanningVisible)
        {
            HideDragPreview();
        }
    }

    public void ConfigurePrefabReferences(VisualSettingsSO visualSettings,
                                          GameObject planningRoot,
                                          GameObject playbackRoot,
                                          GameObject colorRailRoot,
                                          GameObject resultRoot,
                                          TMP_Text stageLabel,
                                          TMP_Text planningProgressLabel,
                                          TMP_Text playbackRoundLabel,
                                          TMP_Text pauseLabel,
                                          TMP_Text resultTitleLabel,
                                          TMP_Text resultRoundLabel,
                                          Button runButton,
                                          Button restartButton,
                                          Button pauseButton,
                                          Button resultRestartButton,
                                          Button[] speedButtonArray,
                                          CanvasDiscGraphic[] speedSelectionRingArray,
                                          HudColorChipReference[] colorChipReferenceArray,
                                          GameObject[] resultStarRootArray,
                                          RectTransform dragPreviewRoot,
                                          CanvasDiscGraphic dragPreviewFill)
    {
        _visualSettings = visualSettings;
        _planningRoot = planningRoot;
        _playbackRoot = playbackRoot;
        _colorRailRoot = colorRailRoot;
        _resultRoot = resultRoot;
        _stageLabel = stageLabel;
        _planningProgressLabel = planningProgressLabel;
        _playbackRoundLabel = playbackRoundLabel;
        _pauseLabel = pauseLabel;
        _resultTitleLabel = resultTitleLabel;
        _resultRoundLabel = resultRoundLabel;
        _runButton = runButton;
        _restartButton = restartButton;
        _pauseButton = pauseButton;
        _resultRestartButton = resultRestartButton;
        _speedButtonArray = speedButtonArray;
        _speedSelectionRingArray = speedSelectionRingArray;
        _colorChipReferenceArray = colorChipReferenceArray;
        _resultStarRootArray = resultStarRootArray;
        _dragPreviewRoot = dragPreviewRoot;
        _dragPreviewFill = dragPreviewFill;
    }

    private void RefreshColorRail(LevelPlayHudVisualData data, bool isPlanningVisible)
    {
        int colorCount = Mathf.Min(data.SelectedProcessRequiredColorIdArray.Length, _colorChipReferenceArray.Length);
        _colorRailRoot.SetActive(isPlanningVisible && colorCount > 0);

        for (int index = 0; index < _colorChipReferenceArray.Length; index++)
        {
            HudColorChipReference chip = _colorChipReferenceArray[index];
            bool isActive = index < colorCount;
            chip.Root.SetActive(isActive);

            if (!isActive)
            {
                continue;
            }

            chip.Fill.color = _visualSettings.GetColor(data.SelectedProcessRequiredColorIdArray[index]);
            bool isSelected = index == data.SelectedRequiredColorIndex;
            chip.SelectionRing.gameObject.SetActive(isSelected);
            chip.SelectionRing.color = _visualSettings.SelectedColor;
        }
    }

    private void RefreshSpeedSelection(float speed)
    {
        for (int index = 0; index < _speedSelectionRingArray.Length; index++)
        {
            _speedSelectionRingArray[index].gameObject.SetActive(Mathf.Approximately(speed, GetSpeed(index)));
        }
    }

    private void RefreshResult(LevelPlayHudVisualData data)
    {
        if (data.ResultState == ELevelPlayResultVisualState.None)
        {
            return;
        }

        bool isClear = data.ResultState == ELevelPlayResultVisualState.Clear;
        _resultTitleLabel.text = isClear ? "CLEAR" : data.ResultState == ELevelPlayResultVisualState.Deadlocked ? "DEADLOCK" : "FAILED";
        _resultTitleLabel.color = isClear ? _visualSettings.CompletedColor : _visualSettings.BlockedColor;
        _resultRoundLabel.gameObject.SetActive(isClear);
        _resultRoundLabel.text = "ROUND " + data.ResultRoundCount;

        for (int index = 0; index < _resultStarRootArray.Length; index++)
        {
            _resultStarRootArray[index].SetActive(isClear && index < data.ResultStarCount);
        }
    }

    private void SubscribeInput()
    {
        if (_runButton == null)
        {
            return;
        }

        _runButton.onClick.AddListener(HandleRunPressed);
        _restartButton.onClick.AddListener(HandleRestartPressed);
        _pauseButton.onClick.AddListener(HandlePausePressed);
        _resultRestartButton.onClick.AddListener(HandleRestartPressed);

        for (int index = 0; index < _speedButtonArray.Length; index++)
        {
            int capturedIndex = index;
            _speedPressedActionArray[index] ??= () => HandleSpeedPressed(capturedIndex);
            _speedButtonArray[index].onClick.AddListener(_speedPressedActionArray[index]);
        }

        for (int index = 0; index < _colorChipReferenceArray.Length; index++)
        {
            HudColorChipInput input = _colorChipReferenceArray[index].Input;
            input.OnPressed += HandleRequiredColorPressed;
            input.OnDragStarted += HandleRequiredColorDragStarted;
            input.OnDragged += HandleRequiredColorDragged;
            input.OnDropped += HandleRequiredColorDropped;
        }
    }

    private void UnsubscribeInput()
    {
        if (_runButton == null)
        {
            return;
        }

        _runButton.onClick.RemoveListener(HandleRunPressed);
        _restartButton.onClick.RemoveListener(HandleRestartPressed);
        _pauseButton.onClick.RemoveListener(HandlePausePressed);
        _resultRestartButton.onClick.RemoveListener(HandleRestartPressed);

        for (int index = 0; index < _speedButtonArray.Length; index++)
        {
            _speedButtonArray[index].onClick.RemoveListener(_speedPressedActionArray[index]);
        }

        for (int index = 0; index < _colorChipReferenceArray.Length; index++)
        {
            HudColorChipInput input = _colorChipReferenceArray[index].Input;
            input.OnPressed -= HandleRequiredColorPressed;
            input.OnDragStarted -= HandleRequiredColorDragStarted;
            input.OnDragged -= HandleRequiredColorDragged;
            input.OnDropped -= HandleRequiredColorDropped;
        }
    }

    private void HandleRunPressed()
    {
        OnRunPressed?.Invoke();
    }

    private void HandleRestartPressed()
    {
        OnRestartPressed?.Invoke();
    }

    private void HandlePausePressed()
    {
        OnPausePressed?.Invoke();
    }

    private void HandleSpeedPressed(int index)
    {
        OnPlaybackSpeedPressed?.Invoke(GetSpeed(index));
    }

    private void HandleRequiredColorPressed(int colorIndex)
    {
        OnRequiredColorPressed?.Invoke(colorIndex);
    }

    private void HandleRequiredColorDropped(int colorIndex, Vector2 screenPosition)
    {
        HideDragPreview();
        OnRequiredColorDropped?.Invoke(colorIndex, screenPosition);
    }

    private void HandleRequiredColorDragStarted(int colorIndex, Vector2 screenPosition)
    {
        if (_currentVisualData == null || colorIndex < 0 || colorIndex >= _currentVisualData.SelectedProcessRequiredColorIdArray.Length)
        {
            return;
        }

        _dragPreviewFill.color = _visualSettings.GetColor(_currentVisualData.SelectedProcessRequiredColorIdArray[colorIndex]);
        _dragPreviewRoot.gameObject.SetActive(true);
        SetDragPreviewPosition(screenPosition);
    }

    private void HandleRequiredColorDragged(Vector2 screenPosition)
    {
        SetDragPreviewPosition(screenPosition);
    }

    private void SetDragPreviewPosition(Vector2 screenPosition)
    {
        if (_dragPreviewRoot == null || !_dragPreviewRoot.gameObject.activeSelf)
        {
            return;
        }

        RectTransform canvasRoot = (RectTransform)transform;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRoot, screenPosition, null, out Vector2 localPosition))
        {
            _dragPreviewRoot.anchoredPosition = localPosition;
        }
    }

    private void HideDragPreview()
    {
        if (_dragPreviewRoot != null)
        {
            _dragPreviewRoot.gameObject.SetActive(false);
        }
    }

    private static float GetSpeed(int index)
    {
        return index == 0 ? 0.5f : index == 1 ? 1f : 2f;
    }

    private bool HasRequiredReferences()
    {
        if (_planningRoot == null || _playbackRoot == null || _colorRailRoot == null || _resultRoot == null ||
            _stageLabel == null || _planningProgressLabel == null || _playbackRoundLabel == null || _pauseLabel == null ||
            _resultTitleLabel == null || _resultRoundLabel == null || _runButton == null || _restartButton == null ||
            _pauseButton == null || _resultRestartButton == null || _speedButtonArray == null ||
            _speedSelectionRingArray == null || _colorChipReferenceArray == null || _resultStarRootArray == null ||
            _dragPreviewRoot == null || _dragPreviewFill == null ||
            _speedButtonArray.Length != SpeedButtonCount || _speedSelectionRingArray.Length != SpeedButtonCount)
        {
            return false;
        }

        for (int index = 0; index < SpeedButtonCount; index++)
        {
            if (_speedButtonArray[index] == null || _speedSelectionRingArray[index] == null)
            {
                return false;
            }
        }

        for (int index = 0; index < _colorChipReferenceArray.Length; index++)
        {
            HudColorChipReference chip = _colorChipReferenceArray[index];

            if (chip == null || chip.Root == null || chip.Fill == null || chip.SelectionRing == null || chip.Input == null)
            {
                return false;
            }
        }

        return true;
    }
}

[Serializable]
public sealed class HudColorChipReference
{
    [SerializeField]
    private GameObject _root;

    [SerializeField]
    private CanvasDiscGraphic _fill;

    [SerializeField]
    private CanvasDiscGraphic _selectionRing;

    public GameObject Root => _root;
    public CanvasDiscGraphic Fill => _fill;
    public CanvasDiscGraphic SelectionRing => _selectionRing;
    public HudColorChipInput Input => _root != null ? _root.GetComponent<HudColorChipInput>() : null;

    public HudColorChipReference(GameObject root, CanvasDiscGraphic fill, CanvasDiscGraphic selectionRing)
    {
        _root = root;
        _fill = fill;
        _selectionRing = selectionRing;
    }
}

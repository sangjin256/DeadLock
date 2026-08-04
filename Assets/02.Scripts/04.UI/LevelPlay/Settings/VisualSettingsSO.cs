using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "VisualSettings_Default", menuName = "DeadLock/Visuals/Visual Settings")]
public sealed class VisualSettingsSO : ScriptableObject
{
    [Header("Color Palette")]
    [SerializeField]
    private List<VisualColorEntry> _colorEntryList = new();
    public IReadOnlyList<VisualColorEntry> ColorEntryList => _colorEntryList;

    [Header("Neutral Colors")]
    [SerializeField]
    private Color _inkColor = new Color(0.055f, 0.065f, 0.075f, 1f);
    public Color InkColor => _inkColor;

    [SerializeField]
    private Color _stationFillColor = new Color(0.91f, 0.94f, 0.95f, 1f);
    public Color StationFillColor => _stationFillColor;

    [SerializeField]
    private Color _mutedColor = new Color(0.72f, 0.78f, 0.80f, 1f);
    public Color MutedColor => _mutedColor;

    [SerializeField]
    private Color _missingColor = Color.magenta;
    public Color MissingColor => _missingColor;

    [Header("State Colors")]
    [SerializeField]
    private Color _selectedColor = new Color(0.18f, 0.78f, 0.68f, 1f);
    public Color SelectedColor => _selectedColor;

    [SerializeField]
    private Color _waitingColor = new Color(0.98f, 0.68f, 0.20f, 1f);
    public Color WaitingColor => _waitingColor;

    [SerializeField]
    private Color _blockedColor = new Color(0.93f, 0.36f, 0.36f, 1f);
    public Color BlockedColor => _blockedColor;

    [SerializeField]
    private Color _completedColor = new Color(0.72f, 0.82f, 0.20f, 1f);
    public Color CompletedColor => _completedColor;

    [SerializeField]
    private Color _relayLinkColor = new Color(0.47f, 0.35f, 0.78f, 1f);
    public Color RelayLinkColor => _relayLinkColor;

    [SerializeField]
    private Color _relayTransferColor = new Color(0.93f, 0.36f, 0.36f, 1f);
    public Color RelayTransferColor => _relayTransferColor;

    [Header("Feedback")]
    [SerializeField]
    private float _waitingPortPulseDuration = 0.65f;
    public float WaitingPortPulseDuration => _waitingPortPulseDuration;

    [SerializeField]
    private float _waitingPortPulseScale = 1.18f;
    public float WaitingPortPulseScale => _waitingPortPulseScale;

    [SerializeField]
    private float _invalidInputShakeDuration = 0.16f;
    public float InvalidInputShakeDuration => _invalidInputShakeDuration;

    [SerializeField]
    private float _invalidInputShakeDistance = 0.06f;
    public float InvalidInputShakeDistance => _invalidInputShakeDistance;

    private readonly Dictionary<int, Color> _colorByIdDict = new();

    public bool TryGetColor(int colorId, out Color color)
    {
        EnsureColorCache();

        if (colorId > 0 && _colorByIdDict.TryGetValue(colorId, out color))
        {
            return true;
        }

        color = colorId <= 0 ? _mutedColor : _missingColor;
        return false;
    }

    public Color GetColor(int colorId)
    {
        TryGetColor(colorId, out Color color);
        return color;
    }

    private void OnEnable()
    {
        RebuildColorCache();
    }

    private void OnValidate()
    {
        RebuildColorCache();
    }

    private void EnsureColorCache()
    {
        if (_colorByIdDict.Count == 0 && _colorEntryList.Count > 0)
        {
            RebuildColorCache();
        }
    }

    private void RebuildColorCache()
    {
        _colorByIdDict.Clear();

        for (int i = 0; i < _colorEntryList.Count; i++)
        {
            VisualColorEntry colorEntry = _colorEntryList[i];

            if (colorEntry == null || colorEntry.ColorId <= 0 || _colorByIdDict.ContainsKey(colorEntry.ColorId))
            {
                continue;
            }

            _colorByIdDict.Add(colorEntry.ColorId, colorEntry.Color);
        }
    }
}

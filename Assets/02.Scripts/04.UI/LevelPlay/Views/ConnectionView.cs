using Shapes;
using UnityEngine;

public sealed class ConnectionView : MonoBehaviour
{
    [SerializeField]
    private VisualSettingsSO _visualSettings;

    [SerializeField]
    private Line _shadowLine;

    [SerializeField]
    private Line _trackLine;

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

    public void Refresh(Vector2 start,
                        Vector2 end,
                        int colorId,
                        EConnectionVisualState visualState)
    {
        CacheReferences();

        if (_visualSettings == null || _shadowLine == null || _trackLine == null)
        {
            return;
        }

        Color trackColor = GetTrackColor(colorId, visualState);
        _shadowLine.Start = LevelPlayViewShapeUtility.ToVector3(start);
        _shadowLine.End = LevelPlayViewShapeUtility.ToVector3(end);
        _shadowLine.Color = LevelPlayViewShapeUtility.WithAlpha(Color.black, 0.34f);
        _trackLine.Start = LevelPlayViewShapeUtility.ToVector3(start);
        _trackLine.End = LevelPlayViewShapeUtility.ToVector3(end);
        _trackLine.Color = trackColor;
    }

    private Color GetTrackColor(int colorId, EConnectionVisualState visualState)
    {
        switch (visualState)
        {
            case EConnectionVisualState.Planned:
                return LevelPlayViewShapeUtility.WithAlpha(_visualSettings.GetColor(colorId), 0.68f);

            case EConnectionVisualState.Waiting:
                return _visualSettings.WaitingColor;

            case EConnectionVisualState.Blocked:
                return _visualSettings.BlockedColor;

            case EConnectionVisualState.Completed:
                return LevelPlayViewShapeUtility.WithAlpha(_visualSettings.CompletedColor, 0.58f);

            default:
                return _visualSettings.GetColor(colorId);
        }
    }

    private void CacheReferences()
    {
        if (_shadowLine != null)
        {
            return;
        }

        _shadowLine = FindComponent<Line>("Background/Shadow");
        _trackLine = FindComponent<Line>("Track/Line");
    }

    private T FindComponent<T>(string path) where T : Component
    {
        Transform target = transform.Find(path);
        return target != null ? target.GetComponent<T>() : null;
    }
}

using Shapes;
using UnityEngine;

public sealed class RelayView : MonoBehaviour
{
    [SerializeField]
    private VisualSettingsSO _visualSettings;

    [SerializeField]
    private float _stubLength = 0.60f;

    [SerializeField]
    private Line _startStubLine;

    [SerializeField]
    private Line _endStubLine;

    [SerializeField]
    private Disc _linkStartRing;

    [SerializeField]
    private Disc _linkEndRing;

    [SerializeField]
    private Disc _transferSenderDisc;

    [SerializeField]
    private Disc _transferReceiverRing;

    [SerializeField]
    private Disc _transferDirectionDisc;

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
                        ERelayVisualType visualType,
                        bool isHighlighted)
    {
        CacheReferences();

        if (!HasRequiredReferences())
        {
            return;
        }

        Vector2 direction = end - start;
        float length = direction.magnitude;

        if (length <= Mathf.Epsilon)
        {
            SetVisualActive(false);
            return;
        }

        SetVisualActive(true);
        direction /= length;
        float stubLength = Mathf.Min(_stubLength, length * 0.5f);
        Vector2 visibleStart = start;
        Vector2 visibleEnd = end;
        Vector2 startStubEnd = visibleStart + direction * stubLength;
        Vector2 endStubEnd = visibleEnd - direction * stubLength;
        Color relayColor = visualType == ERelayVisualType.Link
            ? _visualSettings.RelayLinkColor
            : _visualSettings.RelayTransferColor;
        float alpha = isHighlighted ? 1f : 0.52f;

        RefreshStub(_startStubLine, visibleStart, startStubEnd, relayColor, alpha);
        RefreshStub(_endStubLine, visibleEnd, endStubEnd, relayColor, alpha);
        RefreshEndpoints(visualType, visibleStart, startStubEnd, endStubEnd, relayColor, alpha);
    }

    private void RefreshStub(Line line, Vector2 start, Vector2 end, Color color, float alpha)
    {
        line.Start = LevelPlayViewShapeUtility.ToVector3(start);
        line.End = LevelPlayViewShapeUtility.ToVector3(end);
        line.Color = LevelPlayViewShapeUtility.WithAlpha(color, alpha * 0.75f);
    }

    private void RefreshEndpoints(ERelayVisualType visualType,
                                  Vector2 visibleStart,
                                  Vector2 startStubEnd,
                                  Vector2 endStubEnd,
                                  Color relayColor,
                                  float alpha)
    {
        bool isLink = visualType == ERelayVisualType.Link;
        _linkStartRing.gameObject.SetActive(isLink);
        _linkEndRing.gameObject.SetActive(isLink);
        _transferSenderDisc.gameObject.SetActive(!isLink);
        _transferReceiverRing.gameObject.SetActive(!isLink);
        _transferDirectionDisc.gameObject.SetActive(!isLink);

        if (isLink)
        {
            RefreshRing(_linkStartRing, startStubEnd, relayColor, alpha);
            RefreshRing(_linkEndRing, endStubEnd, relayColor, alpha);
            return;
        }

        _transferSenderDisc.transform.localPosition = LevelPlayViewShapeUtility.ToVector3(startStubEnd);
        _transferSenderDisc.Color = LevelPlayViewShapeUtility.WithAlpha(relayColor, alpha * 0.82f);
        RefreshRing(_transferReceiverRing, endStubEnd, relayColor, alpha);
        _transferDirectionDisc.transform.localPosition = LevelPlayViewShapeUtility.ToVector3(Vector2.Lerp(visibleStart, startStubEnd, 0.62f));
        _transferDirectionDisc.Color = LevelPlayViewShapeUtility.WithAlpha(relayColor, alpha * 0.72f);
    }

    private void RefreshRing(Disc ring, Vector2 position, Color color, float alpha)
    {
        ring.transform.localPosition = LevelPlayViewShapeUtility.ToVector3(position);
        ring.Color = LevelPlayViewShapeUtility.WithAlpha(color, alpha * 0.90f);
    }

    private void CacheReferences()
    {
        if (_startStubLine != null)
        {
            return;
        }

        _startStubLine = FindComponent<Line>("Links/StartStub");
        _endStubLine = FindComponent<Line>("Links/EndStub");
        _linkStartRing = FindComponent<Disc>("Endpoints/LinkStart");
        _linkEndRing = FindComponent<Disc>("Endpoints/LinkEnd");
        _transferSenderDisc = FindComponent<Disc>("Endpoints/TransferSender");
        _transferReceiverRing = FindComponent<Disc>("Endpoints/TransferReceiver");
        _transferDirectionDisc = FindComponent<Disc>("Endpoints/TransferDirection");
    }

    private T FindComponent<T>(string path) where T : Component
    {
        Transform target = transform.Find(path);
        return target != null ? target.GetComponent<T>() : null;
    }

    private bool HasRequiredReferences()
    {
        return _visualSettings != null &&
               _startStubLine != null &&
               _endStubLine != null &&
               _linkStartRing != null &&
               _linkEndRing != null &&
               _transferSenderDisc != null &&
               _transferReceiverRing != null &&
               _transferDirectionDisc != null;
    }

    private void SetVisualActive(bool isActive)
    {
        _startStubLine.gameObject.SetActive(isActive);
        _endStubLine.gameObject.SetActive(isActive);
        _linkStartRing.gameObject.SetActive(isActive);
        _linkEndRing.gameObject.SetActive(isActive);
        _transferSenderDisc.gameObject.SetActive(isActive);
        _transferReceiverRing.gameObject.SetActive(isActive);
        _transferDirectionDisc.gameObject.SetActive(isActive);
    }
}

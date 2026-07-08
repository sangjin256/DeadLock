using System;
using UnityEngine;

[Serializable]
public sealed class LevelRelayData
{
    [SerializeField]
    private int _id;
    public int Id => _id;

    [SerializeField]
    private ERelayType _relayType;
    public ERelayType RelayType => _relayType;

    [SerializeField]
    private int _firstResourceId;
    public int FirstResourceId => _firstResourceId;

    [SerializeField]
    private int _secondResourceId;
    public int SecondResourceId => _secondResourceId;

    [SerializeField]
    private int _senderResourceId;
    public int SenderResourceId => _senderResourceId;
}

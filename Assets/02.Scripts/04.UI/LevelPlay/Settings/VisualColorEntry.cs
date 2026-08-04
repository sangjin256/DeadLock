using System;
using UnityEngine;

[Serializable]
public sealed class VisualColorEntry
{
    [SerializeField]
    private int _colorId;
    public int ColorId => _colorId;

    [SerializeField]
    private Color _color;
    public Color Color => _color;
}

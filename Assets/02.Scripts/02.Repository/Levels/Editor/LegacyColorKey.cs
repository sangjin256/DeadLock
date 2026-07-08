using System;
using UnityEngine;

internal readonly struct LegacyColorKey : IEquatable<LegacyColorKey>
{
    public readonly byte R;
    public readonly byte G;
    public readonly byte B;
    public readonly byte A;

    public LegacyColorKey(Color color)
    {
        R = ToByte(color.r);
        G = ToByte(color.g);
        B = ToByte(color.b);
        A = ToByte(color.a);
    }

    public bool Equals(LegacyColorKey other)
    {
        return R == other.R &&
               G == other.G &&
               B == other.B &&
               A == other.A;
    }

    public override bool Equals(object obj)
    {
        return obj is LegacyColorKey other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + R;
            hash = hash * 31 + G;
            hash = hash * 31 + B;
            hash = hash * 31 + A;
            return hash;
        }
    }

    public string ToHexString()
    {
        return $"#{R:X2}{G:X2}{B:X2}{A:X2}";
    }

    private static byte ToByte(float value)
    {
        return (byte)Mathf.Clamp(Mathf.RoundToInt(value * 255f), 0, 255);
    }
}

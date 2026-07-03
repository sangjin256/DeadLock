using System;

public readonly struct ColorId : IEquatable<ColorId>
{
    public static readonly ColorId None = new ColorId(0);

    public readonly int Value;

    public ColorId(int value)
    {
        Value = value;
    }

    public bool Equals(ColorId other) => Value == other.Value;
    public override bool Equals(object obj) => obj is ColorId other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
}

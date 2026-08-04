using UnityEngine;

public static class LevelPlayViewShapeUtility
{
    public static Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }

    public static Vector3 ToVector3(Vector2 value, float z = 0f)
    {
        return new Vector3(value.x, value.y, z);
    }
}

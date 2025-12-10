using UnityEngine;
using System;

public static class BezierMath
{
    public static Vector2 CalculateBezierPoint(Vector2[] p, float t)
    {
        return new(
            MathF.Pow(1 - t, 2f) * p[0].x + 2 * t * (1-t) * p[1].x + MathF.Pow(t, 2f) * p[2].x,
            MathF.Pow(1 - t, 2f) * p[0].y + 2 * t * (1-t) * p[1].y + MathF.Pow(t, 2f) * p[2].y
        );
    }
}
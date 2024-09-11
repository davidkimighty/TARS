using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class MovementUtils
{
    public static Vector3 CalculateCenterPoint(List<Transform> points)
    {
        return points.Aggregate(Vector3.zero, (current, point) => current + point.position) / points.Count;
    }

    public static Quaternion ShortestRotation(Quaternion a, Quaternion b)
    {
        if (Quaternion.Dot(a, b) < 0)
            return a * Quaternion.Inverse(Multiply(b, -1f));
        return a * Quaternion.Inverse(b);
    }

    public static Quaternion Multiply(Quaternion q, float scalar)
    {
        return new Quaternion(q.x * scalar, q.y * scalar, q.z * scalar, q.w * scalar);
    }
}

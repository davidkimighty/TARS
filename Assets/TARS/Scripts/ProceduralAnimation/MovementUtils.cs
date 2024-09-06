using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class MovementUtils
{
    public static Vector3 CalculateCenterPoint(List<Transform> points)
    {
        return points.Aggregate(Vector3.zero, (current, point) => current + point.position) / points.Count;
    }
}

using UnityEngine;

namespace ScriptBoy.ProceduralBook
{
    static class BezierUtility
    {
        public static Vector3 Evaluate(Vector3 p0, Vector3 p1, Vector3 p2, float t)
        {
            float i = (1 - t);
            return i * i * p0 + 2 * i * t * p1 + t * t * p2;
        }
    }
}
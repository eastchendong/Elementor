using UnityEngine;

namespace ScriptBoy.ProceduralBook
{
    static class MatrixUtility
    {
        public static Ray Transform(Ray ray, Matrix4x4 matrix)
        {
            Vector3 a = ray.origin;
            Vector3 b = ray.origin + ray.direction;
            a = matrix.MultiplyPoint3x4(a);
            b = matrix.MultiplyPoint3x4(b);
            ray.origin = a;
            ray.direction = b - a;
            return ray;
        }
    }
}
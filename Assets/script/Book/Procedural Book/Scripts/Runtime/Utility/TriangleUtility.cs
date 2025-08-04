using System;
using UnityEngine;

namespace ScriptBoy.ProceduralBook
{
    static class TriangleUtility
    {
        public static Vector3 GetNormal(Vector3 a, Vector3 b, Vector3 c)
        {
            float abx = b.x - a.x;
            float aby = b.y - a.y;
            float abz = b.z - a.z;
            float m = 1 / (float)Math.Sqrt(abx * abx + aby * aby + abz * abz);
            abx *= m;
            aby *= m;
            abz *= m;

            float acx = c.x - a.x;
            float acy = c.y - a.y;
            float acz = c.z - a.z;
            m = 1 / (float)Math.Sqrt(acx * acx + acy * acy + acz * acz);
            acx *= m;
            acy *= m;
            acz *= m;

            float x = (aby * acz - abz * acy);
            float y = (abz * acx - abx * acz);
            float z = (abx * acy - aby * acx);
            m = 1 / (float)Math.Sqrt(x * x + y * y + z * z);
            Vector3 n;
            n.x = x * m;
            n.y = y * m;
            n.z = z * m;
            return n;
        }
    }
}
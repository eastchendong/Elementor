using UnityEngine;

namespace ScriptBoy.ProceduralBook
{
    static class EllipseUtility
    {
        public static Vector2 Calmp(Vector2 point, Vector2 ellipseCenter, Vector2 ellipseSize)
        {
            if (IsPointInide(point, ellipseCenter, ellipseSize)) return point;

            return Linecast(new Vector2(point.x, ellipseCenter.y), point, ellipseCenter, ellipseSize);
        }

        static bool IsPointInide(Vector2 point, Vector2 ellipseCenter, Vector2 ellipseSize)
        {
            Vector3 dir = point - ellipseCenter;
            dir.y *= ellipseSize.x / ellipseSize.y;
            return dir.magnitude < ellipseSize.x;
        }

        static Vector2 Linecast(Vector2 lineStart, Vector2 lineEnd, Vector2 ellipseCenter, Vector2 ellipseSize)
        {
            float h = ellipseCenter.x;
            float k = ellipseCenter.y;

            float x1 = lineStart.x;
            float y1 = lineStart.y;
            float x2 = lineEnd.x;
            float y2 = lineEnd.y;

            float a = ellipseSize.x;
            float b = ellipseSize.y;

            float A = 1 / (a * a);
            float B = 1 / (b * b);

            if (Mathf.Abs(x1 - x2) < 1e-5)
            {
                if (x1 >= h - a && x1 <= h + a)
                {
                    var e = new QuadraticEquation(B, -2 * B * k, A * (x1 * x1 - 2 * h * x1 + h * h) + B * k * k - 1);

                    if (e.rootCount == 1)
                    {
                        Vector3 p0 = new Vector3(x1, e.root0);
                        return p0;
                    }
                    else if (e.rootCount == 2)
                    {
                        if (y1 < y2) (e.root0, e.root1) = (e.root1, e.root0);
                        //Vector3 p0 = new Vector3(x1, e.root0);
                        Vector3 p1 = new Vector3(x1, e.root1);
                        return p1;
                    }

                }
            }
            else
            {
                float m = (y2 - y1) / (x2 - x1);
                float c = y1 - m * x1;
                float w = c - k;
                var e = new QuadraticEquation(A + B * m * m, 2 * w * m * B - 2 * h * A, h * h * A + w * w * B - 1);
                if (e.rootCount == 1)
                {
                    Vector3 p0 = new Vector3(e.root0, m * e.root0 + c);
                    return p0;
                }
                else if (e.rootCount == 2)
                {
                    if (x1 < x2) (e.root0, e.root1) = (e.root1, e.root0);
                    //Vector3 p0 = new Vector3(e.root0, m * e.root0 + c);
                    Vector3 p1 = new Vector3(e.root1, m * e.root1 + c);
                    return p1;
                }
            }

            return lineStart;
        }
    }

    struct QuadraticEquation
    {
        public int rootCount;
        public float root0;
        public float root1;

        public QuadraticEquation(float a, float b, float c)
        {
            float delta = b * b - 4 * a * c;
            if (delta < 0)
            {
                rootCount = 0;
                root0 = 0;
                root1 = 0;
            }
            else if (delta == 0)
            {
                rootCount = 1;
                root0 = -b / 2 * a;
                root1 = 0;
            }
            else
            {
                rootCount = 2;
                float s = Mathf.Sqrt(delta);
                root0 = (-b + s) / (2 * a);
                root1 = (-b - s) / (2 * a);
            }
        }
    }
}
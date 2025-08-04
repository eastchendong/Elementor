using UnityEngine;

namespace ScriptBoy.ProceduralBook
{
    static class VectorUtility
    {
        public static Vector3 SmoothDamp(Vector3 current, Vector3 target, ref Vector3 currentVelocity, Vector3 smoothTime)
        {
            current.x = Mathf.SmoothDamp(current.x, target.x, ref currentVelocity.x, smoothTime.x);
            current.y = Mathf.SmoothDamp(current.y, target.y, ref currentVelocity.y, smoothTime.y);
            current.z = Mathf.SmoothDamp(current.z, target.z, ref currentVelocity.z, smoothTime.z);
            return current;
        }

        public static Vector3 SmoothStep(Vector3 from, Vector3 to, Vector3 smoothStep)
        {
            from.x = Mathf.SmoothStep(from.x, to.x, smoothStep.x);
            from.y = Mathf.SmoothStep(from.y, to.y, smoothStep.y);
            from.z = Mathf.SmoothStep(from.z, to.z, smoothStep.z);
            return from;
        }
        
        public static Vector3 MoveTowards(Vector3 current, Vector3 target, Vector3 maxDistanceDelta)
        {
            current.x = Mathf.MoveTowards(current.x, target.x, maxDistanceDelta.x);
            current.y = Mathf.MoveTowards(current.y, target.y, maxDistanceDelta.y);
            current.z = Mathf.MoveTowards(current.z, target.z, maxDistanceDelta.z);
            return current;
        }

        public static Vector3 GetPerpendicularXZ2(Vector3 vector)
        {
            return new Vector3(-vector.z, vector.y, vector.x);
        }

        public static Vector3 XY2XZ(Vector3 v)
        {
            return new Vector3(v.x, 0, v.y);
        }

        public static Vector3 XZ2XY(Vector3 v)
        {
            return new Vector3(v.x, v.z, 0);
        }
    }
}
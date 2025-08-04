using UnityEngine;

namespace ScriptBoy.ProceduralBook
{
    static class ObjectUtility
    {
        public static void Destroy(Object obj)
        {
            if (!obj) return;

#if UNITY_EDITOR
            if (Application.isPlaying)
            {
#endif
                Object.Destroy(obj);
#if UNITY_EDITOR
            }
            else
            {
                Object.DestroyImmediate(obj);
            }
#endif
        }
    }
}
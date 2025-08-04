using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace ScriptBoy.ProceduralBook
{
    [CustomEditor(typeof(LivePageContent))]
    class LivePageContentEditor : PageContentEditor
    {
        protected override void OnBeforeBuild()
        {
            (target as LivePageContent).RefreshRenderTexture();
        }
    }

    static class LivePageContentExtensions
    {
        static MethodInfo s_RefreshRTsMethod = ReflectionUtility.FindMethod(typeof(LivePageContent), "RefreshRTs");

        public static void RefreshRenderTexture(this LivePageContent livePageContent)
        {
            s_RefreshRTsMethod.Invoke(livePageContent, null);
        }
    }
}

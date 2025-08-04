using System;
using System.Reflection;

namespace ScriptBoy.ProceduralBook
{
    static class AnnotationUtility
    {
        static Type type;

        static PropertyInfo showSelectionOutlineProperty;

        public static bool showSelectionOutline
        {
            get => (bool)showSelectionOutlineProperty.GetValue(null);
        }

        static AnnotationUtility()
        {
            type = ReflectionUtility.FindTypeInUnityEditor(nameof(AnnotationUtility));
            showSelectionOutlineProperty = ReflectionUtility.FindProperty(type, nameof(showSelectionOutlineProperty));
        }
    }
}
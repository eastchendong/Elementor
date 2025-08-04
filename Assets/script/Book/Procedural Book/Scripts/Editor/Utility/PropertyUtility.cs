using System.Reflection;
using UnityEditor;
using Type = System.Type;

namespace ScriptBoy.ProceduralBook
{
    static class PropertyUtility
    {
        /// <summary>
        /// SerializedProperty m_Name = serializedObject.FindProperty("m_Name");
        /// </summary>
        public static void FindProperties(Editor editor)
        {
            SerializedObject serializedObject = editor.serializedObject;
            var type = editor.GetType();
            while (type != typeof(Editor))
            {
                var fields = type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
                foreach (var field in fields)
                {
                    if (field.FieldType == typeof(SerializedProperty))
                    {
                        string name = field.Name;
                        field.SetValue(editor, serializedObject.FindProperty(name));
                    }
                }
                type = type.BaseType;
            }
        }
    }
}

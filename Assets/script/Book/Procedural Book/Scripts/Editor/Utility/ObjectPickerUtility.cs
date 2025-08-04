using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Reflection;

using Type = System.Type;

namespace ScriptBoy.ProceduralBook
{
    static class ObjectPickerUtility
    {
        static MethodInfo s_ShowMethod;
        static Dictionary<Type, MethodInfo> s_GenericShowMethods;

        static ObjectPickerUtility()
        {
            s_GenericShowMethods = new Dictionary<Type, MethodInfo>();

            Type[] types = { typeof(Object), typeof(bool), typeof(string), typeof(int) };
            MethodInfo[] methods = typeof(EditorGUIUtility).GetMethods(BindingFlags.Public | BindingFlags.Static);
            foreach (var method in methods)
            {
                if (method.Name != "ShowObjectPicker") continue;
                if (!method.IsGenericMethod) continue;
                Type[] genericArguments = method.GetGenericArguments();
                if (genericArguments.Length != 1) continue;
                if (genericArguments[0].BaseType != typeof(Object)) continue;
                ParameterInfo[] parameters = method.GetParameters();
                int n = types.Length;
                if (parameters.Length != n) continue;
                for (int i = 0; i < n; i++)
                {
                    if (parameters[i].ParameterType != types[i]) continue;
                }
                s_ShowMethod = method;
                break;
            }
        }

        static MethodInfo GetGenericShowMethod(Type type)
        {
            MethodInfo method;
            if (!s_GenericShowMethods.TryGetValue(type, out method))
            {
                method = s_ShowMethod.MakeGenericMethod(type);
                s_GenericShowMethods.Add(type, method);
            }
            return method;
        }

        public static void Show(Type[] types, Object obj, bool allowSceneObjects, string searchFilter, int controlID)
        {
            GenericMenu menu = new GenericMenu();
            foreach (var type in types)
            {
                menu.AddItem(new GUIContent(type.Name), false, () => Show(type, obj, allowSceneObjects, searchFilter, controlID));
            }
            menu.ShowAsContext();
        }

        public static void Show(Type type, Object obj, bool allowSceneObjects, string searchFilter, int controlID)
        {
            object[] parameters = { obj, allowSceneObjects, searchFilter, controlID };
            GetGenericShowMethod(type).Invoke(null, parameters);
        }

        public static int GetControlID()
        {
            return EditorGUIUtility.GetObjectPickerControlID();
        }

        public static Object GetObject()
        {
            return EditorGUIUtility.GetObjectPickerObject();
        }
    }
}


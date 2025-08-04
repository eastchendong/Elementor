using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace ScriptBoy.ProceduralBook
{
    static class ReflectionUtility
    {
        static BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetProperty | BindingFlags.GetField | BindingFlags.Static | BindingFlags.InvokeMethod;

        public static Type FindTypeInUnityEditor(string name)
        {
            return FindType($"UnityEditor.{name},UnityEditor");
        }

        public static Type FindType(string name)
        {
            var type = Type.GetType(name);

            if (type == null)
            {
                Debug.LogError($"The type '{name}' does not exist! ({Application.unityVersion})");
            }

            return type;
        }

        public static PropertyInfo FindProperty(Type type, string name)
        {
            if (type == null) return null;

            if (name.EndsWith("Property")) name = name.Remove(name.Length - "Property".Length);

            var property = type.GetProperty(name, flags);

            if (property == null)
            {
                Debug.LogError($"The property '{type.Name}.{name}' does not exist! ({Application.unityVersion})");
            }

            return property;
        }

        public static MethodInfo FindMethod(Type type, string name)
        {
            if (type == null) return null;

            if (name.EndsWith("Method")) name = name.Remove(name.Length - "Method".Length);

            var method = type.GetMethod(name, flags);

            if (method == null)
            {
                Debug.LogError($"The method '{type.Name}.{name}' does not exist! ({Application.unityVersion})");
            }

            return method;
        }

        public static Type[] FindTypes(Type baseType)
        {
            return FindTypes(baseType, Assembly.GetAssembly(baseType));
        }

        public static Type[] FindTypes(Type baseType, Assembly assembly)
        {
            List<Type> types = new List<Type>();
            foreach (var type in assembly.GetTypes())
            {
                if (type.BaseType != baseType) continue;

                types.Add(type);
            }
            return types.ToArray();
        }
    }
}


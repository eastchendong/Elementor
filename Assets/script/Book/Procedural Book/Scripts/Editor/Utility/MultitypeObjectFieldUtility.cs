using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

using Type = System.Type;

namespace ScriptBoy.ProceduralBook
{
    static class MultitypeObjectFieldUtility
    {
        static Dictionary<Type[], string> s_NullLabels = new Dictionary<Type[], string>();
        static GUIStyle s_ObjectFieldButton = GUI.skin.FindStyle("ObjectFieldButton");

        public static void DrawLayout(SerializedProperty property, Type[] types)
        {
            GUIContent label = new GUIContent(property.displayName);
            DrawLayout(label, property, types);
        }

        public static void DrawLayout(GUIContent label, SerializedProperty property, Type[] types)
        {
            Rect rect = GUILayoutUtility.GetRect(Screen.width, EditorGUIUtility.singleLineHeight);
            Draw(rect, label, property, types);
        }

        public static void Draw(Rect rect, GUIContent label, SerializedProperty property, Type[] types)
        {
            int id = GUIUtility.GetControlID(label, FocusType.Keyboard, rect);
            var eventType = Event.current.type;
            switch (eventType)
            {
                case EventType.MouseDown:
                case EventType.Repaint:
                    Rect fieldRect = EditorGUI.PrefixLabel(rect, id, label);
                    float lineHeight = EditorGUIUtility.singleLineHeight;
                    Rect fieldButtonRect = RectUtility.DockRight(fieldRect, EditorGUIUtility.singleLineHeight);

                    Object obj = property.objectReferenceValue;

                    if (eventType == EventType.Repaint)
                    {
                        DrawFieldBackground(fieldRect, id);

                        bool valid = obj && ValidateObject(obj, types);
                        GUIContent objLabel;

                        if (!obj) objLabel = GetNullLabel(types);
                        else if (!valid) objLabel = GetObjectErrorLabel(obj);
                        else objLabel = GetObjectLabel(obj);

                        Rect fieldLabelRect = RectUtility.Shrink(fieldRect, 0, (fieldRect.height - lineHeight) / 2);
                        fieldLabelRect = RectUtility.Shrink(fieldLabelRect, 2, 0);

                        GUI.Label(fieldLabelRect, objLabel);
                        DrawFieldButton(fieldButtonRect, id);
                    }
                    else
                    {
                        fieldRect.width -= fieldButtonRect.width - 5;

                        if (obj && fieldRect.Contains(Event.current.mousePosition))
                        {
                            if (Event.current.button == 0)
                            {
                                int clickCount = Event.current.clickCount;
                                if (clickCount == 1)
                                {
                                    EditorGUIUtility.PingObject(obj);
                                }
                                else if (clickCount == 2)
                                {
                                    string path = AssetDatabase.GetAssetPath(obj);
                                    Selection.activeObject = path == "" ? obj : AssetDatabase.LoadAssetAtPath(path, typeof(Object));
                                }
                            }
                            Event.current.Use();
                        }

                        if (fieldButtonRect.Contains(Event.current.mousePosition))
                        {
                            if (Event.current.button == 0)
                            {
                                ObjectPickerUtility.Show(types, null, true, "", id);
                            }
                            Event.current.Use();
                        }

                    }
                    break;

                case EventType.DragUpdated:
                case EventType.DragPerform:
                    if (rect.Contains(Event.current.mousePosition))
                    {
                        var objectReference = FindObject(DragAndDrop.objectReferences[0], types);

                        if (objectReference)
                        {
                            DragAndDrop.visualMode = DragAndDropVisualMode.Generic;

                            if (eventType == EventType.DragPerform)
                            {
                                property.objectReferenceValue = objectReference;
                                DragAndDrop.activeControlID = 0;
                                DragAndDrop.AcceptDrag();
                                GUI.changed = true;
                            }
                            else
                            {
                                DragAndDrop.activeControlID = id;
                            }
                        }
                        Event.current.Use();
                    }
                    break;

                case EventType.ExecuteCommand:
                    if (ObjectPickerUtility.GetControlID() == id)
                    {
                        if (Event.current.commandName == "ObjectSelectorUpdated")
                        {
                            obj = ObjectPickerUtility.GetObject();
                            if (obj) obj = FindObject(obj, types);
                            property.objectReferenceValue = obj;
                            GUI.changed = true;
                        }
                    }
                    break;
            }
        }

        static void DrawFieldBackground(Rect rect, int id)
        {
            bool on = DragAndDrop.activeControlID == id;
            bool hover = rect.Contains(Event.current.mousePosition);
            EditorStyles.objectField.Draw(rect, GUIContent.none, id, on, hover);
        }

        static void DrawObjectLabel(Rect rect, int id)
        {
            bool hover = rect.Contains(Event.current.mousePosition);
            s_ObjectFieldButton.Draw(rect, GUIContent.none, id, false, hover);
        }

        static void DrawFieldButton(Rect rect, int id)
        {
            bool hover = rect.Contains(Event.current.mousePosition);
            s_ObjectFieldButton.Draw(rect, GUIContent.none, id, false, hover);
        }

        static bool ValidateObject(Object obj, Type[] types)
        {
            var objType = obj.GetType();

            foreach (var type in types)
            {
                if (type.IsAssignableFrom(objType)) return true;
            }

            return false;
        }

        static Object FindObject(Object obj, Type[] types)
        {
            if (ValidateObject(obj, types)) return obj;

            GameObject gameObject;

            if (obj is GameObject) gameObject = obj as GameObject;
            else if (obj is Component) gameObject = (obj as Component).gameObject;
            else gameObject = null;

            if (gameObject)
            {
                Type componentType = typeof(Component);
                foreach (var type in types)
                {
                    if (componentType.IsAssignableFrom(type))
                    {
                        Component component = gameObject.GetComponent(type);
                        if (component) return component;
                    }
                }
            }
            else
            {
                string path = AssetDatabase.GetAssetPath(obj);
                if (path != "")
                {
                    foreach (var type in types)
                    {
                        Object asset = AssetDatabase.LoadAssetAtPath(path, type);
                        if (asset) return asset;
                    }
                }
            }

            return null;
        }

        static GUIContent GetObjectLabel(Object obj)
        {
            string text = $"{obj.name} ({obj.GetType().Name})";
            return EditorGUIUtility.TrTextContent(text, GetObjectIcon(obj));
        }

        static GUIContent GetObjectErrorLabel(Object obj)
        {
            string text = $"{obj.name} ({obj.GetType().Name} is not supported)";
            GUIContent label = EditorGUIUtility.TrTextContent(text);
            label.image = EditorGUIUtility.IconContent("console.erroricon").image;
            return label;
        }

        static Texture2D GetObjectIcon(Object obj)
        {
            if (obj is Texture) return AssetPreview.GetMiniTypeThumbnail(typeof(Texture));

            return AssetPreview.GetMiniThumbnail(obj);
        }

        static GUIContent GetNullLabel(Type[] types)
        {
            if (!s_NullLabels.TryGetValue(types, out string text))
            {
                int n = types.Length;
                text = "None (";
                for (int i = 0; i < n; i++)
                {
                    text += types[i].Name;
                    if (i != n - 1) text += ", ";
                }
                text += ")";
                s_NullLabels.Add(types, text);
            }

            return EditorGUIUtility.TrTextContent(text);
        }
    }
}


using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Type = System.Type;

namespace ScriptBoy.ProceduralBook
{
    [CustomEditor(typeof(BookContent), true)]
    class BookContentEditor : Editor
    {
        //SerializedProperty m_Orientation;
        SerializedProperty m_Direction;
        SerializedProperty m_Covers;
        SerializedProperty m_Pages;

        PageContentList m_CoverList;
        PageContentList m_PageList;

        BookContent m_BookContent;

        void OnEnable()
        {
            PropertyUtility.FindProperties(this);

            m_BookContent = target as BookContent;

            Undo.undoRedoPerformed += UndoRedoPerformed;
            Undo.postprocessModifications += PostprocessModifications;

            if (target.name == "GameObject") target.name = ObjectNames.GetInspectorTitle(target).Replace(" Book ", " ");

            m_CoverList = new PageContentList(serializedObject, m_Covers);
            m_PageList = new PageContentList(serializedObject, m_Pages);
            m_CoverList.onSelectCallback += OnSelectCover;
            m_PageList.onSelectCallback += OnSelectPage;


            if (Application.isPlaying) return;

            Build();
        }

        void OnDisable()
        {
            Undo.undoRedoPerformed -= UndoRedoPerformed;
            Undo.postprocessModifications -= PostprocessModifications;

            if (Application.isPlaying) return;

            Build();
        }

        void OnSelectCover(ReorderableList list)
        {
            BookUtility.PingCover(m_BookContent, list.index);
            m_PageList.index = -1;
        }

        void OnSelectPage(ReorderableList list)
        {
            BookUtility.PingPage(m_BookContent, list.index);
            m_CoverList.index = -1;
        }

        void UndoRedoPerformed()
        {
            Build();
        }

        UndoPropertyModification[] PostprocessModifications(UndoPropertyModification[] modifications)
        {
            foreach (var m in modifications)
            {
                if (m.currentValue.target == target)
                {
                    EditorApplication.delayCall += Build;
                }
            }
            return modifications;
        }

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();

            serializedObject.Update();
            DrawProperties();
            serializedObject.ApplyModifiedProperties();

            if (EditorGUI.EndChangeCheck())
            {
                Build();
            }
        }

        void DrawProperties()
        {
            //EditorGUILayout.PropertyField(m_Orientation);
            //if(m_Orientation.enumValueIndex == 0)
            EditorGUILayout.PropertyField(m_Direction);

            m_CoverList.Draw();
            m_PageList.Draw();
        }

        void Build()
        {
            foreach (var book in Book.instances)
            {
                if (book.content == target) book.Build();
            }
        }
    }

    class PageContentList : ReorderableList
    {
        static Type[] m_Types = { typeof(Sprite), typeof(PageContent) };
        static Texture s_SelectionColor = TextureUtility.CreateColor(new Color32(200, 50, 60, 150));
        static Texture s_OddPaperColor = TextureUtility.CreateColor(new Color32(255, 255, 255, 20));
        static Texture s_DropColor = TextureUtility.CreateColor(new Color32(60, 120, 180, 255));

        public PageContentList(SerializedObject serializedObject, SerializedProperty property) : base(serializedObject, property,true, false, true, true)
        {
            drawElementCallback += DrawElementCallback;
            drawElementBackgroundCallback += DrawElementBackgroundCallback;
            elementHeight = EditorGUIUtility.singleLineHeight + 4;

            onAddCallback += OnAddCallback;
        }

        void OnAddCallback(ReorderableList list)
        {
            serializedProperty.InsertArrayElementAtIndex(serializedProperty.arraySize);
            var element = GetElement(serializedProperty.arraySize - 1);
            if (!(element.objectReferenceValue is Sprite)) element.objectReferenceValue = null;
            serializedProperty.serializedObject.ApplyModifiedProperties();
        }

        void DrawElementBackgroundCallback(Rect rect, int index, bool isActive, bool isFocused)
        {
            if (isActive)
            {
                GUI.DrawTexture(rect, s_SelectionColor);
            }
            else if ((index / 2) % 2 == 0)
            {
                GUI.DrawTexture(rect, s_OddPaperColor);
            }
        }

        void DrawElementCallback(Rect rect, int index, bool isActive, bool isFocused)
        {
            Rect dropLine = new Rect(rect.x, rect.y - 2, rect.width, 4);
            HandleDragAndDrop(dropLine, index);

            var element = GetElement(index);

            dropLine.y += rect.height;

            HandleDragAndDrop(dropLine, index + 1);

            rect.y += 2;
            rect.height -= 4;

           // rect = RectUtility.Shrink(rect, 0, k_ElementGap / 2);
            GUIContent label = EditorGUIUtility.TrTempContent("Element " + index);
            MultitypeObjectFieldUtility.Draw(rect, label, element, m_Types);

            //EditorGUI.PropertyField(rect, element);
        }

        SerializedProperty GetElement(int index)
        {
            return serializedProperty.GetArrayElementAtIndex(index);
        }

        public void Draw()
        {
            GUIContent label = new GUIContent(serializedProperty.displayName);
            Rect rect = GUILayoutUtility.GetRect(label, GUI.skin.label);

            HandleDragAndDrop(rect, serializedProperty.arraySize);

            bool foldout = serializedProperty.isExpanded;
            foldout = EditorGUI.Foldout(rect, foldout, label);
            serializedProperty.isExpanded = foldout;

            rect = RectUtility.DockRight(rect, 50);
            int arraySize = serializedProperty.arraySize;
            arraySize = EditorGUI.IntField(rect, arraySize);
            serializedProperty.arraySize = arraySize;


            if (foldout)
            {
                EditorGUI.indentLevel++;
                if (arraySize != Mathf.CeilToInt(arraySize / 4f) * 4f)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.HelpBox("The number of elements must be a multiple of 4.", MessageType.Warning);
                }
                EditorGUILayout.Space();
                DoLayoutList();

  
                EditorGUI.indentLevel--;
            }
        }

        void HandleDragAndDrop(Rect rect, int dropAt)
        {
            int id = GUIUtility.GetControlID(FocusType.Keyboard, rect);

            switch (Event.current.type)
            {
                case EventType.Repaint:
                    if (DragAndDrop.activeControlID == id)
                    {
                        GUI.DrawTexture(rect, s_DropColor);
                    }
                    break;
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    if (rect.Contains(Event.current.mousePosition))
                    {
                        Object[] objects = GetDraggedObjects();

                        if (objects.Length > 0)
                        {
                            DragAndDrop.visualMode = DragAndDropVisualMode.Generic;

                            if (Event.current.type == EventType.DragPerform)
                            {
                                foreach (var obj in objects)
                                {
                                    serializedProperty.InsertArrayElementAtIndex(dropAt);
                                    serializedProperty.GetArrayElementAtIndex(dropAt).objectReferenceValue = obj;
                                    dropAt++;
                                }

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
            }
        }

        Object[] GetDraggedObjects()
        {
            List<Object> objects = new List<Object>();
            foreach (var obj in DragAndDrop.objectReferences)
            {
                if (obj is PageContent) objects.Add(obj);
                else if (obj is Sprite) objects.Add(obj);
                else if (obj is Texture)
                {
                    string path = AssetDatabase.GetAssetPath(obj);
                    if (path == "") continue;
                    var assets = AssetDatabase.LoadAllAssetsAtPath(path);
                    foreach (var asset in assets)
                    {
                        if (asset is Sprite) objects.Add(asset);
                    }
                }
                else if (obj is GameObject)
                {
                    var component = (obj as GameObject).GetComponent<PageContent>();
                    if (component) objects.Add(component);
                }
            }
            return objects.ToArray();
        }
    }

    static class BookUtility
    {
        static MethodInfo s_PingPageMethod = ReflectionUtility.FindMethod(typeof(Book), "PingPage");

        static MethodInfo s_GetPageIndexMethod = ReflectionUtility.FindMethod(typeof(BookContent), "GetPageIndex");
        static MethodInfo s_GetPageContentIndexMethod = ReflectionUtility.FindMethod(typeof(BookContent), "GetPageContentIndex");


        public static void PingCover(BookContent content, int i)
        {
            int pageIndex = (int)s_GetPageIndexMethod.Invoke(content, new object[] { i, true });

            foreach (var book in Book.instances)
            {
                if (book.content == content)
                {
                    s_PingPageMethod.Invoke(book, new object[] { pageIndex });
                }
            }

            SceneView.RepaintAll();
        }

        public static void PingPage(BookContent content, int i)
        {
            int pageIndex = (int)s_GetPageIndexMethod.Invoke(content, new object[] { i, false });

            foreach (var book in Book.instances)
            {
                if (book.content == content)
                {
                    s_PingPageMethod.Invoke(book, new object[] { pageIndex });
                }
            }

            SceneView.RepaintAll();
        }

        public static void PingPage(PageContent pageContent)
        {
            BookContent bookContent = pageContent.bookContent;
            if (bookContent == null) return;

            int pageIndex = (int)s_GetPageContentIndexMethod.Invoke(bookContent, new object[] { pageContent });
            if(pageIndex == -1) return; ;

            foreach (var book in Book.instances)
            {
                if (book.content == bookContent)
                {
                    s_PingPageMethod.Invoke(book, new object[] { pageIndex });
                }
            }

            SceneView.RepaintAll();
        }
    }
}
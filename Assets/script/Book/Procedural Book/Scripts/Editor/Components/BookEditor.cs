using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace ScriptBoy.ProceduralBook
{
    [CustomEditor(typeof(Book), true)]
    class BookEditor : Editor
    {
        Book book => target as Book;

        static Type[] s_BindingTypes => ReflectionUtility.FindTypes(typeof(BookBinding));

        SerializedProperty m_Script;
        SerializedProperty m_Content;
        SerializedProperty m_Binding;
        SerializedProperty m_InitialOpenProgress;
        SerializedProperty m_BuildOnAwake;
        SerializedProperty m_CastShadows;
        SerializedProperty m_AlignToGround;
        SerializedProperty m_HideBinder;

        SerializedProperty m_ReduceShadows;
        SerializedProperty m_ReduceSubMeshes;
        SerializedProperty m_ReduceOverdraw;
        SerializedProperty m_UsePaperGPUInstancing;
        SerializedProperty m_PaperInstancingMaterial;

        SerializedProperty m_PagePaperSetup;
        SerializedProperty m_CoverPaperSetup;

        void OnEnable()
        {
            if (!target) return;

            PropertyUtility.FindProperties(this);

            if (target.name == "GameObject") target.name = "Book";

            Undo.undoRedoPerformed += UndoRedoPerformed;
            Undo.postprocessModifications += PostprocessModifications;
            SceneView.duringSceneGui += OnSceneGUI;

            if (!Application.isPlaying) Build();
        }

        void OnDisable()
        {
            Undo.undoRedoPerformed -= UndoRedoPerformed;
            Undo.postprocessModifications -= PostprocessModifications;
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        void OnSceneGUI(SceneView scene)
        {
            if (GraphicsSettings.defaultRenderPipeline == null)
            {
#if UNITY_2022_1_OR_NEWER
                if (AnnotationUtility.showSelectionOutline)
                {
                    Color color = SceneView.selectedOutlineColor;
                    color.a = 1;
                    Handles.DrawOutline(book.rendererIds, color);
                }
#endif
            }
        }

        void FindRenderers()
        {
            Renderer[] renderers = book.GetComponentsInChildren<Renderer>();
            List<int> list = new List<int>(renderers.Length);

            foreach (var renderer in renderers)
            {
                if (renderer.gameObject.hideFlags == HideFlags.HideAndDontSave)
                {
                    list.Add(renderer.GetInstanceID());
                }
            }
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
                    if (m.currentValue.propertyPath.Contains("m_AutoTurnSettings")) continue;
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

        void Build()
        {
            (target as Book).Build();
        }

        void DrawProperties()
        {
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(m_Script);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space();

            GUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(m_Content);
            if (!m_Content.objectReferenceValue && GUILayout.Button("Create", GUILayout.Width(60)))
            {
                CreateComponent(m_Content, typeof(BookContent), "Content");
            }
            GUILayout.EndHorizontal();


            GUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(m_Binding);
            if (!m_Binding.objectReferenceValue && GUILayout.Button("Create", GUILayout.Width(60)))
            {
                GenericMenu menu = new GenericMenu();
                foreach (var type in s_BindingTypes)
                {
                    string name = ObjectNames.NicifyVariableName(type.Name).Replace(" Book ", " ");
                    menu.AddItem(new GUIContent(name), false, () => AddComponent(book.gameObject, m_Binding, type));
                }
                menu.ShowAsContext();
            }
            GUILayout.EndHorizontal();

            EditorGUILayout.PropertyField(m_InitialOpenProgress);

            EditorGUILayout.Space();

            ToggleLeft(m_BuildOnAwake);
            ToggleLeft(m_CastShadows);
            ToggleLeft(m_AlignToGround);
            ToggleLeft(m_HideBinder);

            EditorGUILayout.Space();
            if (m_CastShadows.boolValue)
            {
                ToggleLeft(m_ReduceShadows);
            }
            ToggleLeft(m_ReduceSubMeshes);
            ToggleLeft(m_ReduceOverdraw);
            if (m_ReduceSubMeshes.boolValue && m_Binding.objectReferenceValue is WiroBookBinding)
            {
                ToggleLeft(m_UsePaperGPUInstancing);
                if (m_UsePaperGPUInstancing.boolValue)
                {
                    EditorGUILayout.PropertyField(m_PaperInstancingMaterial);
                }
            }
            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(m_CoverPaperSetup);
            EditorGUILayout.PropertyField(m_PagePaperSetup);
        }


        void ToggleLeft(SerializedProperty property)
        {
            GUIContent label = new GUIContent(property.displayName, property.tooltip);
            property.boolValue = EditorGUILayout.ToggleLeft(label, property.boolValue);
        }

        void CreateComponent(SerializedProperty property, Type type, string name)
        {
            GameObject gameObject = new GameObject(name);
            gameObject.transform.SetParent(book.transform, false);
            AddComponent(gameObject, property, type);
            Undo.RegisterCreatedObjectUndo(gameObject, "Create Book Component");
        }

        void AddComponent(GameObject gameObject, SerializedProperty property, Type type)
        {
            property.objectReferenceValue = Undo.AddComponent(gameObject, type);
            serializedObject.ApplyModifiedProperties();
        }
    }

    [CustomPropertyDrawer(typeof(AutoTurnSetting), true)]
    class AutoTurnSettingDrawer : PropertyDrawer
    {

        static class GUIContents
        {
            public static GUIContent mode = new GUIContent("", "Choose the mode of AutoTurnSetting:" +
                "\n\n- Constant: Specifies a constant value for the auto turn setting." +
                "\n\n- RandomBetweenTwoConstants: Specifies a random value generated between two constant values for the auto turn setting." +
                "\n\n- Curve: Specifies a value based on a curve for the auto turn setting." +
                "\n\n- RandomBetweenTwoCurves: Specifies a random value generated between two curves for the auto turn setting.");

            public static GUIContent curveTimeMode = new GUIContent("", "Choose the curve time mode of AutoTurnSetting:" +
                "\n\n- PaperIndexTime: Evaluates the curve based on the current paper index divided by the total paper count. This gives a time value proportional to the progression through the papers." +
                "\n\n- TurnIndexTime: Evaluates the curve based on the current turn index divided by the total turn count. This provides a time value proportional to the progression through the turns.");

        }


        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            SerializedProperty m_Mode = property.FindPropertyRelative("m_Mode");

            AutoTurnSettingMode mode = (AutoTurnSettingMode)m_Mode.enumValueIndex;

            RectUtility.SplitRight(rect, 20, out Rect rectL, out Rect rectR);
            rect = rectL;
            rect.width -= 5;
            rect = EditorGUI.PrefixLabel(rect, label);
            label = GUIContent.none;

            using (new EditorGUI.IndentLevelScope(-EditorGUI.indentLevel))
            {
                EditorGUI.PropertyField(rectR, m_Mode, GUIContent.none);
                EditorGUI.LabelField(rectR, GUIContents.mode);

                object[] attributes = fieldInfo.GetCustomAttributes(typeof(AutoTurnSettingRangeAttribute), false);
                AutoTurnSettingRangeAttribute range = attributes.Length > 0 ? (AutoTurnSettingRangeAttribute)attributes[0] : null;

                if (mode == AutoTurnSettingMode.Constant)
                {
                    SerializedProperty m_Constant = property.FindPropertyRelative("m_Constant");
                    if (range == null)
                    {
                        EditorGUI.PropertyField(rect, m_Constant, label);
                    }
                    else
                    {
                        EditorGUI.Slider(rect, m_Constant, range.min, range.max, label);
                    }
                }
                else if (mode == AutoTurnSettingMode.RandomBetweenTwoConstants)
                {
                    SerializedProperty m_ConstantMin = property.FindPropertyRelative("m_ConstantMin");
                    SerializedProperty m_ConstantMax = property.FindPropertyRelative("m_ConstantMax");

                    float min = m_ConstantMin.floatValue;
                    float max = m_ConstantMax.floatValue;
                    if (range == null || Mathf.Abs(rect.width) < 185)
                    {
                        GUIContent[] labels = new GUIContent[] { new GUIContent("Min"), new GUIContent("Max") };
                        float[] values = new float[] { min, max };
                        EditorGUI.MultiFloatField(rect, label, labels, values);
                        min = values[0];
                        max = values[1];
                    }
                    else
                    {
                        RectUtility.SplitLeft(rect, 50, out rectL, out rectR);
                        rect = rectL;
                        min = EditorGUI.FloatField(rect, label, min);

                        RectUtility.SplitRight(rectR, 50, out rectL, out rectR);
                        rect = rectL;
                        rect = RectUtility.Shrink(rect, 5, 0);
                        EditorGUI.MinMaxSlider(rect, label, ref min, ref max, range.min, range.max);
                        max = EditorGUI.FloatField(rectR, label, max);
                    }

                    if (range != null)
                    {
                        min = Mathf.Clamp(min, range.min, range.max);
                        max = Mathf.Clamp(max, range.min, range.max);
                    }

                    m_ConstantMin.floatValue = Mathf.Min(min, max);
                    m_ConstantMax.floatValue = Mathf.Max(min, max);
                }
                else
                {
                    SerializedProperty m_CurveTimeMode = property.FindPropertyRelative("m_CurveTimeMode");

                    RectUtility.SplitRight(rect, 20, out rectL, out rectR);
                    EditorGUI.PropertyField(rectR, m_CurveTimeMode, GUIContent.none);
                    EditorGUI.LabelField(rectR, GUIContents.curveTimeMode);
                    rect = rectL;

                    Rect rangeRect = new Rect(0, range.min, 1, range.max - range.min);

                    if (mode == AutoTurnSettingMode.Curve)
                    {
                        SerializedProperty m_Curve = property.FindPropertyRelative("m_Curve");

                        EditorGUI.CurveField(rect, m_Curve, Color.red, rangeRect, label);
                    }
                    else
                    {
                        SerializedProperty m_CurveMin = property.FindPropertyRelative("m_CurveMin");
                        SerializedProperty m_CurveMax = property.FindPropertyRelative("m_CurveMax");

                        RectUtility.SplitRight(rect, rect.width / 2, out rectL, out rectR);

                        EditorGUI.CurveField(rectL, m_CurveMin, Color.red, rangeRect, label);
                        EditorGUI.CurveField(rectR, m_CurveMax, Color.red, rangeRect, label);
                    }
                }
            }
        }
    }
}
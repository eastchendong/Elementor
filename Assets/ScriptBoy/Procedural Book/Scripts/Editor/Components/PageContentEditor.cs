using UnityEditor;
using UnityEngine;

namespace ScriptBoy.ProceduralBook
{
    [CustomEditor(typeof(PageContent), true)]
    class PageContentEditor : Editor
    {
        protected virtual void OnEnable()
        {
            PropertyUtility.FindProperties(this);
            Undo.undoRedoPerformed += UndoRedoPerformed;
            Undo.postprocessModifications += PostprocessModifications;

            if (Application.isPlaying) return;
            Build();
        }

        void OnDisable()
        {
            Undo.undoRedoPerformed -= UndoRedoPerformed;
            Undo.postprocessModifications -= PostprocessModifications;

            if (Application.isPlaying) return;
            Build(false);
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
                    EditorApplication.delayCall += () => Build();
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

        protected virtual void DrawProperties()
        {
            base.OnInspectorGUI();
        }

        void Build(bool pingPage = true)
        {
            OnBeforeBuild();
            var pageContent = target as PageContent;
            pageContent.bookContent?.book?.Build();
            if (pingPage) BookUtility.PingPage(pageContent);
        }

        protected virtual void OnBeforeBuild()
        {

        }
    }
}